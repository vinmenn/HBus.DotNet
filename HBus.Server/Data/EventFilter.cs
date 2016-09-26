using System;
using System.Collections.Generic;

namespace HBus.Server.Data
{
    public class EventFilter
    {
        /// <summary>
        /// Name of event
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Source of event
        /// </summary>
        public string Source { get; set; }
        /// <summary>
        /// Event type
        /// </summary>
        public string Type { get; set; }
        /// <summary>
        /// Unit of analog value
        /// </summary>
        public string Unit { get; set; }
        /// <summary>
        /// Current status of source
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// ValueFilter
        /// </summary>
        public string ValueFilter { get; set; }
        /// <summary>
        /// Aanalog value reference
        /// </summary>
        public float ValueRefA { get; set; }
        /// <summary>
        /// Aanalog value reference
        /// </summary>
        public float ValueRefB { get; set; }

        /// <summary>
        /// Timestamp filter
        /// == : Specific datetime
        /// =d : Specific date
        /// =t : Specific time
        /// =D : At specific day (1-31)
        /// =M : At specific month (1-12)
        /// =Y : At specific year
        /// =h : At specific hour
        /// =m : At specific minute
        /// =s : At specific second
        /// <x : less than (all codes before)
        /// >x : greater than (all codes before)
        /// !x : not than (all codes before)
        /// </summary>
        public string TimestampFilter { get; set; }

        /// <summary>
        /// Timestamp reference
        /// </summary>
        public DateTime TimestampRef { get; set; }

        public IDictionary<EventFilterConnector, EventFilter> ChildFilters { get; set; }


        public bool Evaluate(Event @event)
        {
            //Evaluate filter conditions
            if (!string.IsNullOrEmpty(Name) && Name != @event.Name) return false;
            if (!string.IsNullOrEmpty(Source) && Source != @event.Source) return false;
            if (!string.IsNullOrEmpty(Type) && Type != @event.Channel) return false;

            switch (ValueFilter)
            {
                case ">":   //Greater
                    break;
                case ">=":  //Greater than / equal to
                    break;
                case "<":   //Less than
                    break;
                case "<=":  //Less than / equal to
                    break;
                case "==":  //Equal
                    break;
                case "!=":  //Different
                    break;
                case "<>":  //Between
                    break;
                case "><":  //Outside
                    break;
            }

            //Todo Datetime filters


            //Evaluate child conditions

            return true;
        }
    }
}