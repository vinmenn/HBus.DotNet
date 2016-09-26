using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using HBus.Nodes.Common;
using HBus.Nodes.Devices;
using HBus.Nodes.Exceptions;
using HBus.Nodes.Hardware;
using HBus.Nodes.Pins;
using HBus.Nodes.Sensors;
using HBus.Nodes.Wires;
using HBus.Ports;
using HBus.Utilities;
using log4net;

namespace HBus.Nodes.Configuration
{
  public class XmlConfigurator : INodeConfigurator
  {
    private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
    private readonly string _xmlFile;

    #region public properties
    public IHardwareAbstractionLayer Hal { get; private set; }
    public BusController Bus { get; private set; }
    public Scheduler Scheduler { get; private set; }
    public Node Node { get; private set; }
    #endregion

    public XmlConfigurator(string filename)
    {
      _xmlFile = filename;
    }

    public void Configure(bool defaultConfig)
    {
      try
      {
        #region initial check
        var xdoc = XDocument.Load(_xmlFile);

        if (xdoc.Root == null)
          throw new NodeConfigurationException("xml root not found");

        var xnode =
            xdoc.Root.Elements()
                .FirstOrDefault(
                    x =>
                        x.Name.LocalName == "node" &&
                        Convert.ToBoolean(x.Attribute("default").Value) == defaultConfig);

        if (xnode == null)
          throw new NodeConfigurationException("node configuration not found");

        #endregion

        ConfigureHal(xnode.Element("hal"));

        //configure bus settings
        ConfigureBus(xnode.Element("bus"), xnode.Element("info"));

        if (Bus == null)
          throw new NodeConfigurationException("HBus controller not configured");

        //configure bus settings
        ConfigureScheduler(xnode.Element("scheduler"));

        if (Scheduler == null)
          throw new NodeConfigurationException("Scheduler not configured");

        //configure bus settings
        ConfigureNode(xnode);

        if (Node == null)
          throw new NodeConfigurationException("Node not configured");
      }
      catch (Exception ex)
      {
        throw new NodeConfigurationException("Node configuration failed", ex);
      }
    }

    public bool LoadConfiguration(bool defaultConfig, Node node)
    {
      try
      {
        #region Open xml file
        var xdoc = XDocument.Load(_xmlFile);

        if (xdoc.Root == null)
          throw new NodeConfigurationException("xml root not found");

        var xnode =
            xdoc.Root.Elements()
                .FirstOrDefault(
                    x =>
                        x.Name.LocalName == "node" &&
                        Convert.ToBoolean(x.Attribute("default").Value) == defaultConfig);

        if (xnode == null)
          throw new NodeConfigurationException("node configuration not found");

        #endregion

        //Configure node
        GetNodeInfo(xnode.Element("info"), ref node);
        node.Pins = GetPins(xnode.Element("pins").Elements());
        node.Devices = GetDevices(xnode.Element("devices").Elements());
        foreach (var device in node.Devices)
          device.Address = node.Address;
        node.Wires = GetWires(xnode.Element("wires").Elements(), node, Bus);
        node.Sensors = GetSensors(xnode.Element("sensors").Elements());
        foreach (var sensor in node.Sensors)
          sensor.Address = node.Address;
        node.Subnodes = GetSubnodes(xnode.Element("subnodes").Elements());
        //Update info
        node.DigitalInputs = (byte)node.Pins.Count(p => p.Type == PinTypes.Input);
        node.DigitalOutputs = (byte)node.Pins.Count(p => p.Type == PinTypes.Output);
        node.AnalogInputs = (byte)node.Pins.Count(p => p.Type == PinTypes.Analog);
        node.CounterInputs = (byte)node.Pins.Count(p => p.Type == PinTypes.Counter);
        node.PwmOutputs = (byte)node.Pins.Count(p => p.Type == PinTypes.Pwm);
        node.WiresCount = (byte)node.Wires.Count();
        node.DevicesCount = (byte)node.Devices.Count();
        node.SensorsCount = (byte)node.Sensors.Count();
        Log.Debug(string.Format("Configuration loaded for node {0}", node));
        return true;
      }
      catch (Exception ex)
      {
        Log.Error(new NodeConfigurationException("load configuration failed", ex));
      }
      return false;
    }

    public bool SaveConfiguration(bool defaultConfig)
    {
      try
      {
        #region Open xml file

        var xdoc = XDocument.Load(_xmlFile);

        if (xdoc.Root == null)
          throw new NodeConfigurationException("xml root not found");

        var xnode =
            xdoc.Root.Elements()
                .FirstOrDefault(
                    x =>
                        x.Name.LocalName == "node" &&
                        Convert.ToBoolean(x.Attribute("default").Value) == defaultConfig);

        if (xnode == null)
          throw new NodeConfigurationException("node configuration not found");

        #endregion

        SaveNodeInfo(Node, xnode);
        SavePins(Node.Pins, xnode.Element("pins"));
        SaveDevices(Node.Devices, xnode.Element("devices"));
        SaveWires(Node.Wires, xnode.Element("wires"));
        SaveSensors(Node.Sensors, xnode.Element("sensors"));
        SaveSubnodes(Node.Subnodes, xnode.Element("subnodes"));

        xdoc.Save(_xmlFile);
        return true;
      }
      catch (Exception ex)
      {
        Log.Error(new NodeConfigurationException("save configuration failed", ex));
      }
      return false;
    }

    //------------------------------------------------------------
    //Singleton readonly objects
    //------------------------------------------------------------
    //Hal configuration
    private void ConfigureHal(XElement element)
    {
      if (element == null)
        throw new NodeConfigurationException("hal section not found");

      Hal = (IHardwareAbstractionLayer)GetConfiguredObject(element);

      if (Hal != null)
        Log.Debug(string.Format("Configured hal of type {0}", Hal.GetType().Name));
      else
        Log.Warn("No Hal found (maybe is embededd ?)");
    }

    private void ConfigureBus(XElement busElement, XElement nodeElement)
    {
      if (busElement == null)
        throw new NodeConfigurationException("bus section not found");

      var ports = new List<Port>();

      foreach (var xport in busElement.Element("ports").Elements())
      {
        var port = (Port)GetConfiguredObject(xport);
        if (port != null)
          ports.Add(port);
        else
        {
          Log.Warn(string.Format("port {0} failed configuration", xport));
        }
      }


      var address = nodeElement != null && nodeElement.Element("address") != null ? Address.Parse(Convert.ToUInt32(nodeElement.Element("address").Value)) : Address.Empty;
      Bus = new BusController(address, ports.ToArray());

      Log.Debug(string.Format("Configured bus of type {0}", Bus.GetType().Name));
    }

    private void ConfigureScheduler(XElement element)
    {
      Scheduler = Scheduler.GetScheduler();
    }

    private void ConfigureNode(XElement element)
    {
      if (element == null)
        throw new NodeConfigurationException("node section not found");

      Node = (Node)GetConfiguredObject(element);

      LoadConfiguration(false, Node);
    }

    #region read functions
    private void GetNodeInfo(XElement element, ref Node node)
    {
      try
      {
        if (element == null || node == null) return;

        node.Id = element.Element("id") != null ? Convert.ToUInt32(element.Element("id").Value) : 0;
        node.Name = element.Element("name") != null ? element.Element("name").Value : string.Empty;
        node.Address =
            element.Element("address") != null
                ? Address.Parse(Convert.ToUInt32(element.Element("address").Value))
                : Address.Empty;
        node.Description =
            element.Element("description") != null ? element.Element("description").Value : string.Empty;
        node.Type = element.Element("type") != null ? element.Element("type").Value : string.Empty;
        node.Hardware = element.Element("hardware") != null ? element.Element("hardware").Value : string.Empty;
        node.Version = element.Element("version") != null ? element.Element("version").Value : string.Empty;
        node.Location = element.Element("location") != null ? element.Element("location").Value : string.Empty;

        Log.Debug("Configured node info");

      }
      catch (Exception ex)
      {
        Log.Error(string.Format("{0} failed", MethodBase.GetCurrentMethod().Name), ex);
      }
    }

    private IList<Pin> GetPins(IEnumerable<XElement> elements)
    {
      var pins = new List<Pin>();

      foreach (var xpin in elements)
      {
        if (xpin == null) continue;

        var pin = new Pin(Hal, Scheduler)
        {
          //Id = Convert.ToUInt32(xpin.Attribute("id").Value);
          //NodeId = xpin.Parent != null && xpin.Parent.Parent != null ? Convert.ToUInt32(xpin.Parent.Parent.Attribute("id").Value) : 0
          Index = xpin.Attribute("index") != null ? Convert.ToByte(xpin.Attribute("index").Value) : (byte)0,
          Name = xpin.Attribute("name") != null ? xpin.Attribute("name").Value : string.Empty,
          Description = xpin.Attribute("description") != null ? xpin.Attribute("description").Value : string.Empty,
          Location = xpin.Attribute("location") != null ? xpin.Attribute("location").Value : string.Empty,
          Type = xpin.Attribute("type") != null && Enum.IsDefined(typeof(PinTypes), xpin.Attribute("type").Value) ? (PinTypes)Enum.Parse(typeof(PinTypes), xpin.Attribute("type").Value) : PinTypes.None,
          SubType = xpin.Attribute("subtype") != null && Enum.IsDefined(typeof(PinSubTypes), xpin.Attribute("subtype").Value) ? (PinSubTypes)Enum.Parse(typeof(PinSubTypes), xpin.Attribute("subtype").Value) : PinSubTypes.None,
          Source = xpin.Attribute("source") != null ? xpin.Attribute("source").Value : string.Empty,
          Parameters = xpin.Attribute("parameters") != null ? Csv.CsvToList<byte>(xpin.Attribute("parameters").Value).ToArray() : null
        };

        pins.Add(pin);
      }

      Log.Debug(string.Format("Configured {0} pins", pins.Count));

      return pins;
    }

    private IList<Wire> GetWires(IEnumerable<XElement> elements, Node node, BusController bus)
    {
      var wires = new List<Wire>();

      foreach (var xwire in elements)
      {
        var input = xwire.Attribute("input").Value;
        var pin = node.Pins.FirstOrDefault(p => p.Name == input);
        if (pin == null) throw new WireException("Input pin not found");

        var index = Convert.ToByte(xwire.Attribute("index").Value);

        var address = xwire.Attribute("address") != null && !string.IsNullOrEmpty(xwire.Attribute("address").Value)
            ? Address.Parse(Convert.ToUInt32(xwire.Attribute("address").Value))
            : Address.Empty;

        var useInputData = xwire.Attribute("useInputData") != null && !string.IsNullOrEmpty(xwire.Attribute("useInputData").Value) && Convert.ToBoolean(xwire.Attribute("useInputData").Value);

        var cmdText = xwire.Attribute("command").Value.ToUpperInvariant();
        byte cmd = 0;

        #region parse command name
        switch (cmdText)
        {
          case "PING":
            cmd = NodeCommands.CMD_PING;
            break;
          case "RESET":
            cmd = NodeCommands.CMD_RESET;
            break;
          case "READ_CONFIG":
            cmd = NodeCommands.CMD_READ_CONFIG;
            break;
          case "START":
            cmd = NodeCommands.CMD_START;
            break;
          case "STOP":
            cmd = NodeCommands.CMD_STOP;
            break;
          //Pins
          case "CHANGE_DIGITAL":
            cmd = NodeCommands.CMD_CHANGE_DIGITAL;
            break;
          case "TOGGLE_DIGITAL":
            cmd = NodeCommands.CMD_TOGGLE_DIGITAL;
            break;
          case "TIMED_DIGITAL":
            cmd = NodeCommands.CMD_TIMED_DIGITAL;
            break;
          case "DELAY_DIGITAL":
            cmd = NodeCommands.CMD_DELAY_DIGITAL;
            break;
          case "PULSE_DIGITAL":
            cmd = NodeCommands.CMD_PULSE_DIGITAL;
            break;
          case "CYCLE_DIGITAL":
            cmd = NodeCommands.CMD_CYCLE_DIGITAL;
            break;
          case "CHANGE_ALL_DIGITAL":
            cmd = NodeCommands.CMD_CHANGE_ALL_DIGITAL;
            break;
          case "CHANGE_PWM":
            cmd = NodeCommands.CMD_CHANGE_PWM;
            break;
          case "CHANGE_PIN":
            cmd = NodeCommands.CMD_CHANGE_PIN;
            break;
          case "DELAY_TOGGLE_DIGITAL":
            cmd = NodeCommands.CMD_DELAY_TOGGLE_DIGITAL;
            break;
          case "DELTA_PWM":
            cmd = NodeCommands.CMD_DELTA_PWM;
            break;
          case "FADE_PWM":
            cmd = NodeCommands.CMD_FADE_PWM;
            break;
          case "ACTIVATE":
            cmd = NodeCommands.CMD_ACTIVATE;
            break;
          case "DEACTIVATE":
            cmd = NodeCommands.CMD_DEACTIVATE;
            break;
          case "EXECUTE_DEVICE_ACTION":
            cmd = NodeCommands.CMD_EXECUTE_DEVICE_ACTION;
            break;
          case "PUSH_SENSOR_READ":
            cmd = NodeCommands.CMD_READ_SENSOR;
            break;
        }
        #endregion

        var dataText = xwire.Attribute("data") != null ? xwire.Attribute("data").Value : string.Empty;
        var data = Csv.CsvToList<byte>(dataText).ToArray();
        //var trgText = xwire.Attribute("trigger") != null ? xwire.Attribute("trigger").Value : string.Empty;
        //var trgs = Csv.CsvToList<string>(trgText).ToArray();
        //var trigger = WireTriggers.None;
        //foreach (var trg in trgs)
        //{
        //    trigger |= trg != null && Enum.IsDefined(typeof (WireTriggers), trg)
        //        ? (WireTriggers) Enum.Parse(typeof (WireTriggers), trg)
        //        : WireTriggers.None;
        //}

        var wire = new Wire(pin)
        {
          Index = index,
          Command = cmd,
          Address = address,
          UseInputData = useInputData,
          Parameters = data
        };

        //Add wire trigger event
        //TODO configurable on Activate/deactivate/change
        wire.OnWireTriggered += (sender, args) =>
        {
          var w = (Wire)sender;

          var stack = new SimpleStack(w.Parameters);
          if (w.UseInputData)
          {
            stack.Push(args.Source.Value);
          }
          if (w.Address == Address.Empty || w.Address == bus.Address)
            node.Execute(w.Address, w.Command, stack.Data);
          else
            bus.SendImmediate(w.Command, w.Address, stack.Data);
        };

        wires.Add(wire);
      }

      Log.Debug(string.Format("Configured {0} wires", wires.Count));

      return wires;
    }

    private IList<Device> GetDevices(IEnumerable<XElement> elements)
    {
      var devices = new List<Device>();

      foreach (var xdevice in elements)
      {
        if (xdevice == null) continue;

        var device = (Device)GetConfiguredObject(xdevice);
        var xinfo = xdevice.Element("info");
        device.Name = xinfo.Element("name") != null ? xinfo.Element("name").Value : string.Empty;
        device.Index = xinfo.Element("index") != null ? Convert.ToByte(xinfo.Element("index").Value) : (byte)0;
        device.Class = xinfo.Element("hardware") != null ? xinfo.Element("hardware").Value : string.Empty;
        //Address =
        //    xinfo.Element("address") != null
        //        ? Address.Parse(Convert.ToUInt32(xinfo.Element("address").Value))
        //        : Address.Empty,
        device.Description =
            xinfo.Element("description") != null ? xinfo.Element("description").Value : string.Empty;
        device.Location = xinfo.Element("location") != null ? xinfo.Element("location").Value : string.Empty;
        device.Actions = device.Actions.ToArray();
        devices.Add(device);
      }

      Log.Debug(string.Format("Configured {0} devices", devices.Count));

      return devices;
    }

    private IList<Sensor> GetSensors(IEnumerable<XElement> elements)
    {
      var sensors = new List<Sensor>();

      foreach (var xsensor in elements)
      {
        if (xsensor == null) continue;

        var sensor = (Sensor)GetConfiguredObject(xsensor);
        var xinfo = xsensor.Element("info");

        if (xinfo != null)
        {
          sensor.Name = xinfo.Element("name") != null ? xinfo.Element("name").Value : string.Empty;
          sensor.Index = xinfo.Element("index") != null
              ? Convert.ToByte(xinfo.Element("index").Value)
              : (byte)0;
          sensor.Description =
              xinfo.Element("description") != null ? xinfo.Element("description").Value : string.Empty;
          sensor.Location = xinfo.Element("location") != null ? xinfo.Element("location").Value : string.Empty;
          sensor.Interval =
              xinfo.Element("interval") != null
                  ? Convert.ToUInt16(xinfo.Element("interval").Value)
                  : (ushort)0;
          sensor.Class = xinfo.Element("class").Value;
          sensor.Unit = xinfo.Element("unit").Value;
          sensor.MinRange = Convert.ToSingle(xinfo.Element("minRange").Value);
          sensor.MaxRange = Convert.ToSingle(xinfo.Element("maxRange").Value);
          sensor.Scale = Convert.ToSingle(xinfo.Element("scale").Value);
          sensor.Function = xinfo.Element("function") != null &&
                     Enum.IsDefined(typeof(FunctionType), xinfo.Element("function").Value)
              ? (FunctionType)Enum.Parse(typeof(FunctionType), xinfo.Element("function").Value)
              : FunctionType.None;
          sensor.Hardware = xinfo.Element("hardware").Value;
        }

        if (sensor != null)
          sensors.Add(sensor);
      }

      Log.Debug(string.Format("Configured {0} sensors", sensors.Count));

      return sensors;
    }

    private IList<NodeInfo> GetSubnodes(IEnumerable<XElement> elements)
    {
      var nodes = new List<NodeInfo>();

      foreach (var xnode in elements)
      {
        if (xnode == null) continue;

        var node = new NodeInfo
        {
          Name = xnode.Attribute("name") != null ? xnode.Attribute("name").Value : string.Empty,
          Description = xnode.Attribute("description") != null ? xnode.Attribute("description").Value : string.Empty,
          Location = xnode.Attribute("location") != null ? xnode.Attribute("location").Value : string.Empty,
          Type = xnode.Attribute("type") != null ? xnode.Attribute("type").Value : string.Empty,
          Hardware = xnode.Attribute("hardware") != null ? xnode.Attribute("hardware").Value : string.Empty,
          Version = xnode.Attribute("version") != null ? xnode.Attribute("version").Value : string.Empty,
          DigitalInputs = xnode.Attribute("inputs") != null ? Convert.ToByte(xnode.Attribute("inputs").Value) : (byte)0,
          AnalogInputs = xnode.Attribute("analogs") != null ? Convert.ToByte(xnode.Attribute("analogs").Value) : (byte)0,
          CounterInputs = xnode.Attribute("counters") != null ? Convert.ToByte(xnode.Attribute("counters").Value) : (byte)0,
          DigitalOutputs = xnode.Attribute("outputs") != null ? Convert.ToByte(xnode.Attribute("outputs").Value) : (byte)0,
          PwmOutputs = xnode.Attribute("pwms") != null ? Convert.ToByte(xnode.Attribute("pwms").Value) : (byte)0,
          DevicesCount = xnode.Attribute("devices") != null ? Convert.ToByte(xnode.Attribute("devices").Value) : (byte)0,
          SensorsCount = xnode.Attribute("sensors") != null ? Convert.ToByte(xnode.Attribute("sensors").Value) : (byte)0,

        };

        nodes.Add(node);
      }

      Log.Debug(string.Format("Configured {0} subnodes", nodes.Count));

      return nodes;
    }
    #endregion

    #region write functions
    private void SaveNodeInfo(Node node, XElement xnode)
    {
      try
      {

        if (xnode == null) throw new NodeConfigurationException("node xelement not found");

        var xinfo = xnode.Element("info");

        xinfo.Element("name").SetValue(node.Name);
        xinfo.Element("address").SetValue(node.Address.Value);
        xinfo.Element("description").SetValue(node.Description);
        xinfo.Element("type").SetValue(node.Type);
        xinfo.Element("hardware").SetValue(node.Hardware);
        xinfo.Element("version").SetValue(node.Version);
        xinfo.Element("location").SetValue(node.Location);

        Log.Debug("Saved node info configuration {node.Name}");

      }
      catch (Exception ex)
      {
        Log.Error(string.Format("{0} failed", MethodBase.GetCurrentMethod().Name), ex);
      }
    }

    private void SavePins(IList<Pin> pins, XElement xpins)
    {
      try
      {
        foreach (var pin in pins)
        {
          var xpin = xpins.Elements().FirstOrDefault(p => p.Attribute("index") != null && Convert.ToUInt16(p.Attribute("index").Value) == pin.Index &&
             p.Attribute("type") != null && p.Attribute("type").Value == pin.Type.ToString());
          if (xpin == null)
          {
            Log.Warn(string.Format("pin {0} not found", pin));
            continue;
          }
          xpin.Attribute("name").SetValue(pin.Name);
          xpin.Attribute("description").SetValue(pin.Description);
          xpin.Attribute("location").SetValue(pin.Location);
          xpin.Attribute("type").SetValue(pin.Type);
          xpin.Attribute("subtype").SetValue(pin.SubType);
          xpin.Attribute("source").SetValue(pin.Source);
        }

      }
      catch (Exception ex)
      {
        Log.Error(string.Format("{0} failed", MethodBase.GetCurrentMethod().Name), ex);
      }
    }

    private void SaveDevices(IList<Device> devices, XElement xdevices)
    {
      try
      {
        foreach (var device in devices)
        {
          var xdev = xdevices.Elements().FirstOrDefault(p => p.Element("info") != null && p.Element("info").Element("index") != null &&
              Convert.ToUInt16(p.Element("info").Element("index").Value) == device.Index);
          if (xdev == null)
          {
            Log.Warn(string.Format("device {0} not found", device));
            continue;
          }
          var xinfo = xdev.Element("info");
          xinfo.Element("name").SetValue(device.Name);
          xinfo.Element("description").SetValue(device.Description);
          xinfo.Element("location").SetValue(device.Location);
          xinfo.Element("class").SetValue(device.Class);
        }

      }
      catch (Exception ex)
      {
        Log.Error(string.Format("{0} failed", MethodBase.GetCurrentMethod().Name), ex);
      }
    }

    private void SaveWires(IList<Wire> wires, XElement xwires)
    {
      try
      {
        string outname = string.Empty, outtype = string.Empty, outaction = string.Empty, outvalues = string.Empty;

        foreach (var wire in wires)
        {
          var xwire = xwires.Elements().FirstOrDefault(p => p.Attribute("index") != null && Convert.ToUInt16(p.Attribute("index").Value) == wire.Index);
          if (xwire == null)
          {
            Log.Warn(string.Format("wire {0} not found", wire));
            continue;
          }
        }
      }
      catch (Exception ex)
      {
        Log.Error(string.Format("{0} failed", MethodBase.GetCurrentMethod().Name), ex);
      }
    }

    private void SaveSensors(IList<Sensor> sensors, XElement xsensors)
    {
      try
      {
        foreach (var sensor in sensors)
        {
          var xsen = xsensors.Elements().FirstOrDefault(p => p.Element("info").Element("index") != null && Convert.ToUInt16(p.Element("info").Element("index").Value) == sensor.Index);
          if (xsen == null)
          {
            Log.Warn(string.Format("sensor {0} not found", sensor));
            continue;
          }
          var xinfo = xsen.Element("info");
          xinfo.Element("name").SetValue(sensor.Name);
          xinfo.Element("description").SetValue(sensor.Description ?? string.Empty);
          xinfo.Element("location").SetValue(sensor.Location ?? string.Empty);
          xinfo.Element("class").SetValue(sensor.Class ?? string.Empty);
          xinfo.Element("interval").SetValue(sensor.Interval);
          if (xinfo.Element("type") != null)
          {
            var xtype = xinfo.Element("type");
            //xtype.Element("type").SetValue(sensor.Type.Type ?? string.Empty);
            xtype.Element("unit").SetValue(sensor.Unit ?? string.Empty);
            xtype.Element("minRange").SetValue(sensor.MinRange);
            xtype.Element("maxRange").SetValue(sensor.MaxRange);
            xtype.Element("scale").SetValue(sensor.Scale);
            xtype.Element("function").SetValue(sensor.Function);
            xtype.Element("hardware").SetValue(sensor.Hardware ?? string.Empty);
            //xtype.Element("version").SetValue(sensor.Type.Version ?? string.Empty);
          }
        }

      }
      catch (Exception ex)
      {
        Log.Error(string.Format("{0} failed", MethodBase.GetCurrentMethod().Name), ex);
      }
    }

    private void SaveSubnodes(IList<NodeInfo> subnodes, XElement xsubnodes)
    {
      try
      {
        for (var i = 0; i < subnodes.Count; i++)
        {
          var subnode = subnodes[i];
          var xsub = xsubnodes.Elements().FirstOrDefault(p => p.Attribute("index") != null && Convert.ToUInt16(p.Attribute("index").Value) == i);
          if (xsub == null)
          {
            Log.Warn(string.Format("subnode {0} not found", subnode));
            continue;
          }
          xsub.Attribute("name").SetValue(subnode.Name);
          xsub.Attribute("description").SetValue(subnode.Description);
          xsub.Attribute("location").SetValue(subnode.Location);
          xsub.Attribute("type").SetValue(subnode.Type);
          xsub.Attribute("hardware").SetValue(subnode.Hardware);
          xsub.Attribute("version").SetValue(subnode.Version);
          xsub.Attribute("inputs").SetValue(subnode.DigitalInputs);
          xsub.Attribute("analogs").SetValue(subnode.AnalogInputs);
          xsub.Attribute("counters").SetValue(subnode.CounterInputs);
          xsub.Attribute("outputs").SetValue(subnode.DigitalOutputs);
          xsub.Attribute("pwms").SetValue(subnode.PwmOutputs);
          xsub.Attribute("devices").SetValue(subnode.DevicesCount);
          xsub.Attribute("sensors").SetValue(subnode.SensorsCount);
        }
      }
      catch (Exception ex)
      {
        Log.Error(string.Format("{0} failed", MethodBase.GetCurrentMethod().Name), ex);
      }
    }
    #endregion

    /// <summary>
    /// Return configured object read from xml
    /// </summary>
    /// <param name="xobject">XML node with object constructor parameters</param>
    /// <returns>Configured object</returns>
    private object GetConfiguredObject(XElement xobject)
    {
      try
      {

        // ReSharper disable PossibleNullReferenceException
        if (xobject == null)
          return null;

        if (xobject.Attribute("type") == null)
          return null;

        var objectType = xobject.Attribute("type").Value;

        var type = Type.GetType(objectType);

        if (type == null)
          return null;

        var parameters = GetConstructorParameters(type, xobject.Element("parameters"));

        return Activator.CreateInstance(type, parameters);
      }
      catch (Exception ex)
      {
        Log.Error(string.Format("{0} failed", MethodBase.GetCurrentMethod().Name), ex);
      }
      return null;
    }
    /// <summary>
    /// Return configured object read from xml
    /// </summary>
    private object[] GetConstructorParameters(Type type, XElement xParameters)
    {
      // ReSharper disable PossibleNullReferenceException
      if (type == null)
        return null;

      //Search constructor with same parameters
      var parameters = (from c in type.GetConstructors() where xParameters.Elements().Count() == c.GetParameters().Length select c.GetParameters()).FirstOrDefault();

      if (parameters == null)
      {
        //Constructor without parameters or not found
        return null;
      }

      //Copy xml parameters
      var pars = new List<object>();

      foreach (var parameter in parameters)
      {
        //Read parameters from xml file
        foreach (var xpar in xParameters.Elements())
        {
          //Check parameter mandatory attributes
          if (xpar.Attribute("name") == null) continue;
          if (xpar.Attribute("type") == null) continue;
          //if (xpar.Attribute("value") == null) continue;

          var parType = Type.GetType(xpar.Attribute("type").Value);

          if ((parameter.ParameterType == parType || parType == null) && parameter.Name == xpar.Attribute("name").Value)
          {
            var value = xpar.Attribute("value") != null ? xpar.Attribute("value").Value : null;
            if (value != null)
            {
              if (value.StartsWith("@"))
              {
                switch (value)
                {
                  case "@hal":
                    pars.Add(Hal);
                    break;
                  case "@bus":
                    pars.Add(Bus);
                    break;
                  case "@scheduler":
                    pars.Add((object)Scheduler);
                    break;
                  case "@node":
                    pars.Add(Node);
                    break;
                  case "@configurator":
                    pars.Add(this);
                    break;
                }
              }
              else
              {
                object val = parameter.ParameterType.IsEnum
                    ? Enum.Parse(parameter.ParameterType, value)
                    : Convert.ChangeType(value, parameter.ParameterType);

                if (val != null)
                {
                  pars.Add(val);
                }
                else
                {
                  if (parameter.IsOptional)
                    pars.Add(parameter.DefaultValue);
                }
              }
            }
            else
            {
              if (parameter.IsOptional)
                pars.Add(parameter.DefaultValue);
              else
                return null;

            }
            break;
          }
        }
      }

      //return object with parameters
      return pars.ToArray();
    }
  }
}