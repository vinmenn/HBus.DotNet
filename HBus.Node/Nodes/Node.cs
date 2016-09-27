using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HBus.Nodes.Common;
using HBus.Nodes.Configuration;
using HBus.Nodes.Devices;
using HBus.Nodes.Exceptions;
using HBus.Nodes.Hardware;
using HBus.Nodes.Pins;
using HBus.Nodes.Sensors;
using HBus.Nodes.Wires;
using HBus.Utilities;
using log4net;

namespace HBus.Nodes
{
    /// <summary>
    /// Main class for HBus node
    /// This class implements main node functions:
    ///     Detect input activations and send output activations or device actions
    ///     Manage devices/sensors subscriptions
    ///     Update status with input/output/sensors/devices data
    /// </summary>
    public sealed class Node : IDisposable
    {
        #region private members
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private class SensorSubscriber
        {
            public Address Address { get; private set; }
            public Sensor Sensor   { get; private set; }
            public int Interval    { get; private set; }
            public int Expire { get; private set; }
            public int Time        { get; set; }

            public SensorSubscriber(Address address, Sensor sensor, int interval, int expire) 
            {
                Address = address;
                Sensor = sensor;
                Interval = interval;
                Expire = expire;
                Time = interval;
            }
        }

        private readonly BusController _bus;
        private readonly IHardwareAbstractionLayer _hal;
        private readonly Scheduler _scheduler;
        private readonly IDictionary<string, Pin> _pinSubscribers;
        private readonly IDictionary<string, Device> _deviceSubscribers;
        private readonly IList<SensorSubscriber> _sensorSubscribers;
        private readonly IDictionary<Address, byte> _nodeSubscribers;
        private readonly INodeConfigurator _configurator;

        private Task _loopTask;
        //private int[] _sensorTime;
        private CancellationTokenSource _cts;
        private Thread _thread;

        #endregion

        #region public properties
        //----------------------------------------------------
        //Local properties
        //----------------------------------------------------
        public uint Id { get; set; }
        //----------------------------------------------------
        //Shared properties
        //----------------------------------------------------
        /// <summary>
        /// Node name
        /// </summary>
        public string Name { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public Address Address { get; set; }
        public string Type { get; set; }
        public string Hardware { get; set; }
        public string Version { get; set; }

        public byte DigitalInputs { get; set; }
        public byte DigitalOutputs { get; set; }
        public byte PwmOutputs { get; set; }
        public byte AnalogInputs { get; set; }
        public byte CounterInputs { get; set; }
        public byte WiresCount { get; set; }
        public byte DevicesCount { get; set; }
        public byte SensorsCount { get; set; }
        public bool HasKeypad { get; set; }
        public byte ResetPin { get; set; }

        public IList<Pin> Pins { get; set; }
        public IList<Wire> Wires { get; set; }
        public IList<Device> Devices { get; set; }
        public IList<Sensor> Sensors { get; set; }
        public NodeStatusInfo Status { get; set; }
        public IList<NodeInfo> Subnodes { get; set; }
        #endregion

        #region public events
        public event Action<Address, PinEvent> OnPinEvent;
        public event Action<Address, DeviceEvent> OnDeviceEvent;
        public event Action<Address, SensorRead> OnSensorRead;
        public event Action<Address, NodeStatusInfo> OnNodeChange;
        public event Action<string> OnLastInput;
        public event Action<string> OnLastOutput;
        #endregion

        #region Constructors / destructors
        public Node()
        {
            _configurator = null;

            //Bus controller
            _bus = null;

            //Hardware Abstraction Layer
            _hal = null;

            //Scheduler
            _scheduler = null;

            //Private members
            _pinSubscribers = new Dictionary<string, Pin>();
            _deviceSubscribers = new Dictionary<string, Device>();
            _sensorSubscribers = new List<SensorSubscriber>();
            _nodeSubscribers = new Dictionary<Address, byte>();

            //Public configuration
            Status = new NodeStatusInfo
            {
                Mask = 0,
                NodeStatus = NodeStatusValues.Unknown,
                BusStatus = BusStatus.Reset,
                LastError = 0,
                TotalErrors = 0,
                Time = 0,
                LastActivatedInput = string.Empty,
                LastActivatedOutput = string.Empty
            };

            Log.Debug("Empty node created");
        }
        public Node(INodeConfigurator configurator)
        {
            _configurator = configurator;

            //Bus controller
            _bus = _configurator.Bus;
            _bus.CommandReceived += OnCommandReceived;
            _bus.AckReceived += OnAckReceived;

            //Hardware Abstraction Layer
            _hal = _configurator.Hal;

            //Scheduler
            _scheduler = _configurator.Scheduler;

            //Private members
            _pinSubscribers = new Dictionary<string, Pin>();
            _deviceSubscribers = new Dictionary<string, Device>();
            _sensorSubscribers = new List<SensorSubscriber>();
            _nodeSubscribers = new Dictionary<Address, byte>();

            //Public configuration
            Status = new NodeStatusInfo
            {
                Mask = 0,
                NodeStatus = NodeStatusValues.Reset,
                BusStatus = _bus.Status,
                LastError = 0,
                TotalErrors = 0,
                Time = 0,
                LastActivatedInput = string.Empty,
                LastActivatedOutput = string.Empty
            };
            //TODO: Add autostart property
            //if (AutoStart) {
            //Configure node
            //if (LoadConfiguration(false))
            //{
            //    //Full reset node
            //    Reset(true);
            //}
            //}
            //else
            //_bus.Open();    //Only bus start

            Log.Debug("Node created");
        }
        public void Dispose()
        {
            Stop();
            _bus.Close();
        }
        #endregion

        #region HBus events
        private bool OnCommandReceived(object sender, Message message, int port)
        {
            //if (Status.NodeStatus != NodeStatusValues.Active) return false;

            //Clear bus & node errors
            _bus.ClearErrors();
            Status.LastError = 0;

            //Most common parameters
            var stack = new SimpleStack(message.Data);
            stack.ClearRead();

            Log.Debug(string.Format("received command from node {0}", message.Source));

            return Execute(message.Source, message.Command, message.Data);

        }
        private bool OnAckReceived(object sender, Message message, int port)
        {
            //if (Status.NodeStatus != NodeStatusValues.Active) return false;
            if (message.MessageType == MessageTypes.NackResponse)
            {
                var err = message.Data[0] << 1 | message.Data[1];
                //TODO add OnAckReceived implementation
                Log.Debug(string.Format("received nack from node {0} with error {1}", message.Source, err));
                return false;
            }
            switch (message.Command)
            {
                case NodeCommands.CMD_READ_PIN:
                    if (OnPinEvent != null)
                        OnPinEvent(message.Source, new PinEvent(message.Data));
                    break;
                case NodeCommands.CMD_READ_KEY:
                    throw new NotImplementedException("ACK from CMD_READ_KEY");
                case NodeCommands.CMD_READ_ALL:
                    if (OnNodeChange != null)
                        OnNodeChange(message.Source, new NodeStatusInfo(message.Data));
                    break;
                case NodeCommands.CMD_READ_LAST_INPUT:
                    if (OnLastInput != null)
                        OnLastInput(FixedString.FromArray(message.Data, 0, HBusSettings.NameLength));
                    break;
                case NodeCommands.CMD_READ_LAST_ACTIVE:
                    if (OnLastOutput != null)
                        OnLastOutput(FixedString.FromArray(message.Data, 0, HBusSettings.NameLength));
                    break;
                case NodeCommands.CMD_READ_SENSOR:
                    if (OnSensorRead != null)
                        OnSensorRead(message.Source, new SensorRead(message.Data));
                    break;
            }
            //TODO add OnAckReceived implementation
            Log.Debug(string.Format("received ack from node {0} on port", message.Source, port));
            return true;
        }
        #endregion

        #region Node methods
        public void Start()
        {
            ////init time intervals
            //_sensorTime = new int[Sensors.Count];
            //foreach (var sensor in Sensors)
            //    _sensorTime[sensor.Index] = sensor.Interval;

            //_bus.Open();

            #region start main loop
            _cts = new CancellationTokenSource();
            //_loopTask = new Thread(UpdateLoop, _cts.Token);
            _thread = new Thread(UpdateLoop);
            _thread.Start();
            #endregion

            //if (_bus.Status == BusStatus.Reset)
            _bus.Open();    //Only bus start
            _scheduler.Start();
             
            Status.NodeStatus = NodeStatusValues.Active;

            Log.Debug("Node started");
        }

        public void Stop()
        {
            if (_cts !=null)
                _cts.Cancel();
            if (_scheduler!=null)
                _scheduler.Stop();

            Status.NodeStatus = NodeStatusValues.Ready;

            Log.Debug("Node stopped");
        }

        public void Close()
        {
            Stop();
            _bus.Close();
        }

        public void Reset(bool fullReset)
        {
            Stop();

            #region init Scheduler
            _scheduler.OnTimeElapsed += i =>
            {
                if (Status.NodeStatus != NodeStatusValues.Ready && Status.NodeStatus != NodeStatusValues.Active) return;

                UpdateNodeStatus();

                //26/08/2016 DISABLED NODE STATUS ON POLLING
                // now NODE_STATUS interrogate all pins/devices and sends single values

                ////External node listeners
                //foreach (var subscriber in _nodeSubscribers)
                //{
                //    //Send command to HBus listeners
                //    _bus.SendImmediate(NodeCommands.CMD_PUSH_NODE_STATUS, subscriber.Key, Status.ToArray());
                //}

                //local handlers
                if (OnNodeChange != null)
                    OnNodeChange(_bus.Address, Status);

                //Scheduled sensors reads
                /*
                foreach (SensorSubscriber sub in _sensorSubscribers)
                {
                    if (sub.Time == 0)
                    {
                        var read = sub.Sensor.Read();
                        //Send command subscriber
                        _bus.SendCommand(NodeCommands.CMD_PUSH_SENSOR_READ, sub.Address, read.ToArray());

                        //Reset time counter
                        sub.Time = sub.Interval;
                    }
                    else
                        sub.Time--;
                }
                */
            };
            _scheduler.Clear();
            #endregion

            if (fullReset)
            {
                #region init Pins
                foreach (var pin in Pins)
                {
                    pin.OnPinChange += (sender, args) =>
                    {
                        if (Status.NodeStatus != NodeStatusValues.Ready && Status.NodeStatus != NodeStatusValues.Active) return;

                        //local handlers
                        if (OnPinEvent != null)
                            OnPinEvent(_bus.Address, args.Event);

                        //Remote handlers
                        foreach (var psub in _pinSubscribers.Where(p => p.Value.Name == args.Event.Pin))
                        {
                            _bus.SendCommand(NodeCommands.CMD_PUSH_PIN_EVENT, Address.Parse(psub.Key.Substring(0, psub.Key.IndexOf('.'))), args.Event.ToArray());
                        }
                    };

                    pin.OnPinActivate += (sender, args) =>
                    {
                      if (Status.NodeStatus != NodeStatusValues.Ready && Status.NodeStatus != NodeStatusValues.Active)
                        return;

                      var ptmp = Pins.FirstOrDefault(p => p.Name == args.Event.Pin);
                      if (ptmp != null)
                      {
                        //Store active input or output
                        if (ptmp.Type == PinTypes.Input || ptmp.Type == PinTypes.Analog || ptmp.Type == PinTypes.Counter)
                          Status.LastActivatedInput = ptmp.Name;
                        else
                          Status.LastActivatedOutput = ptmp.Name;
                      }
                      //local handlers
                      if (OnPinEvent != null)
                        OnPinEvent(_bus.Address, args.Event);

                      //Remote handlers
                      foreach (var psub in _pinSubscribers.Where(p => p.Value.Name == args.Event.Pin))
                      {
                        _bus.SendCommand(NodeCommands.CMD_PUSH_PIN_EVENT,
                          Address.Parse(psub.Key.Substring(0, psub.Key.IndexOf('.'))), args.Event.ToArray());
                      }
                    };
                }
                #endregion

                //Wires
                //foreach (var wire in Wires) {}

                #region init Devices
                foreach (var device in Devices)
                {
                    device.DeviceEvent += (sender, args) =>
                    {
                        if (Status.NodeStatus != NodeStatusValues.Ready && Status.NodeStatus != NodeStatusValues.Active) return;

                        //local handlers
                        if (OnDeviceEvent != null)
                            OnDeviceEvent(_bus.Address, args.Event);

                        //Remote handlers
                        foreach (var devsub in _deviceSubscribers.Where(d => d.Value.Name == args.Event.Device))
                        {
                            _bus.SendCommand(NodeCommands.CMD_PUSH_DEVICE_EVENT, Address.Parse(devsub.Key.Substring(0, devsub.Key.IndexOf('.'))), args.Event.ToArray());
                        }
                    };
                }
                #endregion

                #region init Sensors
                foreach (var sensor in Sensors)
                {
                    sensor.OnSensorRead += (sender, args) =>
                    {
                        if (Status.NodeStatus != NodeStatusValues.Ready && Status.NodeStatus != NodeStatusValues.Active) return;
                        var read = args.Read;

                        //local handlers
                        if (OnSensorRead != null)
                            OnSensorRead(_bus.Address, read);

                        //Remote handlers
                        foreach (SensorSubscriber sub in _sensorSubscribers.Where(s => s.Sensor.Name == read.Name))
                        {
                            _bus.SendCommand(NodeCommands.CMD_PUSH_SENSOR_READ, sub.Address,read.ToArray());
                        }

                    };

                    if (sensor.Interval > 0)
                    {
                        _scheduler.AddSchedule(
                            new EventHandlerSchedule(
                            DateTime.Now.AddSeconds(sensor.Interval),
                            "sensor-read:" + sensor.Name,
                            (sender, e) => sensor.Read(), //Anonymous call
                            EventArgs.Empty,
                            ScheduleTypes.Period,
                            sensor.Interval)
                        );
                    }
                }
                #endregion

            }

            //Deactivate all outputs
            foreach (var pin in Pins.Where(p => p.Type != PinTypes.Input && p.Type != PinTypes.Counter && p.IsActive()))
            {
                pin.Deactivate();
            }

            //Reset devices

            //Reset sensors
            foreach (var sensor in Sensors) //.Where(r=> r.Reset != null))
            {
                sensor.Reset();
            }
            _bus.ClearErrors();


            Status.NodeStatus = NodeStatusValues.Reset;
            Status.BusStatus = _bus.Status;
            Status.LastActivatedInput = string.Empty;
            Status.LastActivatedOutput = string.Empty;
            Status.LastError = null;
            Status.TotalErrors = 0;

            Log.Debug("Node reset");
        }
        
        /// <summary>
        /// Execute node command
        /// </summary>
        /// <param name="source">Source of command</param>
        /// <param name="command">HBus command</param>
        /// <param name="datain">Command data</param>
        /// <returns>true if command is executed</returns>
        public bool Execute(Address source, byte command, byte[] datain)
        {
            try
            {
                byte index;
                int value;
                byte delay;
                byte width;
                string name;
                Pin pin;
                Sensor sensor;
                Device device;
                var stack = new SimpleStack(datain);
                var dataOut = new List<byte>();
                var done = false;

                switch (command)
                {
                    #region Bus commands
                    case NodeCommands.CMD_PING:
                        dataOut.AddRange(FixedString.ToArray(Name, HBusSettings.NameLength));
                        _bus.Payload = dataOut.ToArray();
                        done = true;
                        break;
                    #endregion

                    #region Node commands
                    case NodeCommands.CMD_RESET:
                        index = stack.PopByte();	//1 = full reset
                        Reset(index == 1);
                        done = true;
                        break;
                    case NodeCommands.CMD_START:
                        Start();
                        done = true;
                        break;
                    case NodeCommands.CMD_STOP:
                        Stop();
                        done = true;
                        break;
                    case NodeCommands.CMD_FACTORY_DEFAULT:
                        LoadConfiguration(true);
                        done = true;
                        break;
                    case NodeCommands.CMD_READ_CONFIG:
                        LoadConfiguration(false);
                        done = true;
                        break;
                    case NodeCommands.CMD_WRITE_CONFIG:
                        SaveConfiguration();
                        done = true;
                        break;
                    case NodeCommands.CMD_ADD_NODE_LISTENER:
                        //index = stack.PopByte();	//mask
                        //Add pin to subscriptions
                        if (!_nodeSubscribers.ContainsKey(source))
                        {
                            _nodeSubscribers.Add(source, 0);
                            
                            SubscribeAll();

                            done = true;
                        }
                        break;
                    case NodeCommands.CMD_DELETE_NODE_LISTENER:
                        if (_nodeSubscribers.ContainsKey(source))
                        {
                            _nodeSubscribers.Remove(source);

                            UnsubscribeAll();

                            done = true;
                        }
                        break;
                    #endregion

                    #region information commands
                    case NodeCommands.CMD_READ_ALL:
                        value = stack.PopByte(); //mask
                        UpdateNodeStatus((byte)value);
                        _bus.Payload = Status.ToArray();
                        done = true;
                        break;
                    case NodeCommands.CMD_GET_INFO:
                        UpdateNodeInfo();
                        var array = NodeSerializer.Serialize(this);
                        _bus.Payload = array;
                        done = true;
                        break;
                    case NodeCommands.CMD_GET_NAME_INFO: //Subnode info
                        name = stack.PopName();
                        var node = GetSubnode(name);
                        if (node != null)
                        {
                            _bus.Payload = node.ToArray();
                            done = true;
                        }
                        break;
                    #endregion
                    
                    #region General pin commands
                    case NodeCommands.CMD_ACTIVATE:
                        name = stack.PopName();
                        pin = GetPin(name);
                        if (pin != null)
                        {
                            pin.Activate();
                            done = true;
                        }
                        break;
                    case NodeCommands.CMD_DEACTIVATE:
                        name = stack.PopName();
                        pin = GetPin(name);
                        if (pin != null)
                        {
                            pin.Deactivate();
                            done = true;
                        }
                        break;
                    case NodeCommands.CMD_SET_PIN:
                        pin = new Pin(_hal, _scheduler);
                        PinSerializer.DeSerialize(datain, ref pin);
                        //pin = new Pin(datain, _hal, _scheduler);
                        var oldPin = Pins.FirstOrDefault(p => p.Index == pin.Index);
                        if (oldPin != null) Pins.Remove(oldPin);
                        Pins.Add(pin);
                        done = true;
                        break;
                    case NodeCommands.CMD_CONNECT:
                        //var wire = new Wire(datain, this, _bus);
                        var wire = new Wire();
                        WireSerializer.DeSerialize(datain, ref wire);
                        wire.Input = Pins.FirstOrDefault(p => p.Name == wire.Input.Name);

                        var oldWire = Wires.FirstOrDefault(w => w.Index == wire.Index);
                        if (oldWire != null) Wires.Remove(oldWire);
                        Wires.Add(wire);
                        done = true;
                        break;
                    case NodeCommands.CMD_ADD_PIN_LISTENER:
                        //Device name
                        name = stack.PopName();
                        pin = GetPin(name);
                        if (pin != null)
                        {
                            //Add pin to subscriptions
                            if (!_pinSubscribers.ContainsKey(source + "." + name))
                                _pinSubscribers.Add(source + "." + name, pin);
                            done = true;
                        }
                        break;
                    case NodeCommands.CMD_DELETE_PIN_LISTENER:
                        //Device name
                        name = stack.PopName();
                        pin = GetPin(name);
                        if (pin != null)
                        {
                            //Add pin to subscriptions
                            if (_pinSubscribers.ContainsKey(source + "." + name))
                                _pinSubscribers.Remove(source + "." + name);
                            done = true;
                        }
                        break;
                    case NodeCommands.CMD_PUSH_PIN_EVENT:
                        if (OnPinEvent != null)
                        {
                            OnPinEvent(source, new PinEvent(datain));
                        }
                        done = true;
                        break;
                    #endregion

                    #region Pins digital write commands
                    case NodeCommands.CMD_CHANGE_ALL_DIGITAL:
                        value = stack.PopByte();
                        foreach (var p in Pins)
                        {
                            p.Change(value);
                        }
                        done = true;
                        break;
                    case NodeCommands.CMD_CHANGE_DIGITAL:
                        index = stack.PopByte();
                        value = stack.PopByte();
                        pin = GetPin(index, PinTypes.Output);
                        if (pin != null)
                        {
                            pin.Change(value);
                            done = true;
                        }
                        break;
                    case NodeCommands.CMD_TOGGLE_DIGITAL:
                        index = stack.PopByte();
                        pin = GetPin(index, PinTypes.Output);
                        if (pin != null)
                        {
                            pin.Toggle();
                            done = true;
                        }
                        break;
                    case NodeCommands.CMD_TIMED_DIGITAL:
                        index = stack.PopByte();
                        width = stack.PopByte();
                        value = stack.PopByte();
                        pin = GetPin(index, PinTypes.Output);
                        if (pin != null)
                        {
                            pin.TimedOutput(width, value);
                            done = true;
                        }
                        break;
                    case NodeCommands.CMD_DELAY_DIGITAL:
                        index = stack.PopByte();
                        delay = stack.PopByte();
                        value = stack.PopByte();
                        pin = GetPin(index, PinTypes.Output);
                        if (pin != null)
                        {
                            pin.DelayOutput(delay, value);
                            done = true;
                        }
                        break;
                    case NodeCommands.CMD_DELAY_TOGGLE_DIGITAL:
                        index = stack.PopByte();
                        delay = stack.PopByte();
                        pin = GetPin(index, PinTypes.Output);
                        if (pin != null)
                        {
                            pin.DelayToggle(delay);
                            done = true;
                        }
                        break;
                    case NodeCommands.CMD_PULSE_DIGITAL:
                        index = stack.PopByte();
                        delay = stack.PopByte();
                        width = stack.PopByte();
                        value = stack.PopByte();
                        pin = GetPin(index, PinTypes.Output);
                        if (pin != null)
                        {
                            pin.PulsedOutput(delay, width, value);
                            done = true;
                        }
                        break;
                    case NodeCommands.CMD_CYCLE_DIGITAL:
                        index = stack.PopByte();
                        delay = stack.PopByte();
                        width = stack.PopByte();
                        value = stack.PopByte(); //cycles
                        pin = GetPin(index, PinTypes.Output);
                        if (pin != null)
                        {
                            pin.CycledOutput(delay, width, value);
                            done = true;
                        }
                        break;
                    #endregion

                    #region Pins analog/pwm write commands
                    case NodeCommands.CMD_CHANGE_PWM:
                        index = stack.PopByte();
                        delay = stack.PopByte(); //high pulse
                        width = stack.PopByte(); //total pulse
                        pin = GetPin(index, PinTypes.Pwm);
                        if (pin != null)
                        {
                            pin.ChangePwm(delay, width);
                            done = true;
                        }
                        break;
                    case NodeCommands.CMD_DELTA_PWM:
                        index = stack.PopByte();
                        value = stack.PopInt16();
                        pin = GetPin(index, PinTypes.Pwm);
                        if (pin != null)
                        {
                            pin.ChangeDelta(value);
                            done = true;
                        }
                        break;
                    case NodeCommands.CMD_FADE_PWM:
                        index = stack.PopByte();
                        //type = (PinTypes)stack.PopByte();
                        var startValue = stack.PopUInt16();
                        var endValue = stack.PopUInt16();
                        value = stack.PopByte();  //steps
                        delay = stack.PopByte();
                        pin = GetPin(index, PinTypes.Pwm);
                        if (pin != null)
                        {
                            pin.Fade(startValue, endValue, (byte)value, delay);
                            done = true;
                        }
                        break;
                    case NodeCommands.CMD_CHANGE_PIN:
                        name = stack.PopName();
                        value = stack.PopInt32();
                        pin = GetPin(name);
                        if (pin != null)
                        {
                            pin.Change(value);
                            done = true;
                        }
                        break;
                    #endregion

                    #region Pins read commands
                    case NodeCommands.CMD_GET_PIN_INFO:
                        index = stack.PopByte();
                        value = stack.PopByte();
                        pin = GetPin(index, (PinTypes)value);
                        if (pin != null)
                        {
                            _bus.Payload = PinSerializer.Serialize(pin);
                            done = true;
                        }
                        break;
                    case NodeCommands.CMD_GET_CONNECT_INFO:
                        index = stack.PopByte();
                        wire = Wires.FirstOrDefault(w => w.Index == index);
                        if (wire != null)
                        {
                            array = WireSerializer.Serialize(wire);
                            _bus.Payload = array;
                            done = true;
                        }
                        break;
                    case NodeCommands.CMD_READ_PIN:
                        name = stack.PopName();
                        pin = GetPin(name);
                        if (pin != null)
                        {
                            //Get pin value
                            var evt = new PinEvent(name, pin.Read(), pin.IsActive());
                            _bus.Payload = evt.ToArray();
                            done = true;
                        }
                        break;
                    case NodeCommands.CMD_READ_LAST_INPUT:
                        dataOut.AddRange(FixedString.ToPaddedArray(Status.LastActivatedInput, HBusSettings.NameLength, ' '));
                        _bus.Payload = dataOut.ToArray();
                        done = true;
                        break;
                    case NodeCommands.CMD_READ_LAST_ACTIVE:
                        dataOut.AddRange(FixedString.ToPaddedArray(Status.LastActivatedOutput, HBusSettings.NameLength, ' '));
                        _bus.Payload = dataOut.ToArray();
                        done = true;
                        break;
                    #endregion

                    #region Device commands
                    case NodeCommands.CMD_GET_DEVICE_INFO:
                        index = stack.PopByte();
                        device = GetDevice(index);
                        if (device != null)
                        {
                            array = DeviceSerializer.Serialize(device);
                            _bus.Payload = array;
                            done = true;
                        }
                        break;
                    case NodeCommands.CMD_GET_DEVICE_STATUS:
                        name = stack.PopName();
                        device = GetDevice(name);
                        if (device != null)
                        {
                            var status = new DeviceStatus(device.Index, device.Name, device.Status);
                            _bus.Payload = status.ToArray();
                            done = true;
                        }
                        break;
                    case NodeCommands.CMD_EXECUTE_DEVICE_ACTION:
                        //Device name
                        var action = new DeviceAction(datain);
                        device = GetDevice(action.Device);
                        //Execute action
                        if (device != null)
                        {
                            device.ExecuteAction(action);
                            done = true;
                        }
                        break;
                    case NodeCommands.CMD_ADD_DEVICE_LISTENER:
                        //Device name
                        name = stack.PopName();
                        device = GetDevice(name);
                        //Execute action
                        if (device != null)
                        {
                            if (!_deviceSubscribers.ContainsKey(source + "." + name))
                                _deviceSubscribers.Add(source + "." + name, device);
                            done = true;
                        }
                        break;
                    case NodeCommands.CMD_DELETE_DEVICE_LISTENER:
                        //Device name
                        name = stack.PopName();
                        device = GetDevice(name);
                        //Execute action
                        if (device != null)
                        {
                            if (_deviceSubscribers.ContainsKey(source + "." + name))
                                _deviceSubscribers.Remove(source + "." + name);
                            done = true;
                        }
                        break;
                    case NodeCommands.CMD_PUSH_DEVICE_EVENT:
                        if (OnDeviceEvent != null)
                        {
                            var evt = new DeviceEvent(datain);
                            OnDeviceEvent(source, evt);
                            done = true;
                        }
                        break;
                    #endregion

                    #region Sensor commands
                    case NodeCommands.CMD_GET_SENSOR_INFO:
                        index = stack.PopByte();
                        sensor = GetSensor(index);
                        if (sensor != null)
                        {
                            array = SensorSerializer.Serialize(sensor);
                            _bus.Payload = array;
                            done = true;
                        }
                        break;
                    case NodeCommands.CMD_READ_SENSOR:
                        //Sensor name
                        name = stack.PopName();
                        sensor = GetSensor(name);
                        if (sensor != null)
                        {
                            _bus.Payload = sensor.Read().ToArray();
                            done = true;
                        }
                        break;

                    case NodeCommands.CMD_RESET_SENSOR:
                        //Sensor name
                        name = stack.PopName();
                        sensor = GetSensor(name);
                        if (sensor != null)
                        {
                            sensor.Reset();
                            done = true;
                        }
                        break;
                    case NodeCommands.CMD_ADD_SENSOR_LISTENER:
                        //Sensor name
                        name = stack.PopName();
                        var interval = stack.PopByte(); //interval
                        var expire = stack.PopUInt16(); //Expires

                        sensor = GetSensor(name);
                        if (sensor != null)
                        {
                            if (_sensorSubscribers.All(s => s.Sensor != sensor))
                                _sensorSubscribers.Add(new SensorSubscriber(source, sensor, interval, expire));
                            done = true;
                        }
                        break;
                    case NodeCommands.CMD_DELETE_SENSOR_LISTENER:
                        //Sensor name
                        name = stack.PopName();
                        sensor = GetSensor(name);
                        if (sensor != null)
                        {
                            var sub = _sensorSubscribers.FirstOrDefault(s => s.Address == source);
                            if (sub.Sensor != null)
                                _sensorSubscribers.Remove(sub);
                            done = true;
                        }
                        break;
                    case NodeCommands.CMD_PUSH_SENSOR_READ:
                        if (OnSensorRead != null)
                        {
                            OnSensorRead(source, new SensorRead(datain));
                        }
                        done = true;
                        break;
                    #endregion

                    #region Obolete / unsupported commands
                    //case NodeCommands.CMD_MULTI_ACTIVATE:
                    //    //delay = stack.PopByte();
                    //    //var names = stack.PopNames();
                    //    //MultiActivateOutput(names, delay);
                    //    SetError(NodeErrorCodes.CommandNotSupported);
                    //    break;
                    //case NodeCommands.CMD_READ_KEY:
                    //    //value = GetKeyPressed();
                    //    //_bus.Payload = new[] { (byte)value };
                    //    SetError(NodeErrorCodes.CommandNotSupported);
                    //    break;
                    #endregion
                    default:
                        SetError(NodeErrorCodes.CommandNotSupported);
                        break;
                }
                return done;
            }
            catch (Exception ex)
            {
                SetError(NodeErrorCodes.GenericError);
                Log.Error(string.Format("error occurred while processing command {0} for main node {1}", command, Name), ex);
            }
            return false;
        }

        public void SubscribeAll()
        {
            //Pin events
            foreach (var p in Pins)
                //Add subscriber
                p.OnPinChange += PinChangeToNodeSubscriber;

            //Device events
            foreach (var d in Devices)
                //Add subscriber
                d.DeviceEvent += DeviceEventToNodeSubscriber;

            //Sensor reads
            foreach (var s in Sensors)
                //Add subscriber
                s.OnSensorRead += SensorReadToNodeSubscriber;
        }

        public void UnsubscribeAll()
        {
            //Pin events
            foreach (var p in Pins)
                //Add subscriber
                p.OnPinChange -= PinChangeToNodeSubscriber;

            //Device events
            foreach (var d in Devices)
                //Add subscriber
                d.DeviceEvent -= DeviceEventToNodeSubscriber;

            //Sensor reads
            foreach (var s in Sensors)
                //Add subscriber
                s.OnSensorRead -= SensorReadToNodeSubscriber;
        }

        private void PinChangeToNodeSubscriber(object sender, PinEventArgs args)
        {
            foreach (var subscriber in _nodeSubscribers.Keys)
            {
                _bus.SendCommand(NodeCommands.CMD_PUSH_PIN_EVENT, subscriber, args.Event.ToArray());
            }
        }
        private void DeviceEventToNodeSubscriber(object sender, DeviceEventArgs args)
        {
            foreach (var subscriber in _nodeSubscribers.Keys)
            {
                _bus.SendCommand(NodeCommands.CMD_PUSH_DEVICE_EVENT, subscriber, args.Event.ToArray());
            }
        }
        private void SensorReadToNodeSubscriber(object sender, SensorEventArgs args)
        {
            foreach (var subscriber in _nodeSubscribers.Keys)
            {
                _bus.SendCommand(NodeCommands.CMD_PUSH_SENSOR_READ, subscriber, args.Read.ToArray());
            }
        }

        #endregion

        #region support functions
        /// <summary>
        /// Load noce configuration
        /// </summary>
        /// <param name="defaultConfig">load default configuration</param>
        /// <returns>true if configuration is loaded</returns>
        private bool LoadConfiguration(bool defaultConfig)
        {
            try
            {

                if (_configurator == null)
                    throw new NodeConfigurationException("Node configurator not found");

                var status = Status.NodeStatus;

                Stop();

                //Configure node
                if (!_configurator.LoadConfiguration(defaultConfig, this))
                {
                    return false;
                }

                //if (status == active)
                //    Start();

                Log.Info("Node configration loaded");

                return true;
            }
            catch (Exception ex)
            {
                Status.NodeStatus = NodeStatusValues.Error;

                Log.Error("Node failed to configure", ex);

                return false;
            }
        }

        /// <summary>
        /// Save node configuration
        /// </summary>
        /// <returns>true of configuration is saved</returns>
        private bool SaveConfiguration()
        {
            return _configurator.SaveConfiguration(false);
        }

        /// <summary>
        /// Polling loop for passive hals
        /// </summary>
        private void UpdateLoop()
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                _hal.Update();
                Thread.Sleep(100);
            }
        }

        private void UpdateNodeInfo()
        {
            //Update info
            DigitalInputs = (byte)Pins.Count(p => p.Type == PinTypes.Input);
            DigitalOutputs = (byte)Pins.Count(p => p.Type == PinTypes.Output);
            AnalogInputs = (byte)Pins.Count(p => p.Type == PinTypes.Analog);
            CounterInputs = (byte)Pins.Count(p => p.Type == PinTypes.Counter);
            PwmOutputs = (byte)Pins.Count(p => p.Type == PinTypes.Pwm);
            WiresCount = (byte)Wires.Count();
            DevicesCount = (byte)Devices.Count();
            SensorsCount = (byte)Sensors.Count();
        }
        
        /// <summary>
        /// Node status update loop
        /// </summary>
        /// <param name="mask"></param>
        private void UpdateNodeStatus(byte mask = 0)
        {
            Status.BusStatus = _bus.Status;
            Status.Time = _scheduler.TimeIndex;
            if (mask !=0) Status.Mask = mask;

            //Inputs
            if ((Status.Mask & 0x01) != 0)
            {
                Status.Inputs = Pins.Where(p => p.Type == PinTypes.Input).Select(pin => (byte)pin.Read()).ToArray();
            }

            //Outputs
            if ((Status.Mask & 0x02) != 0)
            {
                Status.Outputs = Pins.Where(p => p.Type == PinTypes.Output).Select(pin => (byte)pin.Read()).ToArray();
            }

            //Analogs
            if ((Status.Mask & 0x04) != 0)
            {
                Status.Analogs = Pins.Where(p => p.Type == PinTypes.Analog).Select(pin => (ushort)pin.Read()).ToArray();
            }

            //Counters
            if ((Status.Mask & 0x08) != 0)
            {
                Status.Counters = Pins.Where(p => p.Type == PinTypes.Counter).Select(pin => (uint)pin.Read()).ToArray();
            }

            //Pwms
            if ((Status.Mask & 0x10) != 0)
            {
                Status.Pwms = Pins.Where(p => p.Type == PinTypes.Pwm).Select(pin => (ushort)pin.Read()).ToArray();
            }

            //Devices
            if ((Status.Mask & 0x20) != 0)
            {
                Status.Devices = Devices.Select(dev => dev.Status).ToArray();
            }

            //Sensors
            if ((Status.Mask & 0x40) != 0)
            {
                Status.Sensors = Sensors.Select(sns => sns.Read().Value).ToArray();
            }

        }

        private void SetError(NodeErrorCodes error)
        {
            Status.LastError = error;
            Status.TotalErrors++;

            Log.Debug(string.Format("Node error set to {0}", error));
        }
        #endregion

        #region selector functions
        private Pin GetPin(string name)
        {
            var pin = Pins.FirstOrDefault(p => p.Name == name);
            return pin;
        }
        private Pin GetPin(int index, PinTypes type)
        {
            var pin = Pins.FirstOrDefault(p => p.Index == index && p.Type == type);
            return pin;
        }
        private Device GetDevice(int index)
        {
            if (Devices != null && index < Devices.Count)
            {
                return Devices.ElementAt(index);
            }

            return null;
        }
        private Device GetDevice(string name)
        {
            Device dev = null;

            if (Devices != null) 
                dev = Devices.FirstOrDefault(d => d.Name == name);

            return dev;
        }
        private Sensor GetSensor(int index)
        {
            if (Sensors != null && index < Sensors.Count)
            {
                return Sensors.ElementAt(index);
            }

            return null;
        }
        private Sensor GetSensor(string name)
        {
            Sensor sen = null;

            if (Sensors != null)  sen = Sensors.FirstOrDefault(s => s.Name == name);

            return sen;
        }
        private NodeInfo GetSubnode(string name)
        {
            var node = Subnodes.FirstOrDefault(n => n.Name == name);

            return node;
        }
        #endregion
    }
}