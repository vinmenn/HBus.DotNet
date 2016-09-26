using System;

namespace HBus.Server.Data
{
    /// <summary>
    /// Event data passed between endpoints modules elements
    /// </summary>
    public class Event
    {
        /// <summary>
        /// Event name
        /// </summary>
        /// <example>sensor-read</example>
        public string Name { get; set; }
        /// <summary>
        /// Message type
        /// </summary>
        /// <example>event</example>
        public string MessageType { get; set; }
        /// <summary>
        /// Source of event to be subscribed
        /// </summary>
        /// <example>SN101</example>
        public string Source { get; set; }
        /// <summary>
        /// Low level address of source
        /// Could be a number or ip or whatever
        /// </summary>
        /// <example>1</example>
        public string Address { get; set; }
        /// <summary>
        /// Aanalog value
        /// </summary>
        public float Value { get; set; }
        /// <summary>
        /// Event channels
        /// </summary>
        /// <example>hbus</example>
        public string Channel { get; set; }
        /// <summary>
        /// Event subscriber
        /// </summary>
        /// <example>dashboard</example>
        public string Subscriber { get; set; }
        /// <summary>
        /// Unit of analog value
        /// </summary>
        /// <example>°C</example>
        public string Unit { get; set; }
        /// <summary>
        /// Current status of source
        /// </summary>
        ///<example>open</example>
        public string Status { get; set; }
        /// <summary>
        /// Event timestamp
        /// </summary>
        public DateTime Timestamp { get; set; }
        /// <summary>
        /// Additional data
        /// </summary>
        public byte[] Data { get; set; }
    }
}