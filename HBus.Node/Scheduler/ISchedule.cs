using System;

namespace HBus.Nodes
{
    public enum ScheduleTypes
    {
        Once,
        Day,
        Week,
        Month,
        Year,
        Period //In seconds
    }
    /// <summary>
    /// Common schedule interface
    /// </summary>
    public interface ISchedule
    {
        string Name { get;  }
        DateTime Date { get; set; }
        ScheduleTypes Type { get; set; }
        int Interval { get; set; }
        /// <summary>
        /// Trigger schedule event
        /// </summary>
        void Trigger();
    }
}