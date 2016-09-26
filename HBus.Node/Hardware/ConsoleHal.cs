using System;
using System.Collections.Generic;
using HBus.Nodes.Pins;

namespace HBus.Nodes.Hardware
{
    /// <summary>
    /// Simple console level hardware
    /// Useuful for simulations and debug
    /// </summary>
    public class ConsoleHal : IHardwareAbstractionLayer
    {
        private volatile IDictionary<string, int> _values;
        public IDictionary<string, int> Value { get { return _values; } set { _values = value; } }
        public HardwareInfo Info { private set; get; }

        public ConsoleHal()
        {
            Info = new HardwareInfo() { PwmMinValue = 0, PwmMaxValue = 1023, AnalogMinValue = 0, AnalogMaxValue = 1023 };
        }

        #region Implementation of IHardwareAbstractionLayer
        public int Read(string pin, PinTypes type)
        {
            if (Value == null)
                Value = new Dictionary<string, int>();

            return Value.ContainsKey(pin) ?  Value[pin] : 0;
        }

        public void Write(string pin, PinTypes type, int value)
        {
            if (Value == null)
                Value = new Dictionary<string, int>();
            if (Value.ContainsKey(pin))
                Value.Remove(pin);
            Value.Add(pin, value);

            Console.WriteLine("\t--------------------------------------------");
            Console.WriteLine("\t{3} Write pin {0} type {1} value {2}", pin, type, value, DateTime.Now.ToLongTimeString());
            Console.WriteLine("\t--------------------------------------------");
        }

        public void Update()
        {
            //NTD
        }

        #endregion
    }
}