using System;

namespace HBus.Nodes.Devices
{
    /// <summary>
    /// Device common interface
    /// </summary>
    public class Device
    {
        ////Local properties
        /// <summary>
        /// Device  id (PK)
        /// </summary>
        public uint Id { get; set; }
        /// <summary>
        /// Node id (FK)
        /// </summary>
        public uint NodeId { get; set; }

        //Shared properties
        /// <summary>
        /// Index of device 
        /// </summary>
        public byte Index { get; set; }
        /// <summary>
        /// Name of specific device
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// HBus address of device
        /// </summary>
        public Address Address { get; set; }
        /// <summary>
        /// Device extended description
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// Device real location
        /// </summary>
        public string Location { get; set; }
        /// <summary>
        /// Device type
        /// </summary>
        //public DeviceType Type { get; set; }
        /// <summary>
        /// Device class
        /// </summary>
        /// <example>Shutter, Relay, etc</example>
        public string Class { get; protected internal set; }
        /// <summary>
        /// Device hardware
        /// </summary>
        /// <example>DS18b20</example>
        public string Hardware { get; protected internal set; }
        /// <summary>
        /// Device software version
        /// </summary>
        public string Version { get; protected internal set; }
        /// <summary>
        /// Device actions
        /// </summary>
        public virtual string[] Actions { get; protected internal set; }
        /// <summary>
        /// Device current status
        /// </summary>
        public virtual string Status { get; protected set; }

        public virtual bool ExecuteAction(DeviceAction action)
        {
            throw new NotImplementedException();
        }

        public virtual bool IsActive()
        {
            throw new NotImplementedException();
        }

        public virtual event EventHandler<DeviceEventArgs> DeviceEvent;

    }
}