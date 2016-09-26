using System;

namespace HBus.Nodes.Exceptions
{
    public class NodeNotFoundException : Exception
    {
        public NodeNotFoundException(string message)
            : base(message)
        {
        }
        public NodeNotFoundException(string message, Exception exception)
            : base(message, exception)
        {
        }
    }
}