using System;

namespace HBus.Nodes.Exceptions
{
    public class WireException : Exception
    {
        public WireException(string message) : base(message)
        {
            //
        }
        public WireException(string message, Exception ex)
            : base(message, ex)
        {
            //
        }
    }
}