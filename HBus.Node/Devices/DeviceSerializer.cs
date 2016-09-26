using HBus.Nodes.Common;
using HBus.Utilities;

namespace HBus.Nodes.Devices
{
    public class DeviceSerializer : Serializer<Device>
    {
        public new static Device DeSerialize(byte[] array, ref Device device, int index = 0)
        {
            if (array == null || array.Length < index) return null;

            if (device == null)
                return null;

            var stack = new SimpleStack(array, index);

            device.Index = stack.PopByte();
            device.Name = stack.PopName();
            //Address = stack.PopAddress();
            device.Description = stack.PopString();
            device.Location = stack.PopString();
            device.Class = stack.PopString();
            device.Hardware = stack.PopString();
            device.Version = stack.PopString();
            device.Actions = stack.PopStringArray();

            return device;
        }
        public new static byte[] Serialize(Device device)
        {
            if (device == null)
                return null;

            var stack = new SimpleStack();
            stack.Push(device.Index);
            stack.PushName(device.Name);
            //stack.Push(Address);
            stack.Push(device.Description);
            stack.Push(device.Location);
            stack.Push(device.Class);
            stack.Push(device.Hardware);
            stack.Push(device.Version);
            stack.PushStringArray(device.Actions);
            return stack.Data;
        }
    }
}