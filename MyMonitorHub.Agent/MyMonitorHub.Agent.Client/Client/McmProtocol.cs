using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Linq;
using Serilog;

namespace MyMonitorHub.Agent.Client
{
    public class McmProtocol
    {
        private static readonly ILogger Logger = Log.ForContext<McmProtocol>();

        private readonly Socket _socket;
        private string _header;

        public McmProtocol(Socket socket)
        {
            _socket = socket;
        }

        public string Header => _header;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parms">string of parameters. each param must end with an \\r\\n</param>
        /// <param name="data"></param>
        public void Write(string parms, byte[] data)
        {
            if (string.IsNullOrEmpty(parms))
            {
                parms = "";
            }
            var length = "";
            if (data != null && data.Length > 0)
            {
                length = "Length: " + data.Length + "\r\n";
            }
            var text = "mcm\r\n" + length + parms + "\r\n";
            var all = Encoding.UTF8.GetBytes(text);
            if (data != null && data.Length > 0)
            {
                all = all.Concat(data).ToArray();                
            }
            _socket.Send(all);
        }

        private bool Compared(int offset, int size, byte[] fullBytes, byte[] comparison)
        {
            for (var index = 0; index < size; index++)
            {
                if (fullBytes[index + offset] != comparison[index]) return false;
            }
            return true;
        }

        private int Find(byte[] fullBytes, byte[] lookFor)
        {
            var length = fullBytes.Length;
            var endIndex = length - (lookFor.Length - 1);
            for (var index = 0; index < endIndex; index++)
            {
                if (Compared(index, lookFor.Length, fullBytes, lookFor))
                {
                    return index;
                }
            }
            return -1;
        }

        public byte[] Read()
        {

            var comparison = Encoding.UTF8.GetBytes("mcm");
            var lookFor = Encoding.UTF8.GetBytes("\r\n\r\n");
            // Data buffer for incoming data.
            var bytes = new byte[1024];

            var memoryStream = new MemoryStream();
            var haveHeader = false;
            var end = 0;
            while (!haveHeader)
            {
                var length = _socket.Receive(bytes);
                if (length <- 0)
                {
                    Logger.Error("1: length is {0}",length);
                    throw new Exception("1: length is "+ length);
                }
                if (length > 0)
                {
                    if (length > 2)
                    {
                        if (!Compared(0, 3, bytes, comparison))
                        {
                            Logger.Debug("Not the mcm protocol");
                            throw new Exception("Not the mcm protocol");
                        }
                    }
                    memoryStream.Write(bytes, 0, length);
                    var testBytes = memoryStream.ToArray();
                    if ((end = Find(testBytes, lookFor)) != -1)
                    {
                        haveHeader = true;
                    }
                }
            } // while
            var headerBytes = new byte[end + 4];

            memoryStream.Position = 0;
            memoryStream.Read(headerBytes, 0, end + 4);
            _header = Encoding.UTF8.GetString(headerBytes);
            if (memoryStream.Length == end + 4)
            {
                memoryStream = new MemoryStream();
            }
            else
            {
                var count = memoryStream.Length - (end + 4);
                var dataBytes = new byte[count];
                memoryStream.Read(dataBytes, 0, (int) count);

                memoryStream = new MemoryStream();
                memoryStream.Write(dataBytes,0,dataBytes.Length);
            }

            const int MaxBodyLength = 10 * 1024 * 1024; // 10 MB hard cap

            var stuff = GetHeaderParam("Length: ");
            var bodyLength = 0;
            if (stuff != null)
            {
                if (!int.TryParse(stuff, out bodyLength) || bodyLength < 0 || bodyLength > MaxBodyLength)
                {
                    Logger.Warning("MCM body length '{0}' rejected (max {1} bytes)", stuff, MaxBodyLength);
                    throw new Exception($"MCM body length out of range: {stuff}");
                }
            }
            while (bodyLength != memoryStream.Length)
            {
                var length = _socket.Receive(bytes);
                if (length <= 0)
                {
                    Logger.Error("2: length is {0}",length);
                    throw new Exception("2: length is " + length);
                }
                if (length > 0)
                {
                    memoryStream.Write(bytes, 0, length);
                }
            } // while
            var allBytes = memoryStream.ToArray();

            if (!_header.StartsWith("mcm\r\n"))
            {
                throw new Exception("Unrecognized protocol");                
            }
            return allBytes;
        }

        private string GetHeaderParam(String searchFor)
        {
            var start = _header.IndexOf("\r\n" + searchFor, StringComparison.Ordinal);
            if (start == -1)
            {
                return null;
            }
            var endIndex = _header.IndexOf("\r\n", start + 2, StringComparison.Ordinal);

            var length = (endIndex - (start + searchFor.Length + 2));

            return _header.Substring(start + searchFor.Length + 2, length);
        }
    }
}
