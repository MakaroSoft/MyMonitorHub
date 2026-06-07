using System;

namespace MyMonitorHub.Domain.Exceptions
{
    public class WarningException : Exception
    {
        public WarningException(string message)
            : base(message)
        {
        }
    }
}
