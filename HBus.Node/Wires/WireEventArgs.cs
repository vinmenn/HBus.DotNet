using System;
using HBus.Nodes.Pins;

namespace HBus.Nodes.Wires
{
    public class WireEventArgs : EventArgs
    {
        public WireEventArgs(PinEvent evtSource)
        {
            Source = evtSource;
        }

        public PinEvent Source { get; set; }
    }
}