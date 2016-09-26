using System;

namespace HBus.Nodes.Exceptions
{
    public class NodesMapException : Exception
    {
        public NodesMapException(string message)
            : base(message)
        {
        }
        public NodesMapException(string message, Exception exception)
            : base(message, exception)
        {
        }
    }
}