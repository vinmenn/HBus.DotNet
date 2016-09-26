using System;

namespace HBus.Nodes.Wires
{
    [Flags]
    public enum WireTriggers
    {
        None = 0,
        OnChange = 1,
        OnActivate = 2,
        OnDeactivate = 4
    }
}