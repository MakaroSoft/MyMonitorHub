using System;
using MyMonitorHub.Common.WebApi;
using MyMonitorHub.Agent.Client;
using MyMonitorHub.Agent.Core;
using Serilog;

namespace MyMonitorHub.Agent.Plugins
{
    public class CommandWatch : AbstractPlugin
    {
        private static readonly ILogger Logger = Log.ForContext<CommandWatch>();

        private const string Category = "CMD PRC";
        private const string SubCategory = "Thread";
        private const string Name = "Ping";

        protected override void Execute()
        {
            var client = new EventBroadcastClient(Core.Monitor.Port);
            try
            {
                client.Ping(); // now throws an error instead of supressing it
                Logger.Debug("ok");

                SayStatus(Category, SubCategory, Name, "OK", EventType.Ok); // should not throw an error
            }
            catch (Exception e)
            {
                HandleFail(e);
            }
        }

        private void HandleFail(Exception e)
        {
            var ex = e.InnerException ?? e;

            var message = ex.Message;
            if (string.IsNullOrEmpty(message))
            {
                message = ex.ToString();
            }
            SayStatus(Category, SubCategory, Name, message, EventType.Fail);
            Logger.Error(ex, "CommandWatch execution failed");
        }
    }
}