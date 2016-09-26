using System;
using HBus.Nodes.Sensors;

namespace HBus.Nodes
{
    /// <summary>
    /// Sensor schedule 
    /// </summary>
    public struct SensorSchedule : ISchedule
    {
        private DateTime _date;
        private Sensor _sensor;
        private SensorRead _value;
        private ScheduleTypes _type;
        private int _interval;

        public string Name
        {
            get { return _sensor != null ? _sensor.Name : string.Empty; }
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

        public Sensor Sensor
        {
            get { return _sensor; }
            set { _sensor = value; }
        }
        public SensorRead Value
        {
            get { return _value; }
            set { _value = value; }
        }

        public SensorSchedule(DateTime date, Sensor sensor, ScheduleTypes type = ScheduleTypes.Once, int interval = 0)
        {
            _date = date;
            _sensor = sensor;
            _type = type;
            _interval = interval;
            _value = new SensorRead();
        }

        public void Trigger()
        {
            _value = _sensor.Read();
        }
    }
}