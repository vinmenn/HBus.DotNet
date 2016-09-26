using HBus.Utilities;

namespace HBus.Nodes.Pins
{
    /// <summary>
    /// Pin event information
    /// </summary>
    public struct PinEvent
    {
        private string _pin;
        private int _value;
        private bool _isActive;

        public PinEvent(byte[] array, int startIndex = 0)
        {
            var stack = new SimpleStack(array, startIndex);
            _pin = stack.PopName();
            _value = stack.PopInt32();
            _isActive = stack.PopByte() == 1;
        }
        public PinEvent(string pin, int value, bool isActive)
        {
            _pin = pin;
            _value = value;
            _isActive = isActive;
        }
        public string Pin
        {
            get { return _pin; }
            set { _pin = value; }
        }
        public int Value
        {
            get { return _value; }
            set { _value = value; }
        }

        public bool IsActive
        {
            get { return _isActive; }
            set { _isActive = value; }
        }
        public byte[] ToArray()
        {
            var stack = new SimpleStack();
            stack.PushName(_pin);
            stack.Push(_value);
            stack.Push((byte)(_isActive ?  1 : 0));

            return stack.Data;
        }
        public override string ToString()
        {
            return string.Format("{0} = {1}", _pin, _value);
        }
    }
}