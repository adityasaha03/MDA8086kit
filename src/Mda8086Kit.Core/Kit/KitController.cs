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
                    if (device is Devices.Ppi8255 ppi && ppi.Name == "CS2")
                    {
                        dotMatrixModel.Decode(
                            ppi.GetPinStateA(), 
                            ppi.GetPinStateB(), 
                            ppi.GetPinStateC(), 
                            Devices.DotMatrixOrientation.Normal, 
                            out currentR, out currentG);
                    }
                }

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
                            if (ppi.Name == "CS1")
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
