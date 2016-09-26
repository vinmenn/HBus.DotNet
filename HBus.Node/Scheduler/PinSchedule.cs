using System;
using HBus.Nodes.Pins;

namespace HBus.Nodes
{
    /// <summary>
    /// Pin schedule information
    /// </summary>
    public struct PinSchedule : ISchedule
    {
        private DateTime _date;
        private Pin _pin;
        private int _value;
        private ScheduleTypes _type;
        private int _interval;

        public string Name
        {
            get { return _pin != null ? _pin.Name : string.Empty; }
        }

        public DateTime Date
        {
            get { return _date; }
            set { _date = value; }
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

        public Pin Pin
        {
            get { return _pin; }
            set { _pin = value; }
        }
        public int Value
        {
            get { return _value; }
            set { _value = value; }
        }

        public PinSchedule(DateTime date, Pin pin, int value, ScheduleTypes type = ScheduleTypes.Once, int interval = 0)
        {
            _date = date;
            _pin = pin;
            _value = value;
            _type = type;
            _interval = interval;
        }

        public void Trigger()
        {
            //_pin.Activate();
            _pin.Change(Value);
        }
    }
}