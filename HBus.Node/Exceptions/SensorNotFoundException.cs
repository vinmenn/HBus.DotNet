using System;

namespace HBus.Nodes.Exceptions
{
    public class SensorNotFoundException : Exception
    {
        public SensorNotFoundException(string message)
            : base(message)
        {
        }
        public SensorNotFoundException(string message, Exception exception)
            : base(message, exception)
        {
        }
    }
}