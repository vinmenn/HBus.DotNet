using HBus.Nodes.Common;
using HBus.Utilities;

namespace HBus.Nodes.Sensors
{
    public class SensorSerializer : Serializer<Sensor>
    {
        public new static Sensor DeSerialize(byte[] array, ref Sensor sensor, int index = 0)
        {
            if (array == null || array.Length < index) return null;

            if (sensor == null)
                sensor = new Sensor();

            var stack = new SimpleStack(array, index);

            sensor.Index = stack.PopByte();
            sensor.Name = stack.PopName();
            //sensor.Address = stack.PopAddress();
            sensor.Class = stack.PopString();
            sensor.Description = stack.PopString();
            sensor.Location = stack.PopString();
            sensor.Interval = stack.PopUInt16();
            sensor.Unit = stack.PopString();
            sensor.Hardware = stack.PopString();
            sensor.MinRange = stack.PopSingle();
            sensor.MaxRange = stack.PopSingle();
            sensor.Scale = stack.PopSingle();
            sensor.Function = (FunctionType)stack.PopByte();

            return sensor;
        }
        public new static byte[] Serialize(Sensor sensor)
        {
            if (sensor == null)
                return null;

            var stack = new SimpleStack();
            stack.Push(sensor.Index);
            stack.PushName(sensor.Name);
            //stack.Push(sensor.Address);
            stack.Push(sensor.Class);
            stack.Push(sensor.Description);
            stack.Push(sensor.Location);
            stack.Push(sensor.Interval);
            stack.Push(sensor.Unit);
            stack.Push(sensor.Hardware);
            stack.Push(sensor.MinRange);
            stack.Push(sensor.MaxRange);
            stack.Push(sensor.Scale);
            stack.Push((byte)sensor.Function);

            return stack.Data;
        }
    }
}