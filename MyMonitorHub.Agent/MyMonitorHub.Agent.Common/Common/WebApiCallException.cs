using System;

namespace MyMonitorHub.Agent.Common
{
    public class WebApiCallException : Exception
    {
        public WebApiCallException(string message) : base(message)
        {
        }
    }
}