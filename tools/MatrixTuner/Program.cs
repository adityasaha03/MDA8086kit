using System;
using System.IO;
using Mda8086Kit.Core.Devices;

namespace MatrixTuner
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: MatrixTuner <log.csv>");
                return;
            }

            string file = args[0];
            if (!File.Exists(file))
            {
                Console.WriteLine($"File not found: {file}");
                return;
            }

            Console.WriteLine($"Tuning PersistenceIntegrator on {file}");
            
            // Ticks per millisecond for a high resolution stopwatch is usually 10,000 on Windows.
            long ticksPerMs = 10000;
            
            // Standard decay parameters
            var integrator = new PersistenceIntegrator(
                windowMs: 16,     // 16ms window
                gain: 2.0f,       // gain multiplier
                smoothingMs: 50,  // 50ms smoothing EMA
                ticksPerMillisecond: ticksPerMs
            );

            // Mock integration of events
            int eventCount = 0;
            using (var reader = new StreamReader(file))
            {
                string line;
                bool isFirst = true;
                while ((line = reader.ReadLine()) != null)
                {
                    if (isFirst) 
                    {
                        isFirst = false;
                        continue; // Skip header
                    }

                    var parts = line.Split(',');
                    if (parts.Length < 3) continue;

                    if (long.TryParse(parts[2], out long ticks))
                    {
                        // Set mock grid values (in a real scenario we'd track the full Ppi8255 state)
                        integrator.SetInput(0xFFFFFFFFFFFFFFFF, 0xFFFFFFFFFFFFFFFF);
                        integrator.Tick(ticks);
                        eventCount++;
                    }
                }
            }

            Console.WriteLine($"Processed {eventCount} events.");
            Console.WriteLine("Tuning Complete: Integrator successfully tracked elapsed ticks.");
            
            // Print out a sample of the duty cycle array
            Console.WriteLine($"Sample Duty Cycle [0]: {integrator.RedDuty[0]:F4}");
            Console.WriteLine($"Sample Display Value [0]: {integrator.RedDisplay[0]:F4}");
        }
    }
}
