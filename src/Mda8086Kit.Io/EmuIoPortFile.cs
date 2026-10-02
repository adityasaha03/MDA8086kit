using System;
using System.Collections.Generic;
using System.IO;
using Mda8086Kit.Core;

namespace Mda8086Kit.Io
{
    public class EmuIoPortFile : IPortSource, IDisposable
    {
        private readonly string _path;
        private FileStream _stream;
        private readonly byte[] _shadow = new byte[65536];
        private long _previousLength = 0;

        public EmuIoPortFile(string path)
        {
            _path = path ?? throw new ArgumentNullException(nameof(path));
        }

        public IEnumerable<PortData> Poll(long currentUs)
        {
            var changes = new List<PortData>();

            if (_stream == null)
            {
                try
                {
                    if (File.Exists(_path))
                    {
                        _stream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 1);
                        _previousLength = _stream.Length;
                    }
                    else
                    {
                        return changes; // Not created yet
                    }
                }
                catch (IOException)
                {
                    return changes; // Locked
                }
            }

            try
            {
                long currentLength = _stream.Length;
                if (currentLength < _previousLength)
                {
                    // Truncation detected
                    throw new InvalidOperationException("File was truncated (emulator reset).");
                }
                _previousLength = currentLength;

                if (currentLength == 0) return changes;

                _stream.Seek(0, SeekOrigin.Begin);
                byte[] buffer = new byte[currentLength];
                int read = _stream.Read(buffer, 0, (int)currentLength);

                for (int i = 0; i < read; i++)
                {
                    if (buffer[i] != _shadow[i])
                    {
                        changes.Add(new PortData(i, buffer[i], currentUs));
                        _shadow[i] = buffer[i];
                    }
                }
            }
            catch (IOException)
            {
                // Locked briefly by emulator writing
            }

            return changes;
        }

        public void Reset()
        {
            Array.Clear(_shadow, 0, _shadow.Length);
            _previousLength = 0;
            if (_stream != null)
            {
                _stream.Dispose();
                _stream = null;
            }
        }

        public void Dispose()
        {
            if (_stream != null)
            {
                _stream.Dispose();
                _stream = null;
            }
        }
    }
}
