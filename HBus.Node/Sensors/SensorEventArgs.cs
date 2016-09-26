using System;

namespace HBus.Nodes.Sensors
{
    /// <summary>
    /// Sensor event arguments
    /// </summary>
    public class SensorEventArgs : EventArgs
    {
        public SensorEventArgs(SensorRead read)
        {
            Read = read;
        }
        public SensorRead Read { get; set; }
    }
}