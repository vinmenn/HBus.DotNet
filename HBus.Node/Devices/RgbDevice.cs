using System;
using System.Drawing;
using System.Reflection;
using HBus.Nodes.Hardware;
using HBus.Nodes.Pins;
using log4net;

namespace HBus.Nodes.Devices
{
    /// <summary>
    /// RGB device
    /// Implement a rgb light with 3 digital outputs for 7 colors
    /// Supported actions:
    ///     Off: turn off all colors
    ///     On: set default color
    ///     Full: all colors active
    ///     Set: set specific color
    /// </summary>
    public class RgbDevice : Device
    {
        private const string CLASS_VERSION = "1.0.0";
        protected static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly string[] RgbActions = {"Off", "On", "Full", "Set"};
        private readonly string _red;
        private readonly string _green;
        private readonly string _blue;
        private readonly IHardwareAbstractionLayer _hal;
        private readonly Scheduler _scheduler;

        public RgbDevice(string redPin, string greenPin, string bluePin, IHardwareAbstractionLayer hal)
        {
            _red = redPin;
            _green = greenPin;
            _blue = bluePin;
            _hal = hal;
            _scheduler = Scheduler.GetScheduler();

            WhiteColor = Color.White;
            DefaultColor = Color.White;
            Color = Color.White;
            Class = "Light";
            Hardware = _hal.Info.Name;
            Version = CLASS_VERSION;
        }


        public Color Color { get; set; }
        public Color DefaultColor { get; set; }
        public Color WhiteColor { get; set; }

        public override string[] Actions
        {
            get { return RgbActions; }
        }
        public override string Status
        {
            get
            {
                if (Color == Color.Black)
                    return "Off";
                if (Color == DefaultColor)
                    return "Default";
                if (Color == WhiteColor)
                    return "Full";
                return Color.ToArgb() != 0 ? "On" : "Off";
            }
        }
        public override bool ExecuteAction(DeviceAction action)
        {
            switch (action.Action)
            {
                case "Off":
                    SetColor(Color.Black);
                    break;
                case "On":
                    SetColor(DefaultColor);
                    break;
                case "Full":
                    SetColor(WhiteColor);
                    break;
                case "Set":
                    var color = action.Values != null && action.Values.Length > 2 ? Color.FromArgb(action.Values[0], action.Values[1], action.Values[2]) : Color.Black;
                    SetColor(color);
                    break;
                default:
                    return false;
            }

            if (DeviceEvent != null)
            {
                var devEvent = new DeviceEvent(Name, action.Action, Status, _scheduler.TimeIndex, action.Values);
                    DeviceEvent(this, new DeviceEventArgs(devEvent));
            }
            Log.Debug(string.Format("RgbDevice.ExecuteAction {0}", action));

            return true;
        }
        public override bool IsActive()
        {
            return Color != Color.Black;
        }
        public override event EventHandler<DeviceEventArgs> DeviceEvent;

        private void SetColor(Color color)
        {
            _hal.Write(_red, PinTypes.Pwm,  color.R);
            _hal.Write(_green, PinTypes.Pwm, color.G);
            _hal.Write(_blue, PinTypes.Pwm, color.B);

            Color = color;
        }
    }
}