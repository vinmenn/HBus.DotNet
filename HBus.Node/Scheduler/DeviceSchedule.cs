using System;
using HBus.Nodes.Devices;

namespace HBus.Nodes
{
    /// <summary>
    /// Device schedule information
    /// </summary>
    public struct DeviceSchedule : ISchedule
    {
        private DateTime _date;
        private Device _device;
        private DeviceAction _action;
        private ScheduleTypes _type;
        private int _interval;

        public string Name
        {
            get { return _device != null ? _device.Name : string.Empty;  }
        }
        
        public DateTime Date
        {
            get { return _date; }
            set { _date = value; }
        }
        public Device Device
        {
            get { return _device; }
            set { _device = value; }
        }
        public DeviceAction Action
        {
            get { return _action; }
            set { _action = value; }
        }
        public ScheduleTypes Type
        {
            get { return _type; }
            set { _type = value; }
        }
        public int Interval
        {
            get { return _interval; }
            set { _interval = value; }
        }

        public DeviceSchedule(DateTime date, Device device, DeviceAction action, ScheduleTypes type = ScheduleTypes.Once, int interval = 0)
        {
            _date = date;
            _device = device;
            _action = action;
            _type = type;
            _interval = interval;
        }
        public void Trigger()
        {
            _device.ExecuteAction(_action);
        }
    }

}