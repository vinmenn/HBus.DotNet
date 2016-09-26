using System;

namespace HBus.Nodes.Pins
{
    /// <summary>
    /// Pin event arguments
    /// </summary>
    public class PinEventArgs : EventArgs
    {
        public PinEvent Event { get; set; }
    }
}