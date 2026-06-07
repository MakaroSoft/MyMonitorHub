using System;

namespace MyMonitorHub.Domain.Exceptions
{
    public class SecurityException : Exception
    {
        public SecurityException(string message)
            : base(message)
        {
        }
    }
}
