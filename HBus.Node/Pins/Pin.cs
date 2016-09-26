using System;
using System.Reflection;
using HBus.Nodes.Exceptions;
using HBus.Nodes.Hardware;
using log4net;

namespace HBus.Nodes.Pins
{
    /// <summary>
    /// HBus node pin management
    /// Dependencies:
    ///     IHardwareAbstractionLayer: specific used hardware
    ///     Scheduler: scheduling of pin actions
    /// </summary>
    public class Pin
    {

        protected static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        private readonly IHardwareAbstractionLayer _hal;
        private readonly Scheduler _scheduler;
        private int _value;

        public Pin()
        {
            _hal = null;
            _scheduler = null;
        }
        public Pin(IHardwareAbstractionLayer hal, Scheduler scheduler)
        {
            if (hal == null)
                throw new ArgumentNullException("hal");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");
            _hal = hal;
            _scheduler = scheduler;
        }

        //public Pin(byte[] data, IHardwareAbstractionLayer hal, Scheduler scheduler)
        //    : this(hal, scheduler)
        //{
        //    Info = new PinInfo(data);
        //}
        #region local properties (used only from local node)
        /// <summary>
        /// Pin id (PK)
        /// </summary>
        public uint Id { get; set; }
        /// <summary>
        /// Node id (FK)
        /// </summary>
        public uint NodeId { get; set; }
        /// <summary>
        /// Pin address (relative to parent node)
        /// </summary>
        public Address Address { get; set; }
        /// <summary>
        /// Min pin value (0 for digital)
        /// </summary>
        public float? MinValue { get; set; }
        /// <summary>
        /// Max pin value (1 for digital)
        /// </summary>
        public float? MaxValue { get; set; }
        #endregion

        #region shared properties
        /// <summary>
        /// Pin application name
        /// </summary>
        /// <remarks>Not to be confused with hardware pin name</remarks>
        public string Name { get; set; }
        /// <summary>
        /// Pin description
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// Pin location
        /// </summary>
        public string Location { get; set; }
        /// <summary>
        /// Pin sequential index
        /// </summary>
        /// <remarks>Used to sort pins in specific qays</remarks>
        public byte Index { get; set; }
        /// <summary>
        /// Pin number
        /// </summary>
        /// <remarks>Hardware pin number (depends on platform)</remarks>
        public string Source { get; set; }
        /// <summary>
        /// Pin type
        /// </summary>
        /// <remarks>set use of pin(as digital input etc.)</remarks>
        public PinTypes Type { get; set; }
        /// <summary>
        /// Pin Subtype
        /// Set use of pin associated with its type
        /// </summary>
        /// <remarks>Depends on specific type</remarks>
        public PinSubTypes SubType { get; set; }
        /// <summary>
        /// Pin parameters
        /// </summary>
        /// <remarks>Used with specific subtypes</remarks>
        public byte[] Parameters { get; set; }

        //public PinInfo Info { get; set; }
        #endregion

        //pin events
        public event EventHandler<PinEventArgs> OnPinChange;
        public event EventHandler<PinEventArgs> OnPinActivate;
        public event EventHandler<PinEventArgs> OnPinDeactivate;

        #region activate commands
        public bool Activate()
        {
            try
            {
                byte delay;
                byte width;
                int value;

                var pinScheduled = _scheduler.HasSchedules(Name);

                if (Type == PinTypes.Output || Type == PinTypes.Pwm)
                {
                    switch (SubType)
                    {
                        case PinSubTypes.OutputLow:
                            Change(0);
                            break;
                        case PinSubTypes.OutputHigh:
                            Change(1);
                            break;
                        case PinSubTypes.OutputToggle:
                            Toggle();
                            break;
                        case PinSubTypes.OutputTimedHigh:
                            if (!pinScheduled)
                            {
                                if (Parameters == null || Parameters.Length < 1)
                                    throw new PinNotConfiguredException("TimedHigh width missing");
                                width = Parameters[0];
                                TimedOutput(width, 1);
                            }
                            break;
                        case PinSubTypes.OutputTimedLow:
                            if (!pinScheduled)
                            {
                                if (Parameters == null || Parameters.Length < 1)
                                    throw new PinNotConfiguredException("TimedLow width missing");
                                width = Parameters[0];
                                TimedOutput(width, 0);
                            }
                            break;
                        case PinSubTypes.OutputDelayHigh:
                            if (!pinScheduled)
                            {
                                if (Parameters == null || Parameters.Length < 1)
                                    throw new PinNotConfiguredException("DelayHigh delay missing");
                                delay = Parameters[0];
                                DelayOutput(delay, 1);
                            }
                            break;
                        case PinSubTypes.OutputDelayLow:
                            if (!pinScheduled)
                            {
                                if (Parameters == null || Parameters.Length < 1)
                                    throw new PinNotConfiguredException("DelayLow width missing");
                                delay = Parameters[0];
                                DelayOutput(delay, 0);
                            }
                            break;
                        case PinSubTypes.OutputPulseHigh:
                            if (!pinScheduled)
                            {
                                if (Parameters == null || Parameters.Length < 1)
                                    throw new PinNotConfiguredException("PulseHigh delay missing");
                                if (Parameters == null || Parameters.Length < 2)
                                    throw new PinNotConfiguredException("PulseHigh width missing");

                                delay = Parameters[0];
                                width = Parameters[1];

                                PulsedOutput(delay, width, 1);
                            }
                            break;
                        case PinSubTypes.OutputPulseLow:
                            if (!pinScheduled)
                            {
                                if (Parameters == null || Parameters.Length < 1)
                                    throw new PinNotConfiguredException("PulseLow delay missing");
                                if (Parameters == null || Parameters.Length < 2)
                                    throw new PinNotConfiguredException("PulseLow width missing");

                                delay = Parameters[0];
                                width = Parameters[1];

                                PulsedOutput(delay, width, 0);
                            }
                            break;
                        case PinSubTypes.OutputDelayToggle:
                            if (!pinScheduled)
                            {
                                if (Parameters == null || Parameters.Length < 1)
                                    throw new PinNotConfiguredException("DelayToggle width missing");
                                delay = Parameters[0];
                                DelayToggle(delay);
                            }
                            break;
                        case PinSubTypes.OutputSetValue:
                            if (Parameters == null || Parameters.Length < 1)
                                throw new PinNotConfiguredException("SetValue width missing");
                            value = Parameters[0];
                            Change(value);
                            break;
                        case PinSubTypes.OutputAddValue:
                            if (Parameters == null || Parameters.Length < 1)
                                throw new PinNotConfiguredException("AddValue width missing");
                            value = Parameters[0];
                            ChangeDelta((short) value);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    Check();
                }
                else
                {
                    if (OnPinActivate != null) OnPinActivate(this,
                        new PinEventArgs { Event = new PinEvent(Name, 1, true) });
                }

                Log.Debug(string.Format("Activate pin {0} done", Name));

                return true;
            }
            catch (Exception ex)
            {
                Log.Error(string.Format("Activate pin {0} failed", Name), ex);
            }

            return false;
        }
        public bool Deactivate()
        {
            try
            {
                if (Type == PinTypes.Output || Type == PinTypes.Pwm)
                {
                    switch (SubType)
                    {
                        case PinSubTypes.OutputLow:
                        case PinSubTypes.OutputTimedLow:
                        case PinSubTypes.OutputPulseLow:
                        case PinSubTypes.OutputDelayLow:
                            Change(1);
                            break;
                        case PinSubTypes.OutputHigh:
                        case PinSubTypes.OutputToggle:
                        case PinSubTypes.OutputTimedHigh:
                        case PinSubTypes.OutputDelayHigh:
                        case PinSubTypes.OutputPulseHigh:
                        case PinSubTypes.OutputDelayToggle:
                        case PinSubTypes.OutputSetValue:
                        case PinSubTypes.OutputAddValue:
                            Change(0);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    Check();
                }
                else
                {
                    if (OnPinDeactivate != null) OnPinDeactivate(this,
                        new PinEventArgs { Event = new PinEvent(Name, 0, false) });
                }
                Log.Debug(string.Format("Deactivate {0} done", Name));
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(string.Format("Deactivate pin {0} failed", Name), ex);
            }

            return false;
        }
        #endregion

        #region write commands
        public bool Change(int value)
        {
            try
            {
                //Here goes real output code
                _hal.Write(Source, Type, value);

                Check();

                Log.Debug(string.Format("Change pin {0} value {1} done", Name, value));
                return true;

            }
            catch (Exception ex)
            {
                Log.Error(string.Format("Change pin {0} value {1} failed", Name, value), ex);
            }

            return false;
        }
        public bool Toggle()
        {
            try
            {
                //Here goes real output code
                _hal.Write(Source, Type, _hal.Read(Source, Type) == 1 ? 0 : 1);

                Check();

                Log.Debug(string.Format("Toggle pin {0} done", Name));

                return true;
            }
            catch (Exception ex)
            {
                Log.Error(string.Format("Toggle pin {0} failed", Name), ex);
            }

            return false;
        }
        public bool DelayToggle(byte delay)
        {
            try
            {
                if (_scheduler == null)
                    throw new PinException("Scheduler not defined");

                //Schedule next event
                _scheduler.Purge(Name);
                _scheduler.AddSchedule(new PinSchedule(DateTime.Now.AddMilliseconds(delay * 100), this, _hal.Read(Source, Type) == 1 ? 0 : 1));

                Log.Debug(string.Format("DelayToggle pin {0} delay {1} done", Name, delay));

                return true;
            }
            catch (Exception ex)
            {
                Log.Error(string.Format("DelayToggle pin {0}  delay {1} failed", Name, delay), ex);
            }

            return false;
        }
        public bool TimedOutput(byte width, int value)
        {
            try
            {
                //Here goes real output code
                _hal.Write(Source, Type, value);

                Check();

                //Schedule next event
                _scheduler.Purge(Name);
                _scheduler.AddSchedule(new PinSchedule(DateTime.Now.AddMilliseconds(width * 100), this, 1 - value));

                Log.Debug(string.Format("TimedOutputPin pin {0} width {1} value {2} done", Name, width, value));

                return true;
            }
            catch (Exception ex)
            {
                Log.Error(string.Format("TimedOutput pin {0}  width {1} failed", Name, width), ex);
            }

            return false;
        }
        public bool DelayOutput(byte delay, int value)
        {
            try
            {
                //Schedule next event
                _scheduler.Purge(Name);
                _scheduler.AddSchedule(new PinSchedule(DateTime.Now.AddMilliseconds(delay * 100), this, value));

                Log.Debug(string.Format("DelayOutput pin {0} delay {1} value {2} done", Name, delay, value));

                return true;
            }
            catch (Exception ex)
            {
                Log.Error(string.Format("DelayOutput pin {0}  delay {1} value {2} failed", Name, delay, value), ex);
            }

            return false;
        }
        public bool PulsedOutput(byte delay, byte width, int value)
        {
            try
            {
                //Here goes real output code
                _hal.Write(Source, Type, 1 - value);

                Check();

                //Schedule next event
                _scheduler.Purge(Name);
                _scheduler.AddSchedule(new PinSchedule(DateTime.Now.AddMilliseconds(delay * 100), this, value));
                _scheduler.AddSchedule(new PinSchedule(DateTime.Now.AddMilliseconds((delay + width) * 100), this, 1 - value));

                Log.Debug("PulsedOutput pin {Name} delay {delay} width {width} value {value} done");

                return true;
            }
            catch (Exception ex)
            {
                Log.Error("PulsedOutput pin {Name} delay {delay} width {width} value {value} failed", ex);
            }

            return false;
        }
        public bool CycledOutput(byte delay, byte width, int cycles)
        {
            try
            {
                //Here goes real output code
                _hal.Write(Source, Type, 0);

                Check();

                //Schedule next event
                _scheduler.Purge(Name);

                var time = 0;
                for (var i = 0; i < cycles; i++)
                {
                    _scheduler.AddSchedule(new PinSchedule(DateTime.Now.AddMilliseconds((time + delay) * 100), this, 1));
                    _scheduler.AddSchedule(new PinSchedule(DateTime.Now.AddMilliseconds((time + delay + width) * 100), this, 0));

                    time += delay + width;
                }
                Log.Debug("CycledOutput pin {Name} delay {delay} width {width} cycles {cycles} done");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("CycledOutput pin {Name} delay {delay} width {width} cycles {cycles} failed", ex);
            }

            return false;
        }
        public bool ChangePwm(int highPulse, int totalPulse)
        {
            try
            {
                if (Type != PinTypes.Pwm || !MinValue.HasValue || !MaxValue.HasValue)
                    return false;

                //Here goes real output code
                var ratio = ((float)highPulse / totalPulse); // = off/on ratio

                var value = Convert.ToInt32(ratio * (MaxValue.Value - MinValue.Value) + MinValue.Value);

                _hal.Write(Source, Type, value);

                Check();

                Log.Debug("ChangePwm pin {Name} delay {highPulse} pulse {totalPulse} done");

                return true;
            }
            catch (Exception ex)
            {
                Log.Error("ChangePwm pin {Name} delay {highPulse} pulse {totalPulse} failed", ex);
            }
            return false;
        }
        public bool ChangeDelta(int delta)
        {
            try
            {
                if (Type != PinTypes.Pwm)
                    return false;

                //Here goes real output code
                var value = Convert.ToSingle(_hal.Read(Source, Type) + delta);

                //Clip to min/max range
                if (MinValue.HasValue && MaxValue.HasValue)
                {
                    value = Math.Min(MaxValue.Value, Math.Max(MinValue.Value, value));
                }

                _hal.Write(Source, Type, Convert.ToInt32(value));

                Check();

                Log.Debug("ChangeDelta pin {Name} delta {delta} done");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("ChangeDelta pin {Name} delta {delta} failed", ex);
            }
            return false;
        }
        public bool Fade(int startValue, int endValue, byte steps, int delay)
        {
            try
            {
                //Here goes real output code
                _hal.Write(Source, Type, startValue);

                Check();

                var delta = (endValue - startValue) / steps;
                var value = startValue + delta;
                for (var i = 0; i < steps; i++)
                {
                    _scheduler.AddSchedule(new PinSchedule(DateTime.Now.AddMilliseconds(delay * (i + 1) * 100), this, value));

                    value += delta;
                }

                Log.Debug(string.Format("Fade pin {0} startValue {1} endValue {2} steps {3} delay {4} done", Name,
                                        startValue, endValue, steps, delay));
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(string.Format("Fade pin {0} startValue {1} endValue {2} steps {3} delay {4} failed", Name,
                                        startValue, endValue, steps, delay),ex);
            }
            return false;
        }
        #endregion

        #region read commands
        public int Read()
        {
            try
            {
                //Get pin value
                var value = _hal.Read(Source, Type);

                Log.Debug(string.Format("Read pin {0} type {1} value {2} done", Name, Type, value));

                return value;
            }
            catch (Exception ex)
            {
                Log.Error(string.Format("Read pin {0} type {1} failed", Name, Type), ex);
            }

            return 0;
        }
        public bool IsActive()
        {
            try
            {
                var value = _hal.Read(Source, Type);

                Log.Debug(string.Format("IsActive pin {0} done", Name));

                switch (Type)
                {
                    //Input types
                    case PinTypes.Input:
                    case PinTypes.Analog:
                    case PinTypes.Counter:
                        #region Input subtypes
                        switch (SubType)
                        {
                            case PinSubTypes.InputLow:
                                return (value == 0);
                            case PinSubTypes.InputHigh:
                                return value == 1;
                            case PinSubTypes.InputHighLow:
                                return _value == 1 && value == 0;
                            case PinSubTypes.InputLowHigh:
                                return _value == 0 && value == 1;
                            case PinSubTypes.InputBelow:
                                return Parameters != null && value < Parameters[0];
                            case PinSubTypes.InputBeyond:
                                return Parameters != null && value > Parameters[0];
                            case PinSubTypes.InputBetween:
                                return Parameters != null && value >= Parameters[0] && value <= Parameters[1];
                            case PinSubTypes.InputOutside:
                                return Parameters != null && (value < Parameters[0] || value > Parameters[1]);
                            case PinSubTypes.InputChanged:
                                return value != _value;
                            case PinSubTypes.InputEqualTo:
                                return Parameters != null && Parameters.Length > 0 && value == Parameters[0];
                            default:
                                return false;
                        }
                        #endregion
                    //Output subtypes
                    case PinTypes.Output:
                    case PinTypes.Pwm:
                        #region Output subtypes
                        switch (SubType)
                        {
                            case PinSubTypes.None:
                                return false;
                            case PinSubTypes.OutputLow:
                            case PinSubTypes.OutputTimedLow:
                            case PinSubTypes.OutputDelayLow:
                            case PinSubTypes.OutputPulseLow:
                                return value == 0;
                            case PinSubTypes.OutputHigh:
                            case PinSubTypes.OutputToggle:
                            case PinSubTypes.OutputTimedHigh:
                            case PinSubTypes.OutputDelayHigh:
                            case PinSubTypes.OutputPulseHigh:
                            case PinSubTypes.OutputDelayToggle:
                                return value == 1;
                            case PinSubTypes.OutputSetValue:
                                return Parameters != null && Parameters.Length > 0 && value == Parameters[0];
                            case PinSubTypes.OutputAddValue:
                                return Parameters != null && Parameters.Length > 0 && value != Parameters[0];
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                        #endregion
                    default:
                        throw new ArgumentOutOfRangeException();
                }

            }
            catch (Exception ex)
            {
                Log.Error(string.Format("IsActive pin {0} failed", Name), ex);
            }
            return false;
        }

        public void Check()
        {
            var value = _hal.Read(Source, Type);

            if (value == _value) return;

            var active = IsActive();

            if (OnPinChange != null)
                OnPinChange(this,
                    new PinEventArgs
                    {
                        Event = new PinEvent(Name, value, active)
                    
                    });

            if (active)
            {
                if (OnPinActivate != null) OnPinActivate(this,
                    new PinEventArgs { Event = new PinEvent(Name, value, true) });
            }
            else
            {
                if (OnPinDeactivate != null) OnPinDeactivate(this,
                    new PinEventArgs { Event = new PinEvent(Name, value, false) });
            }

            _value = value;
        }
        #endregion

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;

            if (obj.GetType() != GetType()) return false;
            
            return Equals((Pin) obj);
        }
        protected bool Equals(Pin other)
        {
            return string.Equals(Name, other.Name) && Index == other.Index && Source == other.Source && Type == other.Type && SubType == other.SubType;
        }
        public override int GetHashCode()
        {
            unchecked
            {
                // ReSharper disable NonReadonlyMemberInGetHashCode
                var hashCode = Name != null ? Name.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ Index.GetHashCode();
                hashCode = (hashCode * 397) ^ Source.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)Type;
                hashCode = (hashCode * 397) ^ SubType.GetHashCode();
                // ReSharper restore NonReadonlyMemberInGetHashCode

                return hashCode;
            }
        }
        public override string ToString()
        {
            return Name;
        }
    }
}