using System;

namespace HBus.Nodes.Exceptions
{
    public class HBusTimeoutException : Exception
    {
        public HBusTimeoutException(string message)
            : base(message)
        {
        }
        public HBusTimeoutException(string message, Exception exception)
            : base(message, exception)
        {
        }
    }
}