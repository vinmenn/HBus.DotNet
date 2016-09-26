using HBus.Utilities;

namespace HBus.Nodes.Devices
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
}