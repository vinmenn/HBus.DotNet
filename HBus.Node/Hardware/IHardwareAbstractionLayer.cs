using HBus.Nodes.Pins;

namespace HBus.Nodes.Hardware
{
    /// <summary>
    /// Simple hardware abstraction layer
    /// Implement this interface with specific hardware
    /// </summary>
    public interface IHardwareAbstractionLayer
    {
        HardwareInfo Info { get;  }
        int Read(string pin, PinTypes type);
        void Write(string pin, PinTypes type, int value);
        void Update();
    }
}