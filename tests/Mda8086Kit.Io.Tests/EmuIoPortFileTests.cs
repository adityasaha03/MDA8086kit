using System;
using System.IO;
using System.Linq;
using Xunit;
using Mda8086Kit.Io;

namespace Mda8086Kit.Io.Tests
{
    public class EmuIoPortFileTests : IDisposable
    {
        private readonly string _tempFile;

        public EmuIoPortFileTests()
        {
            _tempFile = Path.GetTempFileName();
        }

        public void Dispose()
        {
            if (File.Exists(_tempFile))
            {
                try { File.Delete(_tempFile); } catch { }
            }
        }

        [Fact]
        public void Poll_FileNotExists_ReturnsEmpty()
        {
            string nonExistent = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            using var fileSource = new EmuIoPortFile(nonExistent);
            
            var changes = fileSource.Poll(100).ToList();
            Assert.Empty(changes);
        }

        [Fact]
        public void Poll_DetectsChanges()
        {
            File.WriteAllBytes(_tempFile, new byte[] { 0, 0, 0xAA, 0 });
            using var fileSource = new EmuIoPortFile(_tempFile);
            
            var changes = fileSource.Poll(100).ToList();
            Assert.Single(changes);
            Assert.Equal(2, changes[0].Port);
            Assert.Equal(0xAA, changes[0].Value);

            // No changes next time
            Assert.Empty(fileSource.Poll(200));

            // Append bytes
            using (var stream = new FileStream(_tempFile, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
            {
                stream.Seek(0, SeekOrigin.Begin);
                stream.Write(new byte[] { 0xBB, 0, 0xAA, 0xCC }, 0, 4);
            }

            var nextChanges = fileSource.Poll(300).ToList();
            Assert.Equal(2, nextChanges.Count); // Port 0 and Port 3
            
            var p0 = nextChanges.Single(c => c.Port == 0);
            Assert.Equal(0xBB, p0.Value);

            var p3 = nextChanges.Single(c => c.Port == 3);
            Assert.Equal(0xCC, p3.Value);
        }

        [Fact]
        public void Poll_DetectsTruncationAndThrows()
        {
            File.WriteAllBytes(_tempFile, new byte[] { 1, 2, 3, 4, 5 });
            using var fileSource = new EmuIoPortFile(_tempFile);
            
            // Initial read
            fileSource.Poll(100);

            // Truncate file (simulate emulator restart)
            using (var stream = new FileStream(_tempFile, FileMode.Truncate, FileAccess.Write, FileShare.ReadWrite))
            {
                stream.Write(new byte[] { 1, 2 }, 0, 2);
            }

            Assert.Throws<InvalidOperationException>(() => fileSource.Poll(200));
        }

        [Fact]
        public void Reset_ClearsShadowAndRecovers()
        {
            File.WriteAllBytes(_tempFile, new byte[] { 0xAA });
            using var fileSource = new EmuIoPortFile(_tempFile);
            
            var changes1 = fileSource.Poll(100).ToList();
            Assert.Single(changes1);

            fileSource.Reset();

            var changes2 = fileSource.Poll(200).ToList();
            Assert.Single(changes2); // Should trigger again because shadow was cleared
            Assert.Equal(0xAA, changes2[0].Value);
        }
    }
}
