using System;

namespace HBus.Nodes.Exceptions
{
    public class PinNotConfiguredException : Exception
    {
        public PinNotConfiguredException(string message)
            : base(message)
        {
        }
        public PinNotConfiguredException(string message, Exception exception)
            : base(message, exception)
        {
        }
    }
}