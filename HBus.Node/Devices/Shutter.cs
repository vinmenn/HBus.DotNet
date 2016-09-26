using System;
using System.Reflection;
using HBus.Nodes.Hardware;
using HBus.Nodes.Pins;
using log4net;

namespace HBus.Nodes.Devices
{
    /// <summary>
    /// Shutter (blind) device
    /// Uses 2 outputs to open/close shutters or blinds
    /// </summary>
    public class Shutter : Device
    {
        #region private members
        private const string CLASS_VERSION = "1.0.0";
        protected static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected const string ActionOpen = "open";
        protected const string ActionClose = "close";
        protected const string ActionStop = "stop";
        protected const string ActionStopAfterClose = "stopclose";
        protected const string ActionStopAfterOpen = "stopopen";
        protected static readonly string[] DevActions = { ActionOpen, ActionClose, ActionStop };

        protected const string StatusUnknown = "?";
        protected const string StatusOpen = "opening";
        protected const string StatusOpened = "opened";
        protected const string StatusClose = "closing";
        protected const string StatusClosed = "closed";
        protected const string StatusStop = "stopped";

        //private DateTime? _start;
        //private float _position;
        private readonly string _openPin;
        private readonly string _closePin;
        private readonly IHardwareAbstractionLayer _hal;
        private readonly int _delay;
        private readonly Scheduler _scheduler;
        private string _status;

        #endregion

        public Shutter(string openPin, string closePin, int delay, IHardwareAbstractionLayer hal)
        {
            if (string.IsNullOrEmpty(openPin))
                throw new ArgumentOutOfRangeException("openPin", "missing open pin");
            if (string.IsNullOrEmpty(closePin))
                throw new ArgumentOutOfRangeException("closePin", "missing close pin");
            if (delay == 0)
                throw new ArgumentOutOfRangeException("delay", "delay must be greater than 0");
            if (hal == null)
                throw new ArgumentNullException("hal");

            _openPin = openPin;
            _closePin = closePin;
            _delay = delay;
            _hal = hal;
            _scheduler = Scheduler.GetScheduler();

            //Stop motors on startup
            _hal.Write(_openPin, PinTypes.Output, 0);
            _hal.Write(_closePin, PinTypes.Output, 0);
            _status = StatusStop;

            Class = "Window";
            Hardware = _hal.Info.Name;
            Version = CLASS_VERSION;
        }
        public override string[] Actions
        {
            get { return DevActions; }
        }
        public override string Status
        {
            get
            {
                if (string.IsNullOrEmpty(_status) && _hal == null)
                    _status = StatusUnknown;

                if (string.IsNullOrEmpty(_status) && _hal != null)
                {
                    var o = _hal.Read(_openPin, PinTypes.Output);
                    var c = _hal.Read(_closePin, PinTypes.Output);

                    if (o == 1 && c == 1)
                    {
                        _hal.Write(_openPin, PinTypes.Output, 0);
                        _hal.Write(_closePin, PinTypes.Output, 0);
                        Log.Warn("ATTENTION: both open and close output were active, now are turned off");

                        _status = StatusStop;
                    }
                    _status = (o == 1 && c == 0) ? StatusOpen : (c == 1 ? StatusClose : StatusStop);
                }

                return _status;

            }
        }
        public override bool ExecuteAction(DeviceAction action)
        {
            //Safe convert to shutter actions
            var oldStatus = Status;
            //Remove old schedules
            _scheduler.Purge(this.Name);
            //Turn off motors
            _hal.Write(_openPin, PinTypes.Output, 0);
            _hal.Write(_closePin, PinTypes.Output, 0);

            switch (action.Action)
            {
                case ActionOpen:
                    if (oldStatus != StatusOpen)
                    {
                        //Open shutter
                        _hal.Write(_openPin, PinTypes.Output, 1);
                        //stop after n seconds
                        _scheduler.AddSchedule(new DeviceSchedule(DateTime.Now.AddMilliseconds(_delay * 1000), this, new DeviceAction(Name, ActionStopAfterOpen)));
                        _status = StatusOpen;
                    }
                    else
                    {
                        //Double open ==> stop
                        action.Action = ActionStop;
                        _status = StatusStop;
                    }
                    break;
                case ActionClose:
                    if (oldStatus != StatusClose)
                    {
                        //Close shutter
                        _hal.Write(_closePin, PinTypes.Output, 1);
                        //stop after n seconds
                        _scheduler.AddSchedule(new DeviceSchedule(DateTime.Now.AddMilliseconds(_delay * 1000), this, new DeviceAction(Name, ActionStopAfterClose)));
                        _status = StatusClose;
                    }
                    else
                    {
                        //Double close ==> stop
                        action.Action = ActionStop;
                        _status = StatusStop;
                    }
                    break;
                case ActionStop:
                    _status = StatusStop;
                    break;
                case ActionStopAfterClose:
                    _status = StatusClosed;
                    break;
                case ActionStopAfterOpen:
                    _status = StatusOpened;
                    break;
                default:
                    return false;
            }

            if (DeviceEvent != null)
            {
                var devEvent = new DeviceEvent(Name, action.Action, Status, _scheduler.TimeIndex, action.Values);
                DeviceEvent(this, new DeviceEventArgs(devEvent));
            }

            Log.Debug(string.Format("Shutter.ExecuteAction {0}", action));

            return true;
        }
        public override bool IsActive()
        {
            return Status != StatusStop;
        }
        public override event EventHandler<DeviceEventArgs> DeviceEvent;
    }
}