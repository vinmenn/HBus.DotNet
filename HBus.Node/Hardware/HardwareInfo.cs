namespace HBus.Nodes.Hardware
{
    /// <summary>
    /// Hardware information
    /// </summary>
    public class HardwareInfo
    {
        /// <summary>
        /// Hardware name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Hardware vendor
        /// </summary>
        public string Vendor { get; set; }
        /// <summary>
        /// Number of digital inputs
        /// </summary>
        public int? Inputs { get; set; }
        /// <summary>
        /// Number of digital outputs
        /// </summary>
        public int? Outputs { get; set; }
        /// <summary>
        /// Number of analog inputs
        /// </summary>
        public int? Analogs { get; set; }
        /// <summary>
        /// Number of counter inputs
        /// </summary>
        public int? Counters { get; set; }
        /// <summary>
        /// Number of pwm outputs
        /// </summary>
        public int? Pwms { get; set; }
        /// <summary>
        /// Min analog value
        /// </summary>
        public int AnalogMinValue { get; set; }
        /// <summary>
        /// Max analog value
        /// </summary>
        public int AnalogMaxValue { get; set; }
        /// <summary>
        /// Min pwm value
        /// </summary>
        public int PwmMinValue { get; set; }
        /// <summary>
        /// Max pwm value
        /// </summary>
        public int PwmMaxValue { get; set; }
    }
}