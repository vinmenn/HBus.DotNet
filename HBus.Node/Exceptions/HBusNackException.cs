using System;

namespace HBus.Nodes.Exceptions
{
    public class HBusNackException : Exception
    {
        public HBusNackException(string message)
            : base(message)
        {
        }
        public HBusNackException(string message, Exception exception)
            : base(message, exception)
        {
        }
    }
}