using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MyMonitorHub.Domain.Util
{
    public class AgentConnections
    {
        private static ILogger _logger = NullLogger.Instance;
        public static readonly AgentConnections Current = new AgentConnections();

        private readonly object _lockObject = new object();
        private readonly Dictionary<string, AgentConnection> _connections = new Dictionary<string, AgentConnection>();
        private readonly Dictionary<int, string> _guidDeviceCrossReference = new Dictionary<int, string>();

        public static void Initialize(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<AgentConnections>();
        }

        private AgentConnections()
        {
        }

        public List<AgentConnection> GetAllBrowserConnectionsForAccount(int accountId)
        {
            lock (_lockObject)
            {
                return (from s in _connections.Values
                    where
                        s.AccountId == accountId && s.ConnectionType == ConnectionType.Browser &&
                        s.Status == ConnectionStatus.Connected
                    select s).ToList();
            }
        }

        public void Add(AgentConnection agentConnection)
        {
            _logger.LogDebug("Add({0} - Type: {1})", agentConnection.Guid, agentConnection.ConnectionType);
            lock (_lockObject)
            {
                _connections[agentConnection.Guid] = agentConnection;
                agentConnection.Status = ConnectionStatus.Connected;
                if (agentConnection.ConnectionType == ConnectionType.Agent)
                {
                    NotifyAllBrowsers(agentConnection.AccountId, agentConnection.DeviceId, Notification.Connected);

                    // create a cross reference between guid and deviceId
                    _guidDeviceCrossReference[agentConnection.DeviceId] = agentConnection.Guid;
                }
            } // lock
        }

        public void Unregister(string guid)
        {
            _logger.LogDebug("Unregister({0})", guid);
            lock (_lockObject)
            {
                if (_connections.ContainsKey(guid))
                {
                    var deviceConnection = _connections[guid];
                    if (deviceConnection.ConnectionType == ConnectionType.Agent)
                    {
                        NotifyAllBrowsers(deviceConnection.AccountId, deviceConnection.DeviceId,
                            Notification.Disconnected);

                        var deviceId = deviceConnection.DeviceId;
                        _guidDeviceCrossReference.Remove(deviceId);
                    }
                    _connections.Remove(guid);
                }
            } // lock
        }

        public void Unregister(WebSocket socket)
        {
            _logger.LogDebug("Unregistering from socket");
            lock (_lockObject)
            {
                var deviceConnection = (from x in _connections.Values
                    where x.Socket == socket
                    select x).FirstOrDefault();

                if (deviceConnection == null)
                {
                    _logger.LogDebug("socket not found");
                    return;
                }

                _logger.LogDebug(string.Format("Disconnected: " + deviceConnection.Guid));
                if (deviceConnection.ConnectionType == ConnectionType.Agent)
                {
                    NotifyAllBrowsers(deviceConnection.AccountId, deviceConnection.DeviceId, Notification.Disconnected);

                    var deviceId = deviceConnection.DeviceId;
                    _guidDeviceCrossReference.Remove(deviceId);
                }
                _connections.Remove(deviceConnection.Guid);
            } // lock
        }

        public AgentConnection GetConnectionByDeviceId(int deviceId)
        {
            try
            {
                var guid = _guidDeviceCrossReference[deviceId];
                return GetConnectionByGuid(guid);
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
        }

        public AgentConnection GetConnectionByGuid(string guid)
        {
            AgentConnection agentConnection;
            try
            {
                agentConnection = _connections[guid];
                if (agentConnection == null || agentConnection.Status != ConnectionStatus.Connected) return null;
            }
            catch (KeyNotFoundException)
            {
                return null;
            }

            return agentConnection;
        }

        public List<AgentConnection> GetStatus()
        {
            lock (_lockObject)
            {
                return (from x in _connections.Values
                    select new AgentConnection
                    {
                        Guid = x.Guid,
                        Status = x.Status,
                        AccountId = x.AccountId,
                        ConnectionType = x.ConnectionType,
                        DeviceId = x.DeviceId,
                        Username = x.Username
                    })
                    .ToList();
            } // lock
        }

        private enum Notification
        {
            Connected,
            Disconnected
        }

        private void NotifyAllBrowsers(int accountId, int fromId, Notification notification)
        {
            _logger.LogDebug("About to notify all browsers of a connection by id: " + fromId);
            var browserConnections = Current.GetAllBrowserConnectionsForAccount(accountId);
            _logger.LogDebug("Browser connections to notify: " + browserConnections.Count);
            foreach (var toConnection in browserConnections)
            {
                var obj = new
                {
                    Success = true,
                    Command = "Connection",
                    Data = notification.ToString()
                };

                var json = JsonSerializer.Serialize(obj);
                var message = $"{fromId}|{json}";
                try
                {
                    toConnection.Socket.SendAsync(
                        new ArraySegment<byte>(Encoding.UTF8.GetBytes(message)), WebSocketMessageType.Text, true,
                        CancellationToken.None).Wait();
                }
                catch (Exception e)
                {
                    _logger.LogError("exception notifying all browsers: " + e.Message);
                }
            }
        }

    } // class

    public enum ConnectionType
    {
        All,
        Agent,
        Browser
    }

    public enum ConnectionStatus
    {
        Connected,
        Disconnected
    }

    public class AgentConnection
    {
        public ConnectionType ConnectionType { get; set; }
        public string Guid { get; set; }
        public WebSocket Socket { get; set; }
        public ConnectionStatus Status { get; set; }

        public int AccountId { get; set; }
        public int DeviceId { get; set; }
        public string Username { get; set; }
    }
}
