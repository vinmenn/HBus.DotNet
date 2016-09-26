using System;

namespace HBus.Nodes
{
    /// <summary>
    /// HBus command schedule
    /// </summary>
    public struct CommandSchedule : ISchedule
    {
        private DateTime _date;
        private string _name;
        private byte _cmd;
        private Address _address;
        private byte[] _data;
        private readonly BusController _bus;
        private ScheduleTypes _type;
        private int _interval;

        public string Name
        {
            get { return _name; }
            set { _name = value; }
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
        public byte Command
        {
            get { return _cmd; }
            set { _cmd = value; }
        }
        public Address Address
        {
            get { return _address; }
            set { _address = value; }
        }
        public byte[] Data
        {
            get { return _data; }
            set { _data = value; }
        }
        public CommandSchedule(DateTime date, string name, byte command, Address address, byte[] data, BusController bus, ScheduleTypes type = ScheduleTypes.Once, int interval  = 0)
        {
            _date = date;
            _name = name;
            _cmd = command;
            _address = address;
            _data = data;
            _bus = bus;
            _type = type;
            _interval = interval;
        }

        public void Trigger()
        {
            _bus.SendCommand(_cmd, _address, _data);
        }
        public override string ToString()
        {
            return _name +":"  + Date;
        }
    }
}