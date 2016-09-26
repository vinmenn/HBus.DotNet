using HBus.Nodes.Common;
using HBus.Nodes.Pins;
using HBus.Utilities;

namespace HBus.Nodes.Wires
{
    public class WireSerializer : Serializer<Wire>
    {
        public new static byte[] Serialize(Wire wire)
        {
            if (wire == null)
                return null;

            var stack = new SimpleStack();
            stack.Push((byte)wire.Index);
            stack.PushName(wire.Input!= null ? wire.Input.Name : string.Empty);
            stack.Push(wire.Command);
            stack.Push(wire.Address);
            stack.Push((byte)(wire.UseInputData ? 1 : 0));
            stack.Push(wire.Parameters);

            return stack.Data;
        }

        public new static Wire DeSerialize(byte[] array, ref Wire wire, int index = 0)
        {
            if (array == null || array.Length < index) return null;

            if (wire == null)
                wire = new Wire();

            //Deserialize configuration
            var stack = new SimpleStack(array, index);

            wire.Index = stack.PopByte();
            if (wire.Input == null)
                wire.Input = new Pin();
            wire.Input.Name = stack.PopName();
            wire.Command = stack.PopByte();
            wire.Address = stack.PopAddress();
            wire.UseInputData = stack.PopByte() == 1;
            wire.Parameters = stack.PopArray();

            return wire;
        }

    }
}