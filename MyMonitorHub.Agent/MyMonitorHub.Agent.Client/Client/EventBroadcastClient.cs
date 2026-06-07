using System;
using System.Net.Sockets;
using System.Text;
using Serilog;

namespace MyMonitorHub.Agent.Client
{
    public enum StatusCode
    {
        Ok = 1,
        Fail = 3
    }

    public enum StyleCode
    {
        SendsHealth = 1,
        NoHealth = 2
    }

    public class EventBroadcastClient
    {
        private readonly int _port;
        private readonly string _ip;
        private static readonly ILogger Logger = Log.ForContext<EventBroadcastClient>();

        public EventBroadcastClient(int port)
        {
            _port = port;
            _ip = "localhost";
        }

        public void FireEvent(string eventName, string category, string name, string description, StatusCode status,
                              StyleCode style)
        {
            string statusString;
            switch (status)
            {
                case StatusCode.Ok:
                    statusString = "OK";
                    break;
                case StatusCode.Fail:
                    statusString = "FAIL";
                    break;
                default:
                    throw new Exception("invalid status");
            }

            string styleString;
            switch (style)
            {
                case StyleCode.SendsHealth:
                    styleString = "SENDS_HEALTH";
                    break;
                case StyleCode.NoHealth:
                    styleString = "NO_HEALTH";
                    break;
                default:
                    throw new Exception("invalid style");
            }
            var parms =
                "Command: fireEvent\r\n" + 
                "Group: " + eventName + "\r\n" +
                "Category: " + category + "\r\n" +
                "Name: " + name + "\r\n" +
                "Description: " + description + "\r\n" +
                "Status: " + statusString + "\r\n" +
                "Style: " + styleString + "\r\n";


            try
            {
                Send(parms, null);
            }
            catch (Exception e)
            {
                Logger.Error(e.Message + "\r\n" + e.StackTrace);
            }
        }

        public void Ping()
        {
            const string parms = "Command: ping\r\n";
            try
            {
                Send(parms, null);
            }
            catch (Exception e)
            {
                Logger.Error(e.Message + "\r\n" + e.StackTrace);
                throw; // in keeping with what the original code does, rethrow the error
            }

        }

        private string Send(string parms, byte[] data)
        {
            Logger.Debug("IP: {0}, Port: {1}, parms = {2}",_ip, _port, parms);
            var socket = new Socket(AddressFamily.InterNetwork,
                                    SocketType.Stream, ProtocolType.Tcp);

            try
            {

                socket.Connect(_ip, _port);

                socket.ReceiveTimeout = 30000; // 30 seconds

                var mcmProtocol = new McmProtocol(socket);

                mcmProtocol.Write(parms, data);

                Logger.Debug("Start read");
                var result = mcmProtocol.Read();
                Logger.Debug("End read");
                return Encoding.UTF8.GetString(result);
            }
            finally
            {
                try
                {
                    socket.Shutdown(SocketShutdown.Both);
                }
                // ReSharper disable EmptyGeneralCatchClause
                catch (Exception)
                {
                }
                try
                {
                    socket.Close();
                }
                catch (Exception)
                {
                }
                
            }
        }
    } // class
}