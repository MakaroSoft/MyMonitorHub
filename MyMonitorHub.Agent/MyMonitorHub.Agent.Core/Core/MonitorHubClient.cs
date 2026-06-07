using System;
using System.Security.Authentication;
using System.ServiceProcess;
using System.Text.Json;
using System.Threading;
using MyMonitorHub.Agent.Common;
using Serilog;

namespace MyMonitorHub.Agent.Core
{
    public class MonitorHubClient
    {
        private static readonly ILogger Logger = Log.ForContext<MonitorHubClient>();

        private readonly int _accountId;
        private readonly int _deviceId;

        private ManagedWebSocket? _websocket;

        private bool _stopped;

        public MonitorHubClient(int accountId, int deviceId)
        {
            _accountId = accountId;
            _deviceId = deviceId;
        }

        public void Start()
        {
            try
            {
                Logger.Information("Start");
                var accessToken = MonitorProfile.TokenManager.GetAccessToken();
                var headers = new System.Collections.Generic.Dictionary<string, string>
                {
                    { "Authorization", $"Bearer {accessToken}" }
                };
                _websocket = new ManagedWebSocket(
                    $"{MonitorProfile.Current.WebSocketAddress}/api/Hub/GetForAgent",
                    SslProtocols.Tls12,
                    headers);
                _websocket.Opened += websocket_Opened;
                _websocket.Error += websocket_Error;
                _websocket.Closed += websocket_Closed;
                _websocket.MessageReceived += websocket_MessageReceived;
                _websocket.DataReceived += websocket_DataReceived;
                _websocket.Open();
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
                if (!_stopped)
                {
                    // websocket_Closed will never fire because no websocket was created,
                    // so we must schedule the retry ourselves from a new thread to avoid
                    // growing the call stack on every failed attempt.
                    var retryThread = new Thread(() =>
                    {
                        Thread.Sleep(10000);
                        Start();
                    });
                    retryThread.IsBackground = true;
                    retryThread.Start();
                }
            }
        }

        private void websocket_DataReceived(object? sender, WebSocketDataReceivedEventArgs e)
        {
            Logger.Information("Not implemented yet");
        }

        public class Puller
        {
            private readonly string _text;
            private int _lastPosition = -1;
            public Puller(string text)
            {
                _text = text;
            }

            public string Pull()
            {
                if (_lastPosition + 1 == _text.Length)
                {
                    return "";
                }
                var index = _text.IndexOf("|", _lastPosition + 1, StringComparison.Ordinal);
                if (index == -1)
                {
                    var result = _text.Substring(_lastPosition + 1);
                    _lastPosition = _text.Length - 1;
                    return result;
                }
                var result2 = _text.Substring(_lastPosition + 1, (index - _lastPosition) - 1);
                _lastPosition = index;
                return result2;
            }

            public string Tail
            {
                get
                {
                    if (_lastPosition + 1 == _text.Length)
                    {
                        return "";
                    }
                    return _text.Substring(_lastPosition + 1);
                }
            }
        }

        private void websocket_MessageReceived(object? sender, WebSocketMessageReceivedEventArgs e)
        {
            if (e.Message.StartsWith("Registered: ", StringComparison.Ordinal))
            {
                var connectionId = e.Message.Substring("Registered: ".Length);
                Logger.Information("Registered with hub. ConnectionId: {0}", connectionId);
                return;
            }

            var index = e.Message.IndexOf("|", StringComparison.Ordinal);
            if (index == -1)
            {
                Logger.Warning("Received WebSocket message with unexpected format (no '|' separator); message ignored.");
            }
            else
            {
                var puller = new Puller(e.Message);

                var from = puller.Pull();
                if (from == "server")
                {
                    Logger.Debug("Received from {0}", from);
                    return;
                }

                var command = puller.Pull();
                var tail = puller.Tail;

                Logger.Debug("Received from: {0} - command: {1}", from, command);
                switch (command)
                {
                    case "service":
                        var codeId = puller.Pull();
                        var serviceName = puller.Pull();
                        var action = puller.Pull();
                        OnService(command, from, codeId, serviceName, action);
                        break;
                    case "services":
                        var code = puller.Pull();
                        OnServices(command, from, code);
                        break;
                    case "topCpu":
                        if (!int.TryParse(puller.Pull(), out var take) || take < 1 || take > 50)
                        {
                            SendFailure(from, command, "Invalid take value.");
                            break;
                        }
                        OnTopCpu(command, from, take);
                        break;
                    case "sysInfo":
                        Logger.Information("sysInfo keyword found");
                        OnSysInfo(command, from);
                        break;
                    default:
                        SendFailure(from, command, "Unknown Command");
                        break;
                }
            }
        }

        private void OnService(string command, string toGuid, string code, string serviceName, string action)
        {
            var allowedNames = MonitorProfile.Current.ServiceNames;
            if (!allowedNames.Contains(serviceName))
            {
                Logger.Warning("Rejected service command for unlisted service: {0}", serviceName);
                SendFailure(toGuid, command, $"Service '{serviceName}' is not in the configured service list.");
                return;
            }

            try
            {
                var controller = getServiceController(serviceName);
                if (action == "start")
                {
                    var timeout = TimeSpan.FromMilliseconds(20000);
                    controller.Start();
                    try
                    {
                        controller.WaitForStatus(ServiceControllerStatus.Running, timeout);
                    }
                    catch (System.ServiceProcess.TimeoutException)
                    {
                    }
                }
                else if (action == "stop")
                {
                    var timeout = TimeSpan.FromMilliseconds(20000);
                    controller.Stop();
                    try
                    {
                        controller.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
                    }
                    catch (System.ServiceProcess.TimeoutException)
                    {
                    }
                }
                else
                {
                    Logger.Warning("Rejected unknown service action '{0}' for service '{1}'", action, serviceName);
                    SendFailure(toGuid, command, $"Unknown service action '{action}'.");
                    return;
                }
                controller.Refresh();
                var isRunning = controller.Status == ServiceControllerStatus.Running ||
                                controller.Status == ServiceControllerStatus.StartPending;
                var response = new { Code = code, Checked = isRunning, StatusDesc = controller.Status.ToString() };
                SendSuccess(toGuid, command, response);
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                SendFailure(toGuid, command, e.Message);
            }
        }

        private void OnServices(string command, string toGuid, string code)
        {
            try
            {
                var configuredNames = MonitorProfile.Current.ServiceNames;
                var serviceList = new System.Collections.Generic.List<object>();
                foreach (var name in configuredNames)
                {
                    try
                    {
                        var svc = new ServiceController { MachineName = ".", ServiceName = name };
                        var isRunning = svc.Status == ServiceControllerStatus.Running ||
                                        svc.Status == ServiceControllerStatus.StartPending;
                        serviceList.Add(new
                        {
                            Code = name,
                            Description = svc.DisplayName,
                            Name = name,
                            Checked = isRunning,
                            StatusDesc = svc.Status.ToString()
                        });
                    }
                    catch (Exception)
                    {
                        serviceList.Add(new
                        {
                            Code = name,
                            Description = name,
                            Name = name,
                            Checked = false,
                            StatusDesc = "Not Found"
                        });
                    }
                }
                SendSuccess(toGuid, command, serviceList);
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                SendFailure(toGuid, command, e.Message);
            }
        }

        private ServiceController getServiceController(string serviceName)
        {
            return new ServiceController { MachineName = ".", ServiceName = serviceName };
        }

        private void OnSysInfo(string command, string toGuid)
        {
            try
            {
                Logger.Debug("received sysInfo request");
                var result = new CollectSysInfo().Collect();
                SendSuccess(toGuid, command, result);
                Logger.Debug("Sent SysInfoResponse");
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                if (e.InnerException != null)
                {
                    Logger.Error("Inner Exception: {Message}", e.InnerException.Message);
                }
                SendFailure(toGuid, command, e.Message);
            }
        }

        private void OnTopCpu(string command, string toGuid, int take)
        {
            try
            {
                Logger.Debug("received topCpu request");
                var result = Monitor.ProcessCollector!.TopCpu(take);
                SendSuccess(toGuid, command, result);
                Logger.Debug("Sent TopCpuResponse");
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                if (e.InnerException != null)
                {
                    Logger.Error("Inner Exception: {Message}", e.InnerException.Message);
                }
                SendFailure(toGuid, command, e.Message);
            }
        }

        private void Send(string sendTo, string text)
        {
            try
            {
                _websocket?.Send(string.Format("Request|{0}|{1}", sendTo, text));
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }
        }

        private void SendFailure(string toGuid, string command, string message)
        {
            Logger.Error($"SendFailure> {toGuid}, command = {command}");
            var obj = new
            {
                Success = false,
                Command = command,
                FailureReason = message
            };
            var json = JsonSerializer.Serialize(obj);
            Send(toGuid, json);
        }

        private void SendSuccess(string toGuid, string command, object data)
        {
            var obj = new
            {
                Success = true,
                Command = command,
                Data = data
            };
            var json = JsonSerializer.Serialize(obj);
            Send(toGuid, json);
        }

        private void websocket_Closed(object? sender, EventArgs e)
        {
            try
            {
                if (_stopped)
                {
                    Logger.Information("Stopped");
                    return;
                }
                Logger.Information("Closed");
                Thread.Sleep(1000);
                _websocket?.Dispose();
                Thread.Sleep(9000);
                Start();
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }
        }

        private void websocket_Error(object? sender, WebSocketErrorEventArgs e)
        {
            Logger.Error(e.Exception.Message);
        }

        private void websocket_Opened(object? sender, EventArgs e)
        {
            Logger.Information("Connected.");
        }

        public void Stop()
        {
            _stopped = true;
            Logger.Information("Stop");
            _websocket?.Dispose();
        }
    }
}
