using System;
using System.IO;
using System.Threading;

class Program
{
    static void Main()
    {
        string path = @"C:\emu8086.io";
        
        // Ensure file exists and is empty initially
        using (var fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
        {
            fs.SetLength(0);
            fs.SetLength(100); // Pre-allocate some bytes so length is not an issue
        }

        Console.WriteLine("Writing commands...");

        WriteByte(path, 0x00, 0x38);
        Thread.Sleep(50);
        WriteByte(path, 0x00, 0x0C);
        Thread.Sleep(50);
        WriteByte(path, 0x00, 0x06);
        Thread.Sleep(50);
        WriteByte(path, 0x00, 0x01);
        Thread.Sleep(50);

        WriteByte(path, 0x00, 0x80);
        Thread.Sleep(50);

        string text = "HELLO";
        foreach (char c in text)
        {
            WriteByte(path, 0x04, (byte)c);
            Thread.Sleep(50);
        }

        Console.WriteLine("Done writing.");
    }

    static void WriteByte(string path, int offset, byte value)
    {
        using (var fs = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
        {
            fs.Seek(offset, SeekOrigin.Begin);
            fs.WriteByte(value);
        }
    }
}
