using System;
using HBus.Nodes.Pins;

namespace HBus.Nodes.Wires
{
    public delegate void WireDelegate(Wire wire);
    public class Wire
    {
        private Pin _input;
        //private readonly INode _node;
        //private readonly BusController _bus;

        //public WireInfo Info { get; set; }

        #region shared properties
        /// <summary>
        /// Wire index
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Input Pin
        /// </summary>
        public Pin Input
        {
            get { return _input; }
            set
            {
                _input = value;
                //Add pin event handlers
                _input.OnPinChange += OnPinChange;
                _input.OnPinActivate += OnPinActivate;
                _input.OnPinDeactivate += OnPinDeactivate;
            }
        }

        /// <summary>
        /// HBus command (local or remote)
        /// </summary>
        public byte Command { get; set; }
        /// <summary>
        /// HBus address for remote commands
        /// </summary>
        public Address Address { get; set; }
        /// <summary>
        /// Flag use Input Data on event
        /// </summary>
        public bool UseInputData { get; set; }
        /// <summary>
        /// HBus command parameters
        /// </summary>
        public byte[] Parameters { get; set; }
        #endregion

        //local properties
        public event EventHandler<WireEventArgs> OnWireTriggered;


        public Wire()
        {
        }
        public Wire(Pin pin)
        {
            if (pin == null)
                throw new ArgumentNullException("pin");

            Input = pin;
        }
        ~Wire()
        {
            //Remove pin event handlers
            _input.OnPinChange -= OnPinChange;
            _input.OnPinActivate -= OnPinActivate;
            _input.OnPinDeactivate -= OnPinDeactivate;
        }
        //public Wire(byte[] data, INode node, BusController bus)
        //{
        //    if (node == null) throw new WireException("node not configured");

        //    _node = node;
        //    _bus = bus;
        //    //Set wire info
        //    Info = new WireInfo(data);
        //    //Find pin into node

        //    _pin = node.Pins.FirstOrDefault(p => p.Name == Info.Input);
        //    if (_pin == null)
        //        throw new Exception(string.Format("Wire input {0} not found", Info.Input));
        //    //Add pin event handlers
        //    _pin.OnPinChange += OnPinChange;
        //    _pin.OnPinActivate += OnPinActivate;
        //    _pin.OnPinDeactivate += OnPinDeactivate;
        //}

        //public Wire(int index, string input, byte command, Address address, bool useInputData, byte[] data, INode node, BusController bus)
        //{
        //    //_node = node;
        //    //_bus = bus;
        //    //Set wire info
        //    Info = new WireInfo()
        //    {
        //        Index = index,
        //        Input = input,
        //        Address = address,
        //        Command = command,
        //        Data = data,
        //        UseInputData = useInputData
        //    };
        //    //Find pin into node
        //    _pin = node.Pins.FirstOrDefault(p => p.Name == Info.Input);
        //    if (_pin == null)
        //        throw new Exception($"Wire input {Info.Input} not found");

        //    ////Add handlers
        //    //if ((trigger & WireTriggers.OnChange) != 0)
        //    //    Input.OnPinChange += OnPinChange;
        //    //if ((trigger & WireTriggers.OnActivate) != 0)
        //    //    Input.OnPinActivate += OnPinActivate;
        //    //if ((trigger & WireTriggers.OnDeactivate) != 0)
        //    //    Input.OnPinDeactivate += OnPinDeactivate;
        //    //Add pin event handlers
        //    _pin.OnPinChange += OnPinChange;
        //    _pin.OnPinActivate += OnPinActivate;
        //    _pin.OnPinDeactivate += OnPinDeactivate;
        //}

        public override string ToString()
        {
            return string.Format("{0} => {1}{2}",
                Input,
                Command,
                Address != Address.Empty ? " @(" + Address.Value + ")" : string.Empty);
            //return "{Input} => {Command}{(Address == Address.Empty ? string.Empty : " @(" + Address.Value + ")")}";
        }
        /// <summary>
        /// Input change handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPinChange(object sender, PinEventArgs e)
        {
            //if ((Trigger & WireTriggers.OnChange) != WireTriggers.OnChange) return;

            //var stack = new SimpleStack(Info.Data);
            //if (Info.UseInputData)
            //{
            //    stack.Push(e.Event.Value);
            //}

            //if (Info.Address == Address.Empty || Info.Address == _bus.Address)
            //    _node.Execute(Info.Address, Info.Command, stack.Data);
            //else
            //    _bus.SendImmediate(Info.Command, Info.Address, stack.Data);
        }
        /// <summary>
        /// Input activation handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPinActivate(object sender, PinEventArgs e)
        {
            // ReSharper disable once UseNullPropagation
            if (OnWireTriggered != null)
                OnWireTriggered(this, new WireEventArgs(e.Event));

            //MOVED TO EXTERNAL CONFIGURATOR

            //var stack = new SimpleStack(Info.Data);
            //if (Info.UseInputData)
            //{
            //    stack.Push(e.Event.Value);
            //}
            //if (Info.Address == Address.Empty || Info.Address == _bus.Address)
            //    _node.Execute(Info.Address, Info.Command, stack.Data);
            //else
            //    _bus.SendImmediate(Info.Command, Info.Address, stack.Data);
        }


        /// <summary>
        /// Input deactivation handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPinDeactivate(object sender, PinEventArgs e)
        {
            //if ((Trigger & WireTriggers.OnDeactivate) == 0) return;

            //var stack = new SimpleStack(Info.Data);
            //if (Info.UseInputData)
            //{
            //    stack.Push(e.Event.Value);
            //}

            //if (Info.Address == Address.Empty || Info.Address == _bus.Address)
            //    _node.Execute(Info.Address, Info.Command, stack.Data);
            //else
            //    _bus.SendImmediate(Info.Command, Info.Address, stack.Data);
        }
    }
}