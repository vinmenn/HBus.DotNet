using System;

namespace HBus.Nodes.Exceptions
{
    public class NodeConfigurationException : Exception
    {
        public NodeConfigurationException(string s)
        {
            throw new NotImplementedException();
        }
        public NodeConfigurationException(string message, Exception exception)
            : base(message, exception)
        {
        }
    }
}