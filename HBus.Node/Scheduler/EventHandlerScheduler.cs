using System;

namespace HBus.Nodes
{
    /// <summary>
    /// Generic event handler schedule
    /// </summary>
    public struct EventHandlerSchedule : ISchedule
    {
        #region private members
        private DateTime _date;
        private string _name;
        private ScheduleTypes _type;
        private int _interval;
        private EventArgs _args;
        #endregion

        #region public properties
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

        public event EventHandler TriggerFired;
        //{
        //    get { return _evh; }
        //    set {
        //        _triggerFired += value;
        //    }
        //}
        #endregion

        #region constructors
        public EventHandlerSchedule(DateTime date, string name, EventHandler eventHandler, EventArgs args, ScheduleTypes type = ScheduleTypes.Once, int interval  = 0)
        {
            _date = date;
            _name = name;
            _args = args;
            _type = type;
            _interval = interval;
            TriggerFired = eventHandler;
        }
        #endregion

        #region methods
        public void Trigger()
        {
            if (TriggerFired != null)
                TriggerFired(this, _args);
        }
        public override string ToString()
        {
            return _name +":"  + Date;
        }
#endregion
    }
}