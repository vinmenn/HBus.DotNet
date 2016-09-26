using System;
using HBus.Nodes.Hardware;

namespace HBus.Nodes.Sensors
{
    /// <summary>
    /// Simple example sensor
    /// </summary>
    public class DummySensor : Sensor
    {
        private readonly Scheduler _scheduler;
        private readonly IHardwareAbstractionLayer _hal;

        public DummySensor(IHardwareAbstractionLayer hal, Scheduler scheduler)
        {
            _hal = hal;
            _scheduler = scheduler;
        }

        #region Implementation of Sensor
        /// <summary>
        /// Sensor read complete
        /// </summary>
        public override event EventHandler<SensorEventArgs> OnSensorRead;

        /// <summary>
        /// Sensor read handler
        /// </summary>
        public override SensorRead Read()
        {
            var rnd = new Random((int)_scheduler.TimeIndex);
            var value = ((float)rnd.NextDouble() * (MaxRange - MinRange)) + MinRange;
            var read = new SensorRead { Time = _scheduler.TimeIndex, Name = Name, Value = value };

            Log.Debug(string.Format("sensor read {0}", read));

            if (OnSensorRead != null)
                OnSensorRead(this, new SensorEventArgs(read));

            return read;
        }

        #endregion

        public override string ToString()
        {
            return Name;
        }
    }
}