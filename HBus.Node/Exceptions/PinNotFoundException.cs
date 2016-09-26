using System;

namespace HBus.Nodes.Exceptions
{
    public class PinNotFoundException : Exception
    {
        public PinNotFoundException(string message)
            : base(message)
        {
        }
        public PinNotFoundException(string message, Exception exception)
            : base(message, exception)
        {
        }
    }
}