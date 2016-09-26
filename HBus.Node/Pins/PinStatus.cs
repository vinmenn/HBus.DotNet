using HBus.Utilities;

namespace HBus.Nodes.Pins
{
    public struct PinStatus
    {
        /// <summary>
        /// Pin sequential index
        /// </summary>
        /// <remarks>Used to sort pins in specific qays</remarks>
        public byte Index { get; set; }
        /// <summary>
        /// Pin name
        /// </summary>
        public string Pin { get; set; }
        /// <summary>
        /// Pin current value
        /// </summary>
        public uint Value { get; set; }
        public PinStatus(byte index, string pin, uint value)
            : this()
        {
            Index = index;
            Pin = pin;
            Value = value;
        }
        public PinStatus(byte[] array, int start = 0) : this()
        {
            if (array == null || array.Length < start) return;
            var stack = new SimpleStack(array, start);
            Index = stack.PopByte();
            Pin = stack.PopName();
            Value = stack.PopUInt32();
        }

        public byte[] ToArray()
        {
            var stack = new SimpleStack();
            stack.Push(Index);
            stack.PushName(Pin);
            stack.Push(Value);

            return stack.Data;
        }

        public override string ToString()
        {
            return string.Format("pin[0] {1} = {2}", Index, Pin, Value);
        }
    }
}