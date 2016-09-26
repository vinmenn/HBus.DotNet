using System;

namespace HBus.Nodes.Exceptions
{
    public class CommandFailedException : Exception
    {
        public CommandFailedException(string message)
            : base(message)
        {
        }
        public CommandFailedException(string message, Exception exception)
            : base(message, exception)
        {
        }
    }
}