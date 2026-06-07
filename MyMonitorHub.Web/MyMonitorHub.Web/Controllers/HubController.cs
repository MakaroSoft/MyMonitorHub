using System;
using System.Globalization;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MyMonitorHub.Common.Util;
using MyMonitorHub.Domain.Entity;
using MyMonitorHub.Domain.Interface;
using MyMonitorHub.Domain.Security;
using MyMonitorHub.Domain.Service;
using MyMonitorHub.Domain.Util;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MyMonitorHub.Web.Controllers
{
    public class HubController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;

        private readonly IDbContextScopeFactory _contextScopeFactory;
        private readonly WebSocketTicketFactory _webSocketTicketFactory;
        private readonly IConfiguration _configuration;

        public HubController(IDbContextScopeFactory contextScopeFactory, WebSocketTicketFactory webSocketTicketFactory, ILoggerFactory loggerFactory, IConfiguration configuration)
        {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<HubController>();
            _contextScopeFactory = contextScopeFactory;
            _webSocketTicketFactory = webSocketTicketFactory;
            _configuration = configuration;
        }

        // This is for the original web socket communication (aka commonWebSocket)
        [Authorize]
        [HttpGet("api/Hub/Get")]
        public async Task Get()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                var socket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                await ProcessWsChat(socket);
            }
            else
            {
                HttpContext.Response.StatusCode = 400;
            }
        }

        // Agent hub connection — identity is established via the JWT Bearer Authorization header on the upgrade request
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("api/Hub/GetForAgent")]
        public async Task GetForAgent()
        {
            if (!HttpContext.WebSockets.IsWebSocketRequest)
            {
                HttpContext.Response.StatusCode = 400;
                return;
            }

            var socket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            await ProcessAgentWsChat(socket);
        }

        private async Task ProcessAgentWsChat(WebSocket socket)
        {
            var accountId = int.Parse(User.FindFirst("accountId")!.Value);
            var deviceId = int.Parse(User.FindFirst("deviceId")!.Value);
            var connectionId = Guid.NewGuid().ToString();

            var agentConn = new AgentConnection
            {
                ConnectionType = ConnectionType.Agent,
                Socket = socket,
                Guid = connectionId,
                AccountId = accountId,
                DeviceId = deviceId
            };
            AgentConnections.Current.Add(agentConn);

            _logger.LogDebug("ProcessAgentWsChat> registered accountId={0} deviceId={1} guid={2}", accountId, deviceId, connectionId);

            var registeredMsg = Encoding.UTF8.GetBytes("Registered: " + connectionId);
            await socket.SendAsync(new ArraySegment<byte>(registeredMsg), WebSocketMessageType.Text, true, CancellationToken.None);

            var buffer = new ArraySegment<byte>(new byte[1024]);
            try
            {
                while (true)
                {
                    if (socket.State != WebSocketState.Open)
                    {
                        AgentConnections.Current.Unregister(connectionId);
                        return;
                    }

                    var userMessage = "";
                    WebSocketReceiveResult webSocketResult;
                    do
                    {
                        try
                        {
                            webSocketResult = await socket.ReceiveAsync(buffer, CancellationToken.None);
                            if (webSocketResult.MessageType == WebSocketMessageType.Close ||
                                webSocketResult.MessageType == WebSocketMessageType.Binary)
                            {
                                AgentConnections.Current.Unregister(connectionId);
                                return;
                            }
                            if (buffer.Array != null)
                                userMessage += Encoding.UTF8.GetString(buffer.Array, 0, webSocketResult.Count);
                        }
                        catch (Exception x)
                        {
                            _logger.LogError($"ProcessAgentWsChat> {x.GetType().Name} | {x.Message} | guid: {connectionId}");
                            AgentConnections.Current.Unregister(connectionId);
                            return;
                        }
                    } while (!webSocketResult.EndOfMessage);

                    _logger.LogDebug("ProcessAgentWsChat> Received: {0}", userMessage);

                    var puller = new Puller(userMessage);
                    var command = puller.Pull();
                    if (command == "Request")
                        await DoRequestAsync(socket, puller, connectionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ProcessAgentWsChat> {ExceptionType}", ex.GetType().Name);
                AgentConnections.Current.Unregister(connectionId);
            }
        }

        private async Task ProcessWsChat(WebSocket socket)
        {
            var myTicket = "";
            var userMessage = "";
            // Registration deadline: if RegisterBrowser does not arrive within this window the connection is closed.
            using var registrationCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            try
            {
                var buffer = new ArraySegment<byte>(new byte[1024]);
                _logger.LogDebug("ProcessWsChat> authenticated = {0}", User.Identity?.IsAuthenticated);

                while (true)
                {
                    userMessage = "";

                    if (socket.State == WebSocketState.Open)
                    {
                        WebSocketReceiveResult webSocketResult;
                        do
                        {
                            try
                            {
                                var receiveToken = myTicket == "" ? registrationCts.Token : CancellationToken.None;
                                webSocketResult = await socket.ReceiveAsync(buffer, receiveToken);
                                if (webSocketResult.MessageType == WebSocketMessageType.Close)
                                {
                                    AgentConnections.Current.Unregister(myTicket);
                                    return;
                                }
                                if (webSocketResult.MessageType == WebSocketMessageType.Binary)
                                {
                                    AgentConnections.Current.Unregister(myTicket);
                                    return;
                                }
                                if (buffer.Array != null)
                                    userMessage += Encoding.UTF8.GetString(buffer.Array, 0, webSocketResult.Count);
                            }
                            catch (OperationCanceledException)
                            {
                                _logger.LogWarning("ProcessWsChat> Registration timeout, closing connection");
                                try
                                {
                                    await socket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Registration timeout", CancellationToken.None);
                                }
                                catch { }
                                return;
                            }
                            catch (Exception x)
                            {
                                _logger.LogError($"Exception: {x.GetType().Name} | {x.Message} | ticket: {myTicket}");
                                AgentConnections.Current.Unregister(myTicket);
                                return;
                            }
                        } while (!webSocketResult.EndOfMessage);

                        _logger.LogDebug("ProcessWsChat> Received: " + userMessage);

                        var puller = new Puller(userMessage);
                        var command = puller.Pull();
                        switch (command)
                        {
                            case "RegisterBrowser":
                                var ticket = puller.Pull();
                                var ticketObject = _webSocketTicketFactory.Validate(ticket);
                                if (ticketObject == null)
                                {
                                    _logger.LogError("Invalid ticket");
                                    var errMsg = Encoding.UTF8.GetBytes("Invalid Ticket");
                                    await socket.SendAsync(new ArraySegment<byte>(errMsg), WebSocketMessageType.Text, true, CancellationToken.None);
                                    return;
                                }
                                var browserConn = new AgentConnection
                                {
                                    ConnectionType = ConnectionType.Browser, Socket = socket, Guid = ticket,
                                    Username = ticketObject.Username, AccountId = ticketObject.AccountId
                                };
                                myTicket = ticket;
                                AgentConnections.Current.Add(browserConn);
                                var regMsg = Encoding.UTF8.GetBytes("Registered: " + ticket);
                                await socket.SendAsync(new ArraySegment<byte>(regMsg), WebSocketMessageType.Text, true, CancellationToken.None);
                                break;

                            case "Request":
                                await DoRequestAsync(socket, puller, myTicket);
                                break;

                            default:
                                _logger.LogWarning("ProcessWsChat> Unrecognised command: {0}", command);
                                break;
                        }
                    }
                    else
                    {
                        _logger.LogInformation($"ProcessWsChat> status is now: {socket.State}");
                        AgentConnections.Current.Unregister(myTicket);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ProcessWsChat> {ExceptionType}", ex.GetType().Name);
                AgentConnections.Current.Unregister(myTicket);
            }
        }

        private async Task DoRequestAsync(WebSocket socket, Puller puller, string myTicket)
        {
            try
            {
                var to = puller.Pull();
                var theMessage = puller.Tail;
                var me = AgentConnections.Current.GetConnectionByGuid(myTicket);

                AgentConnection? toConnection;
                if (me.ConnectionType == ConnectionType.Browser)
                    toConnection = AgentConnections.Current.GetConnectionByDeviceId(int.Parse(to));
                else
                    toConnection = AgentConnections.Current.GetConnectionByGuid(to);

                if (toConnection == null) { await SendFailureAsync(socket, "Requested party is not connected"); return; }

                var myConnection = AgentConnections.Current.GetConnectionByGuid(myTicket);
                if (myConnection.AccountId != toConnection.AccountId) { await SendFailureAsync(socket, "Permission denied"); return; }

                string fromId;
                if (myConnection.ConnectionType == ConnectionType.Agent)
                {
                    if (toConnection.ConnectionType != ConnectionType.Browser) { await SendFailureAsync(socket, "Permission denied"); return; }
                    fromId = myConnection.DeviceId.ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    if (toConnection.ConnectionType != ConnectionType.Agent) { await SendFailureAsync(socket, "Permission denied"); return; }

                    var command = theMessage.Split('|')[0];
                    if (command == "service" && !Authorizer.Authorize(Permissions.CanStopStartServices))
                    {
                        _logger.LogWarning("DoRequestAsync> Permission denied for service command. User: {0}", myConnection.Username);
                        await SendFailureAsync(socket, Permissions.CanStopStartServices.FailMessage);
                        return;
                    }

                    fromId = myTicket;
                }

                var msg = Encoding.UTF8.GetBytes($"{fromId}|{theMessage}");
                await toConnection.Socket!.SendAsync(new ArraySegment<byte>(msg), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch (Exception e) { _logger.LogError("DoRequestAsync exception: " + e.Message); }
        }

        private async Task SendFailureAsync(WebSocket socket, string message)
        {
            var json = JsonSerializer.Serialize(new { Success = false, command = "", FailureReason = message });
            var bytes = Encoding.UTF8.GetBytes("server|" + json);
            await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }

}
