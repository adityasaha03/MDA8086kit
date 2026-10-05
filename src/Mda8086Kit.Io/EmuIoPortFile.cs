using System;
using System.IO;
using Mda8086Kit.Core.Abstractions;

namespace Mda8086Kit.Io
{
    public class EmuIoPortFile : IPortSource
    {
        private readonly string _path;
        private FileStream _stream;

        public ConnectionState State { get; private set; }

        public EmuIoPortFile(string path)
        {
            _path = path;
            State = new ConnectionState(ConnectionStatus.WaitingForEmu8086, "Waiting for emu8086", "Start emu8086 and run a program that uses the virtual device.");
        }

        public bool TryReadRange(int firstPort, byte[] buffer, int count)
        {
            try
            {
                if (_stream == null)
                {
                    _stream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                    State = new ConnectionState(ConnectionStatus.Connected, "Connected", "");
                }

                long len = _stream.Length;
                if (len <= firstPort)
                {
                    // File hasn't reached this port yet, zero the buffer
                    Array.Clear(buffer, 0, count);
                    return true; 
                }

                int toRead = (int)Math.Min(count, len - firstPort);
                _stream.Seek(firstPort, SeekOrigin.Begin);
                int read = _stream.Read(buffer, 0, toRead);
                
                if (read < count)
                {
                    // Zero out the rest of the buffer to reflect the file's current size
                    Array.Clear(buffer, read, count - read);
                }

                State = new ConnectionState(ConnectionStatus.Connected, "Connected", "");
                return true;
            }
            catch (FileNotFoundException)
            {
                State = new ConnectionState(ConnectionStatus.WaitingForEmu8086, "Waiting for emu8086", "Start emu8086 and run a program.");
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                State = new ConnectionState(ConnectionStatus.AccessDenied, "Access Denied", "Check file permissions.");
                return false;
            }
            catch (IOException ex) when ((ex.HResult & 0xFFFF) == 0x20 || (ex.HResult & 0xFFFF) == 0x21)
            {
                State = new ConnectionState(ConnectionStatus.Locked, "Locked", "File is locked by another process.");
                return false;
            }
            catch (IOException)
            {
                State = new ConnectionState(ConnectionStatus.IoError, "IO Error", "Check logs.");
                return false;
            }
        }

        public bool TryWriteByte(int port, byte value)
        {
            try
            {
                if (_stream == null) return false;
                if (_stream.Length <= port) return false;

                _stream.Seek(port, SeekOrigin.Begin);
                _stream.WriteByte(value);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            _stream?.Dispose();
            _stream = null;
        }
    }
}
