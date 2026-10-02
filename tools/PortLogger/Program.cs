using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace PortLogger
{
    class Program
    {
        [DllImport("winmm.dll")]
        private static extern uint timeBeginPeriod(uint uPeriod);

        [DllImport("winmm.dll")]
        private static extern uint timeEndPeriod(uint uPeriod);

        private const string IoFilePath = @"C:\emu8086.io";
        
        private static bool _running = true;
        private static readonly object _sync = new object();
        private static string _lastFiveChanges = "";
        private static long _totalChanges = 0;

        static void Main(string[] args)
        {
            // Parse arguments
            string mode = "1ms";
            int rangeStart = 0;
            int rangeCount = 256;
            string outDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--mode" && i + 1 < args.Length) mode = args[++i];
                if (args[i] == "--range" && i + 2 < args.Length)
                {
                    rangeStart = int.Parse(args[++i]);
                    rangeCount = int.Parse(args[++i]);
                }
                if (args[i] == "--out" && i + 1 < args.Length) outDir = args[++i];
            }

            Console.WriteLine($"Starting PortLogger: mode={mode}, range={rangeStart}-{rangeStart+rangeCount}, out={outDir}");

            // Setup output files
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string changesCsv = Path.Combine(outDir, $"{timestamp}_changes.csv");
            string filesCsv = Path.Combine(outDir, $"{timestamp}_files.csv");

            // Guarantee read-only access to emu8086 files
            FileStream ioStream = null;
            while (ioStream == null)
            {
                try
                {
                    // Open read-only
                    ioStream = new FileStream(IoFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                }
                catch (FileNotFoundException)
                {
                    Console.WriteLine("Waiting for C:\\emu8086.io to be created...");
                    Thread.Sleep(250);
                }
            }

            Console.WriteLine("C:\\emu8086.io found. Starting capture (Press Ctrl+C to stop, or any letter to insert a marker).");

            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                _running = false;
            };

            Thread captureThread = new Thread(() => CaptureLoop(ioStream, mode, rangeStart, rangeCount, changesCsv, filesCsv));
            captureThread.Start();

            // Handle markers
            while (_running)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(intercept: true);
                    if (char.IsLetter(key.KeyChar))
                    {
                        lock (_sync)
                        {
                            File.AppendAllText(changesCsv, $"{Stopwatch.GetTimestamp() * 1000000L / Stopwatch.Frequency},MARK,{key.KeyChar},,\n");
                        }
                    }
                }
                Thread.Sleep(50);
            }

            captureThread.Join();
            Console.WriteLine("\nStopped cleanly.");
        }

        private static void CaptureLoop(FileStream ioStream, string mode, int rangeStart, int rangeCount, string changesCsv, string filesCsv)
        {
            if (mode == "1ms") timeBeginPeriod(1);

            byte[] shadow = new byte[65536];
            byte[] current = new byte[65536];
            
            Stopwatch sw = Stopwatch.StartNew();
            long lastFileLogTicks = 0;
            long lastFlushTicks = 0;

            StreamWriter changesWriter = new StreamWriter(new FileStream(changesCsv, FileMode.Create, FileAccess.Write, FileShare.Read));
            changesWriter.WriteLine("t_us,src,offset,old,new");
            
            StreamWriter filesWriter = new StreamWriter(new FileStream(filesCsv, FileMode.Create, FileAccess.Write, FileShare.Read));
            filesWriter.WriteLine("t_ms,io_size,io_lastwrite_utc,hw_size,hw_lastwrite_utc,hw_first64_hex");

            while (_running)
            {
                long currentUs = sw.ElapsedTicks * 1000000L / Stopwatch.Frequency;
                
                ioStream.Seek(0, SeekOrigin.Begin);
                int bytesRead = ioStream.Read(current, 0, current.Length);

                for (int i = rangeStart; i < rangeStart + rangeCount && i < bytesRead; i++)
                {
                    if (current[i] != shadow[i])
                    {
                        lock (_sync)
                        {
                            changesWriter.WriteLine($"{currentUs},io,{i},{shadow[i]},{current[i]}");
                            _totalChanges++;
                            _lastFiveChanges = $"{i:X2}H:{shadow[i]:X2}->{current[i]:X2} " + _lastFiveChanges;
                            if (_lastFiveChanges.Length > 60) _lastFiveChanges = _lastFiveChanges.Substring(0, 60);
                        }
                        shadow[i] = current[i];
                    }
                }

                if (sw.ElapsedMilliseconds - lastFileLogTicks > 250)
                {
                    FileInfo fi = new FileInfo(IoFilePath);
                    fi.Refresh();
                    long ms = sw.ElapsedMilliseconds;
                    filesWriter.WriteLine($"{ms},{fi.Length},{fi.LastWriteTimeUtc:o},0,,");
                    lastFileLogTicks = ms;

                    // Update live console (crude carriage return overwrite)
                    Console.Write($"\r{sw.Elapsed:hh\\:mm\\:ss} | Changes: {_totalChanges} | Recent: {_lastFiveChanges}".PadRight(80));
                }

                if (sw.ElapsedMilliseconds - lastFlushTicks > 1000)
                {
                    lock (_sync)
                    {
                        changesWriter.Flush();
                        filesWriter.Flush();
                    }
                    lastFlushTicks = sw.ElapsedMilliseconds;
                }

                if (mode == "1ms")
                {
                    Thread.Sleep(1);
                }
                else
                {
                    Thread.SpinWait(20);
                }
            }

            changesWriter.Flush();
            changesWriter.Close();
            filesWriter.Flush();
            filesWriter.Close();
            ioStream.Close();
            if (mode == "1ms") timeEndPeriod(1);
        }
    }
}
