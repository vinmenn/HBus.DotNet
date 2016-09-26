using System;

namespace HBus.Nodes.Exceptions
{
    public class NameNotFoundException : Exception
    {
        public NameNotFoundException(string message)
            : base(message)
        {
        }
        public NameNotFoundException(string message, Exception exception)
            : base(message, exception)
        {
        }
    }
}