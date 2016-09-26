using System;

namespace HBus.Nodes.Exceptions
{
    public class DeviceNotFoundException : Exception
    {
        public DeviceNotFoundException(string message)
            : base(message)
        {
        }
        public DeviceNotFoundException(string message, Exception exception)
            : base(message, exception)
        {
        }
    }
}