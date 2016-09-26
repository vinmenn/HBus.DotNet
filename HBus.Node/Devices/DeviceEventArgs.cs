using System;

namespace HBus.Nodes.Devices
{
    /// <summary>
    /// Device event arguments
    /// Used with event handlers
    /// </summary>
    public class DeviceEventArgs : EventArgs
    {
        public DeviceEventArgs(DeviceEvent deviceEvent)
        {
            //Name = deviceName;
            Event = deviceEvent;
        }

        //public string Name{ get; set; }
        public DeviceEvent Event { get; set; }
    }
}