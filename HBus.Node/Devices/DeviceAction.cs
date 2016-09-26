using HBus.Utilities;

namespace HBus.Nodes.Devices
{
    /// <summary>
    /// Device action information
    /// Store all information needed to exeute ana ction on specific device
    /// </summary>
    public struct DeviceAction
    {
        private string _device;
        private string _action;
        private byte[] _values;

        public DeviceAction(byte[] array, int startIndex = 0)
        {
            var stack = new SimpleStack(array, startIndex);
            _device = stack.PopName();
            _action = stack.PopString();
            _values = stack.PopArray();
        }

        public DeviceAction(string device, string action, byte[] values = null)
        {
            _device = device;
            _action = action;
            _values = values;
        }
        public string Device
        {
            get { return _device; }
            set { _device = value; }
        }
        public string Action
        {
            get { return _action; }
            set { _action = value; }
        }
        public byte[] Values
        {
            get { return _values; }
            set { _values = value; }
        }

        public byte[] ToArray()
        {
            var stack = new SimpleStack();
            stack.PushName(_device);
            stack.Push(_action);
            stack.Push(_values);

            return stack.Data;
        }
        public override string ToString()
        {
            return string.Format("{0} => {1}",_device, _action);
        }
    }
}