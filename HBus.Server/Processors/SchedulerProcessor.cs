using System;
using System.Collections.Generic;
using HBus.Nodes;
using HBus.Server.Data;

namespace HBus.Server.Processors
{
    public struct EventSchedule
        : ISchedule
    {
        public string Name { get; private set; }
        public DateTime Date { get; set; }
        public ScheduleTypes Type { get; set; }
        public int Interval { get; set; }
        public Event Event { get; set; }

        public Action<Event> TriggerEvent { get; set; }
    
        public void Trigger()
        {
            if (TriggerEvent != null)
                TriggerEvent(Event);
        }

        public EventSchedule(DateTime date, Event @event, ScheduleTypes type = ScheduleTypes.Once, int interval = 0)
        {
            Date = date;
            Name = @event.Name;
            Event = @event;
            Type = type;
            Interval = interval;
            TriggerEvent = null;
        }

    }

    public class SchedulerProcessor : BaseProcessor
    {
        private const string Channel = "scheduler";
        private readonly Scheduler _scheduler;

        public SchedulerProcessor(IList<EventSchedule> scheduledEvents)
        {
            _scheduler = Scheduler.GetScheduler();

            foreach (var schedule in scheduledEvents)
            {
                var s = schedule;
                s.TriggerEvent += OnTriggerEvent;

                _scheduler.AddSchedule(s);
            }

            //Event from ep source
            OnSourceEvent = (@event, point) =>
            {
                if (@event.Channel != Channel && !string.IsNullOrEmpty(@event.Channel)) return;

                switch (@event.Name)
                {
                    case "scheduler-start":
                        _scheduler.Start();
                        break;
                    case "schedule-stop":
                        _scheduler.Stop();
                        break;
                    case "scheduler-clear":
                        _scheduler.Clear();
                        break;
                    //TODO: other event names
                    default:
                        Log.Warn("Unknown event message" + @event.Name);
                        break;
                }
            };

            //Error from ep source
            OnSourceError = (exception, sender) =>
            {
                Log.Error("Error from source endpoint", exception);
            };

            //Close connection with ep source
            OnSourceClose = (sender) =>
            {
                //Close HBus endpoint
                Stop();

                Log.Debug("closed on source close");
            };

        }

        public override void Start()
        {
            _scheduler.Start();

            base.Start();
        }
        public override void Stop()
        {
            _scheduler.Stop();

            base.Stop();
        }

        private void OnTriggerEvent(Event @event)
        {
            //Send event to subscribers
            Send(@event, this);
        }
    }
}