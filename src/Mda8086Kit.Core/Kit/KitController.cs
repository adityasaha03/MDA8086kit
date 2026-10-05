using System;
using System.Collections.Generic;
using Mda8086Kit.Core.Abstractions;
using System.Threading;

namespace Mda8086Kit.Core.Kit
{
    public class KitController
    {
        public object SyncRoot { get; } = new object();
        private readonly List<IPortDevice> _devices = new List<IPortDevice>();
        
        private long _sequenceNumber = 0;
        private long _lastChangeTicks = 0;
        private KitSnapshot _latestSnapshot;

        private readonly Devices.PersistenceIntegrator _matrixIntegrator;

        public KitController()
        {
            // 33ms window, gain 8.0, 2000ms smoothing (2 seconds) for emu8086 IPC overhead
            _matrixIntegrator = new Devices.PersistenceIntegrator(33, 8.0f, 50000, 1);
            
            _latestSnapshot = new KitSnapshot(
                new ConnectionState(ConnectionStatus.WaitingForEmu8086, "Waiting", "Start emu8086"), 
                _sequenceNumber, 
                0,
                default(Devices.SegmentState),
                default(Devices.LedState),
                new DotMatrixState(new float[64], new float[64], Devices.DotMatrixOrientation.Normal),
                null);
        }

        public void AddDevice(IPortDevice device)
        {
            lock (SyncRoot)
            {
                _devices.Add(device);
            }
        }

        public KitSnapshot GetLatestSnapshot()
        {
            return Volatile.Read(ref _latestSnapshot);
        }

        public void Reset()
        {
            lock (SyncRoot)
            {
                foreach (var device in _devices)
                {
                    device.Reset();
                }
                _sequenceNumber = 0;
                _matrixIntegrator.Reset();
            }
        }

        public int[] ClearDevice(string deviceName)
        {
            var clearedPorts = new List<int>();
            lock (SyncRoot)
            {
                foreach (var device in _devices)
                {
                    if (device is Devices.Ppi8255 ppi)
                    {
                        if (deviceName == "7-SEGMENT" && ppi.Name == "CS2")
                        {
                            ppi.OnCpuWrite(0x19, 0xFF, 0); // Port A (Active low)
                            clearedPorts.Add(0x19);
                        }
                        else if (deviceName == "LEDS" && ppi.Name == "CS2")
                        {
                            ppi.OnCpuWrite(0x1B, 0x00, 0); // Port B
                            clearedPorts.Add(0x1B);
                        }
                        else if (deviceName == "DOT MATRIX 8 x 8" && ppi.Name == "CS1")
                        {
                            ppi.OnCpuWrite(0x18, 0xFF, 0); // Port A (Red Data, Active-Low)
                            ppi.OnCpuWrite(0x1A, 0xFF, 0); // Port B (Scan, Active-Low)
                            ppi.OnCpuWrite(0x1C, 0x00, 0); // Port C (Green Data, Active-High)
                            clearedPorts.AddRange(new[] { 0x18, 0x1A, 0x1C });
                            _matrixIntegrator.Reset();
                        }
                    }
                    else if (device is Devices.LcdDevice lcd && deviceName == "LCD 16 x 2")
                    {
                        lcd.Reset();
                        clearedPorts.AddRange(new[] { 0x30, 0x32 });
                    }
                }
            }
            return clearedPorts.ToArray();
        }

        public void OnPoll(PortBatch batch, long ticks)
        {
            lock (SyncRoot)
            {
                bool changed = false;

                // Step 1: Tick every device
                foreach (var device in _devices)
                {
                    device.Tick(ticks);
                }

                // Step 2: Dispatch batch changes
                for (int i = 0; i < batch.Count; i++)
                {
                    var change = batch[i];
                    changed = true;
                    _lastChangeTicks = change.Ticks;

                    foreach (var device in _devices)
                    {
                        if (device.Handles(change.Port))
                        {
                            device.OnCpuWrite(change.Port, change.Value, change.Ticks);
                        }
                    }
                }

                // Step 3: Continuously integrate the matrix state even if no port changes occurred this poll
                var dotMatrixModel = new Devices.DotMatrixModel();
                ulong currentR = 0;
                ulong currentG = 0;

                foreach (var device in _devices)
                {
                    if (device is Devices.Ppi8255 ppi && ppi.Name == "CS1")
                    {
                        dotMatrixModel.Decode(
                            ppi.GetPinStateA(), 
                            ppi.GetPinStateB(), 
                            ppi.GetPinStateC(), 
                            Devices.DotMatrixOrientation.Normal, 
                            out currentR, out currentG);
                    }
                }

                if (currentG != 0) Console.WriteLine($"DIAG: currentG = {currentG:X16}");

                _matrixIntegrator.SetInput(currentR, currentG);
                _matrixIntegrator.Tick(ticks);

                // Build snapshot if something changed (stubbed simplified version)
                if (changed || batch.Reseeded || _sequenceNumber == 0 || true) // Always build to animate matrix
                {
                    _sequenceNumber++;
                    
                    Devices.SegmentState segState = default;
                    Devices.LedState ledState = default;
                    LcdDisplayState lcdState = null;

                    foreach (var device in _devices)
                    {
                        if (device is Devices.Ppi8255 ppi)
                        {
                            if (ppi.Name == "CS2")
                            {
                                var sevenSegModel = new Devices.SevenSegmentModel();
                                var ledBank = new Devices.LedBank();
                                
                                segState = sevenSegModel.Decode(ppi.GetPinStateA());
                                ledState = ledBank.Decode(ppi.GetPinStateB());
                            }
                        }
                        else if (device is Devices.LcdDevice lcd)
                        {
                            lcdState = new LcdDisplayState(lcd.Model);
                        }
                    }
                    
                    // Copy arrays so the UI thread doesn't see partial updates
                    var rDisp = new float[64];
                    var gDisp = new float[64];
                    Array.Copy(_matrixIntegrator.RedDisplay, rDisp, 64);
                    Array.Copy(_matrixIntegrator.GreenDisplay, gDisp, 64);
                    var matrixState = new DotMatrixState(rDisp, gDisp, Devices.DotMatrixOrientation.Normal);

                    var newSnapshot = new KitSnapshot(
                        new ConnectionState(ConnectionStatus.Connected, "Connected", ""), 
                        _sequenceNumber, 
                        _lastChangeTicks,
                        segState,
                        ledState,
                        matrixState,
                        lcdState);
                    
                    Volatile.Write(ref _latestSnapshot, newSnapshot);
                }
            }
        }
    }
}
