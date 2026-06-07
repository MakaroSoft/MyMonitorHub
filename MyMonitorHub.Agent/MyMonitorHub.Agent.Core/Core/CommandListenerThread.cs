using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Serilog;

namespace MyMonitorHub.Agent.Core
{
    internal sealed class CommandListenerThread : IThreadShutdown
    {
        private static readonly ILogger Logger = Log.ForContext<CommandListenerThread>();

        private readonly int _port;
        private volatile bool _stopped = true;

        private Socket? _listener;
        private static readonly ManualResetEvent AllDone = new ManualResetEvent(false);


        internal CommandListenerThread(int port)
        {
            _port = port;
        }

        private void Run()
        {
            _stopped = false;
            var count = 0;
            while (!_stopped)
            {
                Logger.Information("Command listener Started on Port: " + _port);
                try
                {
                    // Bind to loopback only. This listener is intended for local IPC
                    // (e.g. backup scripts on the same host firing "OK"/"FAIL" events into
                    // the agent's queue). Binding to IPAddress.Any exposed an unauthenticated
                    // cleartext command port to the entire network, letting any reachable
                    // host inject or suppress monitoring events. See security audit finding A-1.
                    var localEndPoint = new IPEndPoint(IPAddress.Loopback, _port);

                    // Create a TCP/IP socket.
                    _listener = new Socket(AddressFamily.InterNetwork,
                        SocketType.Stream, ProtocolType.Tcp);

                    // Bind the socket to the local endpoint and 
                    // listen for incoming connections.

                    _listener.Bind(localEndPoint);
                    _listener.Listen(100);

                    // Start listening for connections.
                    _stopped = false;
                    while (!_stopped)
                    {
                        // Set the event to nonsignaled state.
                        AllDone.Reset();

                        _listener.BeginAccept(AcceptCallback, _listener);

                        // Wait until a connection is made before continuing.
                        AllDone.WaitOne();

                    }
                }
                catch (Exception e)
                {
                    count++;
                    Logger.Error(e, "Command listener error");
                    if (count == 2)
                    {
                        _stopped = true;
                        break;
                    }
                    Thread.Sleep(10000); // wait 10 seconds
                }

            } // while
            Logger.Information("CommandListenerThread has come to an end");
        }

        void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue.
            AllDone.Set();

            // Get the socket that handles the client request.
            var listener = (Socket)ar.AsyncState!;
            var handler = listener.EndAccept(ar);

            var commandProcessing = new CommandProcessingThread(handler);
            commandProcessing.Start();
        }


        private Thread? _thread;
        internal void Start()
        {
            _thread = new Thread(Run);
            _thread.Start();
        }

        public void Stop()
        {
            _stopped = true;
            _listener?.Dispose();
            _thread?.Join(10000); // wait a max of 10 seconds to shut this down
        }
    }

    // class
}