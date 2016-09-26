using HBus.Nodes.Common;
using HBus.Utilities;

namespace HBus.Nodes
{
    public class NodeSerializer : Serializer<Node>
    {
        public new static byte[] Serialize(Node node)
        {
            if (node == null)
                return null;

            var stack = new SimpleStack();

            stack.PushName(node.Name);
            stack.Push(node.Description);
            stack.Push(node.Location);
            stack.Push(node.Address);
            stack.Push(node.Type);
            stack.PushName(node.Hardware);
            stack.PushName(node.Version);
            stack.Push(node.DigitalInputs);
            stack.Push(node.DigitalOutputs);
            stack.Push(node.AnalogInputs);
            stack.Push(node.CounterInputs);
            stack.Push(node.PwmOutputs);
            stack.Push(node.WiresCount);
            stack.Push(node.DevicesCount);
            stack.Push(node.SensorsCount);
            //stack.Push(SupportedCommands);
            stack.Push(node.ResetPin);

            return stack.Data;
        }

        public new static Node DeSerialize(byte[] array, ref Node node, int index = 0)
        {
            if (array == null || array.Length < index) return null;

            if (node == null)
                return null;

            //Deserialize configuration
            var stack = new SimpleStack(array, index);


            node.Name = stack.PopName();
            node.Description = stack.PopString();
            node.Location = stack.PopString();
            node.Address = stack.PopAddress();
            node.Type = stack.PopString();
            node.Hardware = stack.PopName();
            node.Version = stack.PopName();
            node.DigitalInputs = stack.PopByte();
            node.DigitalOutputs = stack.PopByte();
            node.AnalogInputs = stack.PopByte();
            node.CounterInputs = stack.PopByte();
            node.PwmOutputs = stack.PopByte();
            node.WiresCount = stack.PopByte();
            node.DevicesCount = stack.PopByte();
            node.SensorsCount = stack.PopByte();
            node.ResetPin = stack.PopByte();

            return node;
        }
    }
}