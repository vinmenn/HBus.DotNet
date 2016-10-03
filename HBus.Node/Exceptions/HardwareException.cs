using System;
using System.Runtime.Serialization;

namespace HBus.Nodes.Hardware
{
  [Serializable]
  public class HardwareException : Exception
  {
    public HardwareException()
    {
    }

    public HardwareException(string message) : base(message)
    {
    }

    public HardwareException(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected HardwareException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
  }
}