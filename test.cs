using System;
using System.IO;
class Program {
    static void Main() {
        try {
            using (var fs = new FileStream(@"C:\emu8086.io", FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete)) {
                fs.Seek(0x1B, SeekOrigin.Begin);
                fs.WriteByte(0x00);
                fs.Flush();
                Console.WriteLine("Write successful");
            }
        } catch (Exception ex) {
            Console.WriteLine(ex.Message);
        }
    }
}
