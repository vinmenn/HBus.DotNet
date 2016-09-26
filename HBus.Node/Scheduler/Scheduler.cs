using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Timers;

namespace HBus.Nodes
{
    /// <summary>
    /// Generic scheduler
    /// Execute schedules at specific times
    /// </summary>
    public class Scheduler
    {
        private const int TimerPeriod = 1000;
        private static Scheduler _scheduler;
        private IList<ISchedule> _schedules;
        //private readonly Timer _timer;
        private Thread _thread;
        private DateTime _lastTime;
        private uint _time;
        private bool _disable;
        private bool _pause;
        private int _index;

        private Scheduler(int timePeriod = 0)
        {
            _schedules = new List<ISchedule>();

            //Activate timer for output updates
            _disable = false;
            _pause = true;

            //_timer = new Timer(timePeriod > 0 ? timePeriod : TimerPeriod);
            //_timer.Elapsed += TimerOnElapsed;
            //_timer.Start();
            _time = 0;

        }
        public static Scheduler GetScheduler()
        {
            return _scheduler ?? (_scheduler = new Scheduler());
        }
        public void Start()
        {
            _disable = false;
            _pause = false;
            _index = 0;
            _thread = new Thread(() =>
            {
                try
                {
                    while (!_disable)
                    {
                        var now = DateTime.Now;
                        if (_lastTime.AddMilliseconds(TimerPeriod) <= now)
                        {
                            if (!_pause)
                                TimerOnElapsed(this, null);

                            _lastTime = DateTime.Now; //now;
                        }
                        Thread.Sleep(TimerPeriod - 10);
                    }
                }
                catch (Exception)
                {
                    //Exit
                    return;
                }
            });
            //_timer.Start();
            _thread.Start();
        }
        public void Stop()
        {
            _disable = true;
            //_timer.Stop();
            //Thread.Sleep(TimerPeriod);
            if (_thread != null)
            {
                _thread.Abort();
                _thread.Join();
            }

        }
        public void Clear()
        {
            //if (_disable) return;
            //_timer.Enabled = false;
            _pause = true;
            _index = 0;
            _schedules.Clear();
            //_timer.Enabled = true;
            _pause = false;
            _time = 0;
        }
        public void AddSchedule(ISchedule schedule)
        {
            //if (_disable) return;

            //_timer.Enabled = false;
            _pause = true;
            _schedules.Add(schedule);
            //_timer.Enabled = true;
            _pause = false;
        }
        public void RemoveSchedule(ISchedule schedule)
        {
            //if (_disable) return;
            //_timer.Stop();
            _pause = true;
            _schedules = _schedules.Where(s => s.Name != schedule.Name && s.Date != schedule.Date).ToList();
            //_timer.Start();
            _pause = false;
        }
        public bool HasSchedules(string name)
        {
            var schedules = _schedules.Any(s => s.Name == name);
            return schedules;
        }
        public void Purge(string name = null)
        {
            //if (_disable) return;
            //Leaves only new schedules
            var schedules = new List<ISchedule>();

            foreach (var s in _schedules)
            {
                if (s.Name == name || string.IsNullOrEmpty(name) && s.Date <= DateTime.Now)
                {
                    //Schedule expired: reschedule if necessary
                    switch (s.Type)
                    {
                        case ScheduleTypes.Day:
                            s.Date = s.Date.AddDays(1);
                            break;
                        case ScheduleTypes.Week:
                            s.Date = s.Date.AddDays(7);
                            break;
                        case ScheduleTypes.Month:
                            s.Date = s.Date.AddMonths(1);
                            break;
                        case ScheduleTypes.Year:
                            s.Date = s.Date.AddYears(1);
                            break;
                        case ScheduleTypes.Period:
                            s.Date = DateTime.Now.AddSeconds(s.Interval);
                            break;
                        default:
                            s.Date = DateTime.MinValue;
                            break;
                    }
                }
                //keep only future schedules
                if (s.Date > DateTime.Now)
                    schedules.Add(s);
            }
            _schedules = schedules;
            //_schedules = _schedules.Where(s => !(s.Date < DateTime.Now && (s.Name == name || string.IsNullOrEmpty(name)))).ToList();
        }
        public bool IsEmpty
        {
            get { return !_schedules.Any(); }
        }
        public uint TimeIndex { get { return _time; } }

        #region events
        public Action<uint> OnTimeElapsed;
        public Action<IEnumerable<ISchedule>> SchedulerHandler { get; set; }
        public void TimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            //_timer.Enabled = false;

            var now = DateTime.Now;
            lock (_schedules)
            {
                for (var index = 0; index < _schedules.Count; index++)
                {
                    var schedule = _schedules[index];
                    if (schedule.Date <= now)
                    {
                        schedule.Trigger();
                        switch (schedule.Type) //Reschedule if necessary
                        {
                            case ScheduleTypes.Day:
                                schedule.Date = schedule.Date.AddDays(1);
                                break;
                            case ScheduleTypes.Week:
                                schedule.Date = schedule.Date.AddDays(7);
                                break;
                            case ScheduleTypes.Month:
                                schedule.Date = schedule.Date.AddMonths(1);
                                break;
                            case ScheduleTypes.Year:
                                schedule.Date = schedule.Date.AddYears(1);
                                break;
                            case ScheduleTypes.Period:
                                schedule.Date = DateTime.Now.AddSeconds(schedule.Interval);
                                break;
                            default:
                                _schedules.Remove(schedule);
                                break;
                        }
                    }
                    //_index = _index < (_schedules.Count - 1) ? _index + 1 : 0;
                }
            }
            /*
            var expiredSchedules = _schedules.Where(s => s.Date < now).ToList();

            if (expiredSchedules.Count > 0)
            {
                if (SchedulerHandler != null)
                {
                    SchedulerHandler(expiredSchedules);
                }
                else
                {
                    foreach (var schedule in expiredSchedules)
                    {
                        schedule.Trigger();
                    }

                    //foreach (PinSchedule schedule in expiredSchedules.Where(s => s is PinSchedule))
                    //{
                    //    schedule.Pin.Change(schedule.Value);
                    //}
                    //foreach (DeviceSchedule schedule in expiredSchedules.Where(s => s is DeviceSchedule))
                    //{
                    //    schedule.Device.ExecuteAction(schedule.Action);
                    //}
                }
                //_schedules = _schedules.Where(s => s.Date > now).ToList();
                Purge();
            }
             */
            //Increment time index
            _time++;

            if (OnTimeElapsed != null)
                OnTimeElapsed(_time);

            //_timer.Enabled = true;
        }
        #endregion
    }
}