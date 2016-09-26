using System.Linq;
using HBus.Utilities;

namespace HBus.Nodes.Devices
{
    /// <summary>
    /// Device event information
    /// </summary>
    public struct DeviceEvent
    {
        private string _device;
        private string _event;
        private string _status;
        private uint _time;
        private byte[] _values;

        public DeviceEvent(byte[] array, int startIndex = 0)
        {
            if (array != null)
            {
                var stack = new SimpleStack(array, startIndex);
                _device = stack.PopName();
                _event = stack.PopString();
                _status = stack.PopString();
                _time = stack.PopUInt32();
                _values = stack.PopArray();
            }
            else 
            {
                _device = string.Empty;
                _event = string.Empty;
                _status = string.Empty;
                _time = 0;
                _values = null;
            }
        }

        public DeviceEvent(string device, string action, string status)
        {
            _device = device;
            _event = action;
            _status = status;
            _time = 0;
            _values = null;
        }
        public DeviceEvent(string device, string @event, string status, uint time, byte[] values)
        {
            _device = device;
            _event = @event;
            _status = status;
            _time = time;
            _values = values;
        }
        public string Device
        {
            get { return _device; }
            set { _device = value; }
        }
        public string Event
        {
            get { return _event; }
            set { _event = value; }
        }
        public string Status
        {
            get { return _status; }
            set { _status = value; }
        }
        public uint Time
        {
            get { return _time; }
            set { _time = value; }
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
            stack.Push(_event);
            stack.Push(_status);
            stack.Push(_time);
            stack.Push(_values);

            return stack.Data;
        }
        public override string ToString()
        {
            return string.Format("{0}: {1} @ {2}", _device, _event, _time);
        }
        public string ToJson()
        {
            var values = _values.Aggregate(string.Empty, (current, value) => current + (value.ToString() + ","));

            var result = "{" + string.Format("\n\t\"device\": \"{0}\",  \"event\": \"{1}\", \"status\": \"{2}\", \"time\": \"{3}\",  \"values\": [ {4}]", _device, _event, _status, _time, values) + "\n}";
            
            return result;
        }
    }
}