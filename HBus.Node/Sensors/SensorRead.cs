using HBus.Utilities;

namespace HBus.Nodes.Sensors
{
    /// <summary>
    /// Sensor read information
    /// </summary>
    public struct SensorRead
    {
        public string Name;
        public uint Time;
        public float Value;

        public SensorRead(byte[] array)
        {
            var stack = new SimpleStack(array);

            Name = stack.PopName();
            Time = stack.PopUInt32();
            Value = stack.PopSingle();
        }
        public static SensorRead Empty
        {
            get { return new SensorRead {Time = uint.MinValue, Value = float.NaN}; }
        }

        public byte[] ToArray()
        {
            var stack = new SimpleStack();

            stack.PushName(Name);
            stack.Push(Time);
            stack.Push(Value);

            return stack.Data;
        }

        public override string ToString()
        {
            return string.Format("{2:000}: {0} = {1}", Name,Value, Time);
        }
    }
}