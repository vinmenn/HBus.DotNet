using HBus.Nodes.Common;
using HBus.Utilities;

namespace HBus.Nodes.Pins
{
    public class PinSerializer : Serializer<Pin>
    {
        public new static byte[] Serialize(Pin pin)
        {
            if (pin == null)
                return null;

            var stack = new SimpleStack();
            stack.PushName(pin.Name);
            stack.Push(pin.Description);
            stack.Push(pin.Location);
            stack.Push(pin.Index);
            stack.Push(pin.Source);
            stack.Push((byte)pin.Type);
            stack.Push((byte)pin.SubType);
            stack.Push(pin.Parameters);

            return stack.Data;
        }

        public new static Pin DeSerialize(byte[] array, ref Pin pin, int index = 0)
        {
            if (array == null || array.Length < index) return null;
            var stack = new SimpleStack(array, index);

            if (pin == null)
                pin = new Pin();

            pin.Name = stack.PopName();
            pin.Description = stack.PopString();
            pin.Location = stack.PopString();
            pin.Index = stack.PopByte();
            pin.Source = stack.PopString();
            pin.Type = (PinTypes)stack.PopByte();
            pin.SubType = (PinSubTypes)stack.PopByte();
            pin.Parameters = stack.PopArray();

            return pin;
        }
    }
}