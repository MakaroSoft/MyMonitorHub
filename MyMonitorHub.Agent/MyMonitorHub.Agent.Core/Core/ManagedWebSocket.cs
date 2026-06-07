using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MyMonitorHub.Agent.Core
{
    public enum ManagedWebSocketState
    {
        None,
        Connecting,
        Open,
        CloseSent,
        CloseReceived,
        Closed,
        Aborted
    }

    public class WebSocketMessageReceivedEventArgs : EventArgs
    {
        public string Message { get; }
        public WebSocketMessageReceivedEventArgs(string message) => Message = message;
    }

    public class WebSocketDataReceivedEventArgs : EventArgs
    {
        public byte[] Data { get; }
        public WebSocketDataReceivedEventArgs(byte[] data) => Data = data;
    }

    public class WebSocketErrorEventArgs : EventArgs
    {
        public Exception Exception { get; }
        public WebSocketErrorEventArgs(Exception ex) => Exception = ex;
    }

    /// <summary>
    /// Event-based WebSocket wrapper around System.Net.WebSockets.ClientWebSocket,
    /// replacing the discontinued WebSocket4Net library.
    /// </summary>
    public class ManagedWebSocket : IDisposable
    {
        private ClientWebSocket? _socket;
        private readonly string _url;
        private readonly IReadOnlyDictionary<string, string>? _headers;
        private CancellationTokenSource? _cts;
        private bool _disposed;

        public event EventHandler? Opened;
        public event EventHandler<WebSocketErrorEventArgs>? Error;
        public event EventHandler? Closed;
        public event EventHandler<WebSocketMessageReceivedEventArgs>? MessageReceived;
        public event EventHandler<WebSocketDataReceivedEventArgs>? DataReceived;

        public ManagedWebSocketState State
        {
            get
            {
                if (_socket == null) return ManagedWebSocketState.None;
                return _socket.State switch
                {
                    WebSocketState.Connecting    => ManagedWebSocketState.Connecting,
                    WebSocketState.Open          => ManagedWebSocketState.Open,
                    WebSocketState.CloseSent     => ManagedWebSocketState.CloseSent,
                    WebSocketState.CloseReceived => ManagedWebSocketState.CloseReceived,
                    WebSocketState.Closed        => ManagedWebSocketState.Closed,
                    WebSocketState.Aborted       => ManagedWebSocketState.Aborted,
                    _                            => ManagedWebSocketState.None
                };
            }
        }

        public ManagedWebSocket(string url, SslProtocols sslProtocols = SslProtocols.Tls12, IReadOnlyDictionary<string, string>? headers = null)
        {
            _url = url;
            _headers = headers;
        }

        public void Open()
        {
            _cts = new CancellationTokenSource();
            Task.Run(() => ConnectAndReceiveAsync(_cts.Token));
        }

        private async Task ConnectAndReceiveAsync(CancellationToken ct)
        {
            _socket = new ClientWebSocket();
            if (_headers != null)
                foreach (var kvp in _headers)
                    _socket.Options.SetRequestHeader(kvp.Key, kvp.Value);
            try
            {
                await _socket.ConnectAsync(new Uri(_url), ct);
                Opened?.Invoke(this, EventArgs.Empty);
                await ReceiveLoopAsync(ct);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Error?.Invoke(this, new WebSocketErrorEventArgs(ex));
            }
            finally
            {
                if (!ct.IsCancellationRequested)
                {
                    Closed?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private async Task ReceiveLoopAsync(CancellationToken ct)
        {
            var buffer = new byte[65536];
            while (!ct.IsCancellationRequested && _socket!.State == WebSocketState.Open)
            {
                using var memoryStream = new MemoryStream();
                WebSocketReceiveResult result;
                do
                {
                    result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                        return;
                    }
                    memoryStream.Write(buffer, 0, result.Count);
                } while (!result.EndOfMessage);

                var data = memoryStream.ToArray();
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(data);
                    MessageReceived?.Invoke(this, new WebSocketMessageReceivedEventArgs(message));
                }
                else
                {
                    DataReceived?.Invoke(this, new WebSocketDataReceivedEventArgs(data));
                }
            }
        }

        public void Send(string message)
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            var socket = _socket;
            if (socket == null || socket.State != WebSocketState.Open) return;
            Task.Run(async () =>
            {
                try
                {
                    await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    Error?.Invoke(this, new WebSocketErrorEventArgs(ex));
                }
            });
        }

        public void Send(byte[] bytes, int offset, int length)
        {
            var socket = _socket;
            if (socket == null || socket.State != WebSocketState.Open) return;
            var segment = new ArraySegment<byte>(bytes, offset, length);
            Task.Run(async () =>
            {
                try
                {
                    await socket.SendAsync(segment, WebSocketMessageType.Binary, true, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    Error?.Invoke(this, new WebSocketErrorEventArgs(ex));
                }
            });
        }

        public void Close()
        {
            try
            {
                _cts?.Cancel();
                var socket = _socket;
                if (socket?.State == WebSocketState.Open)
                {
                    Task.Run(() => socket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None));
                }
            }
            catch { }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _cts?.Cancel();
            _cts?.Dispose();
            _socket?.Dispose();
        }
    }
}
