using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using MyMonitorHub.Common.WebApi;
using MyMonitorHub.Agent.Client;
using Serilog;

namespace MyMonitorHub.Agent.Core
{
    /// <summary>
    ///     This process handles the incomming event which will basically put it in the queue. We batch up events and send
    ///     them every minute rather than immediately.
    /// </summary>
    internal sealed class CommandProcessingThread
    {
        private static readonly ILogger Logger = Log.ForContext<CommandProcessingThread>();
        private readonly Socket _socket;
        private McmProtocol? _mcmProtocol;

        internal CommandProcessingThread(Socket socket)
        {
            _socket = socket;
        }

        // constructor

        /// <summary>
        ///     Main starting point of thread.
        /// </summary>
        private void Run()
        {
            try
            {
                using (_socket)
                {
                    try
                    {
                        _socket.ReceiveTimeout = 30000; // 30 seconds

                        _mcmProtocol = new McmProtocol(_socket);

                        // the body currently isn't used
                        _mcmProtocol.Read();

                        // the header has all my params in it.
                        var data = _mcmProtocol.Header;

                        // I must have got the full response now
                        var command = GetData(data, "Command: ");
                        if (command == null)
                        {
                            SayFailed("Missing 'Command' parameter");
                            return;
                        }
                        if (command == "fireEvent")
                        {
                            FireEventCommand(data);
                        }
                        else if (command == "ping")
                        {
                            Ping();
                        }
                        else
                        {
                            SayFailed("Unrecognized command: " + command);
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Error($"run> {e.Message}\r\n{e.StackTrace}");
                    }
                } // using _socket

            }
            catch (Exception ex)
            {
                // these seems extremely unlikely but I will trap for it anyways
                Logger.Error($"run> dispose problem> {ex.Message}\r\n{ex.StackTrace}");
            }
        }

        // run

        private void Ping()
        {
            SayPassed();
        }

        private void FireEventCommand(string data)
        {
            var group = GetData(data, "Group: ");
            var category = GetData(data, "Category: ");
            var name = GetData(data, "Name: ");
            var desc = GetData(data, "Description: ");
            var status = GetData(data, "Status: ");
            var style = GetData(data, "Style: ");
            if (group == null || category == null || name == null || desc == null || status == null || style == null)
            {
                SayFailed("Missing a parameter");
                return;
            }
            EventType stat;
            switch (status.ToUpper())
            {
                case "OK":
                    stat = EventType.Ok;
                    break;
                //case "WARN":
                //    stat = Event.StatusWarn;
                //    break;
                case "FAIL":
                    stat = EventType.Fail;
                    break;
                default:
                    SayFailed("Invalid status code.");
                    return;
            }

            ItemStyle styleInt;
            if (style.ToUpper() == "NO_HEALTH")
            {
                styleInt = ItemStyle.NoHealth;
            }
            else if (style.ToUpper() == "SENDS_HEALTH")
            {
                styleInt = ItemStyle.SendsHealth;
            }
            else
            {
                SayFailed("Invalid style code.");
                return;
            }

            Monitor.FireEvent(group, category, name, desc, stat, styleInt, DateTime.Now);
            SayPassed();
        }


        private static string? GetData(string data, string searchFor)
        {
            var start = data.IndexOf("\r\n" + searchFor, StringComparison.Ordinal);
            if (start == -1)
            {
                return null;
            }
            var endIndex = data.IndexOf("\r\n", start + 2, StringComparison.Ordinal);

            return data.Substring(start + searchFor.Length + 2, endIndex - (start + searchFor.Length + 2));
        }

        private void SayFailed(string reason)
        {
            try
            {
                // if what was read was encrypted then the response should also be encrypted
                _mcmProtocol!.Write("Status: Fail\r\n", Encoding.UTF8.GetBytes(reason));
            }
            catch (IOException e)
            {
                Logger.Error(e, "Failed to say Fail");
            }
        }

        private void SayPassed()
        {
            try
            {
                // if what was read was encrypted then the response should also be encrypted
                _mcmProtocol!.Write("Status: OK\r\n", null);
            }
            catch (IOException e)
            {
                Logger.Error(e, "Failed to say Pass");
            }
        }

        internal void Start()
        {
            var thread = new Thread(Run);
            thread.Start();
        }

    }

    // class
}