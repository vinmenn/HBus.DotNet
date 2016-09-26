using HBus.Utilities;

namespace HBus.Node.Devices
{
    public struct DeviceStatus
    {
        /// <summary>
        /// Device sequential index
        /// </summary>
        /// <remarks>Used to sort pins in specific qays</remarks>
        public byte Index { get; set; }
        /// <summary>
        /// Device name
        /// </summary>
        public string Device { get; set; }
        /// <summary>
        /// Device status
        /// </summary>
        public string Status { get; set; }

        public DeviceStatus(byte index, string device, string status)
            : this()
        {
            Index = index;
            Device = device;
            Status = status;
        }
        public DeviceStatus(byte[] array, int start = 0) : this()
        {
            if (array == null || array.Length < start) return;
            var stack = new SimpleStack(array, start);
            Index = stack.PopByte();
            Device = stack.PopName();
            Status = stack.PopString();
        }

        public byte[] ToArray()
        {
            var stack = new SimpleStack();
            stack.Push(Index);
            stack.PushName(Device);
            stack.Push(Status);

            return stack.Data;
        }

        public override string ToString()
        {
            return string.Format("device[0] {1} = {2}", Index, Device, Status);
        }
    }
    /// <summary>
    /// Device hardware type information
    /// </summary>
    public struct DeviceType
    {
        public uint Id { get; set; }
        public string Type { get; set; }
        public string Hardware { get; set; }
        public string Version { get; set; }
        public string[] Actions { get; set; }
    }

    /// <summary>
    /// Device information
    /// </summary>
    public struct DeviceInfo
    {
        ////Local properties
        //public uint Id { get; set; }
        //public uint NodeId { get; set; }

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
        //public Address Address { get; set; }
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
        public string Class { get; set; }
        /// <summary>
        /// Device hardware
        /// </summary>
        /// <example>DS18b20</example>
        public string Hardware { get; set; }
        /// <summary>
        /// Device software version
        /// </summary>
        public string Version { get; set; }
        /// <summary>
        /// Device actions
        /// </summary>
        public string[] Actions { get; set; }

        /// <summary>
        /// HBus compatible constructor
        /// </summary>
        /// <param name="data"></param>
        /// <param name="index"></param>
        public DeviceInfo(byte[] data, int index = 0) : this()
        {
            var stack = new SimpleStack(data, index);
            Index = stack.PopByte();
            Name = stack.PopName();
            //Address = stack.PopAddress();
            Description = stack.PopString();
            Location = stack.PopString();
            Class = stack.PopString();
            Hardware = stack.PopString();
            Version = stack.PopString();
            Actions = stack.PopStringArray();
        }
        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        /// Convert shared information
        /// to transmit to other devices
        /// </summary>
        /// <returns></returns>
        public byte[] ToArray()
        {
            var stack = new SimpleStack();
            stack.Push(Index);
            stack.PushName(Name);
            //stack.Push(Address);
            stack.Push(Description);
            stack.Push(Location);
            stack.Push(Class);
            stack.Push(Hardware);
            stack.Push(Version);
            stack.PushStringArray(Actions);
            return stack.Data;
        }
    }
}