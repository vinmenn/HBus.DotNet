using System;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Threading;
using HBus.Nodes;
using HBus.Nodes.Common;
using HBus.Nodes.Configuration;
using HBus.Nodes.Devices;
using HBus.Nodes.Pins;
using HBus.Nodes.Sensors;
using HBus.Utilities;
using log4net;
using System.Collections.Generic;

namespace HBus.ConsoleNode
{
  internal class Program
  {
    protected static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
    private static Node _node;
    private static INodeConfigurator _configurator;
    private static BusController _bus;

    //Only for demo
    private static IList<Sensor> _extSensors;

    private static void Main(string[] args)
    {
      log4net.Config.XmlConfigurator.Configure();

#if DEBUG
      //set log level to DEBUG
      var logLevel = log4net.Core.Level.Debug;
      ((log4net.Repository.Hierarchy.Hierarchy)LogManager.GetRepository()).Root.Level = logLevel;
      ((log4net.Repository.Hierarchy.Hierarchy)LogManager.GetRepository()).RaiseConfigurationChanged(EventArgs.Empty);
#endif

      Console.WriteLine("-----------------------------------------");
      Console.WriteLine("HBus Console node version {0}", Assembly.GetExecutingAssembly().GetName().Version);
      Console.WriteLine("-----------------------------------------");

      //Startup parameters
      Console.WriteLine("Configuring node.");
      var defaultConfig = Convert.ToBoolean(ConfigurationManager.AppSettings["node.defaultConfiguration"]);
      var xconfig = ConfigurationManager.AppSettings["node.configurationFile"];

      //Create configurator
      _configurator = new XmlConfigurator(xconfig);

      //Configure all global objects
      _configurator.Configure(defaultConfig);

      //Get the node
      _node = _configurator.Node;
      _bus = _configurator.Bus;
#if DEBUG
      //Add handlers to bus
      _bus.OnMessageReceived += (source, message) =>
      {
        Console.WriteLine("Received message from {0} to {1} = {2} ({3})", message.Source, message.Destination, message.Command, message.Flags);
      };
#endif

      //Configure remote sensors for demo porpouses
      _extSensors = new List<Sensor>() {
        new Sensor() { Name = "SN701", Address = Address.Parse(7) },
        new Sensor() { Name = "SN702", Address = Address.Parse(7) },
    };

      //Add handlers to sensor events
      _node.OnSensorRead += ( Address arg1, SensorRead arg2) =>
      {
        Console.WriteLine("Sensor read from {0} : {1} = {2} @ {3}", arg1, arg2.Name, arg2.Value, arg2.Time);
      };

      Console.WriteLine("Reset node.");
      //Full reset
      _node.Reset(true);

      Console.WriteLine("Start node.");
      //Start node
      _node.Start();

      Console.WriteLine("Node {0} started", _node.Name);
      Console.WriteLine("Press i [enter] for info.");
      Console.WriteLine("Press x [enter] to exit.");

      #region local test section
      //Main loop
      while (true)
      {
        var w = Console.ReadLine();
        Pin pin;
        Device device;

        switch (w)
        {
          #region info
          case "i":
            Console.WriteLine(string.Format("-----------------------------------------"));
            Console.WriteLine(string.Format("HBus Console node version {0}", Assembly.GetExecutingAssembly().GetName().Version));
            Console.WriteLine(string.Format("-----------------------------------------"));
            Console.WriteLine(string.Format("Node commands:"));
            Console.WriteLine(string.Format("\t0: send ping to host node"));
            Console.WriteLine(string.Format("\t1: activate input pin {0}", _node.Pins[0].Name));
            Console.WriteLine(string.Format("\t2: activate input pin {0}", _node.Pins[1].Name));
            Console.WriteLine(string.Format("\t3: activate input pin {0}", _node.Pins[2].Name));
            Console.WriteLine(string.Format("\t4: activate input pin {0}", _node.Pins[3].Name));
            Console.WriteLine(string.Format("\t5: activate input pin {0}", _node.Pins[4].Name));
            Console.WriteLine(string.Format("\t6: activate input pin {0}", _node.Pins[5].Name));
            Console.WriteLine(string.Format("\t7: activate input pin {0}", _node.Pins[6].Name));
            Console.WriteLine(string.Format("\t8: activate input pin {0}", _node.Pins[7].Name));
            Console.WriteLine(string.Format("\tq: activate output pin {0}", _node.Pins[8].Name));
            Console.WriteLine(string.Format("\tw: activate output pin {0}", _node.Pins[9].Name));
            Console.WriteLine(string.Format("\te: activate output pin {0}", _node.Pins[10].Name));
            Console.WriteLine(string.Format("\tr: activate output pin {0}", _node.Pins[11].Name));
            Console.WriteLine(string.Format("\ta: device {0} action open", _node.Devices[0].Name));
            Console.WriteLine(string.Format("\tb: device {0} action close", _node.Devices[0].Name));
            Console.WriteLine(string.Format("\tc: device {0} action open", _node.Devices[1].Name));
            Console.WriteLine(string.Format("\td: device {0} action close", _node.Devices[1].Name));
            Console.WriteLine(string.Format("\tQ: read remote sensor {0} @ {1}", _extSensors[0].Name, _extSensors[0].Address));
            Console.WriteLine(string.Format("\tW: read remote sensor {0} @ {1}", _extSensors[1].Name, _extSensors[1].Address));
            Console.WriteLine(string.Format("\tE: read local sensor {0}", _node.Sensors[0].Name));
            Console.WriteLine(string.Format("\tR: read local sensor {0}", _node.Sensors[1].Name));
            if (_node.Sensors.Count > 2)
              Console.WriteLine(string.Format("\tT: read local sensor {0}", _node.Sensors[2].Name));
            Console.WriteLine(string.Format("\tx: exit"));
            break;
          #endregion

          #region input pins
          case "1":
            pin = _node.Pins[0];
            pin.Activate();
            break;
          case "2":
            pin = _node.Pins[1];
            pin.Activate();
            break;
          case "3":
            pin = _node.Pins[2];
            pin.Activate();
            break;
          case "4":
            pin = _node.Pins[3];
            pin.Activate();
            break;
          case "5":
            pin = _node.Pins[4];
            pin.Activate();
            break;
          case "6":
            pin = _node.Pins[5];
            pin.Activate();
            break;
          case "7":
            pin = _node.Pins[6];
            pin.Activate();
            break;
          case "8":
            pin = _node.Pins[7];
            pin.Activate();
            break;
          #endregion

          #region output pins
          case "q":
            pin = _node.Pins[8];
            pin.Activate();
            break;
          case "w":
            pin = _node.Pins[8];
            pin.Activate();
            break;
          case "e":
            pin = _node.Pins[8];
            pin.Activate();
            break;
          case "r":
            pin = _node.Pins[8];
            pin.Activate();
            break;
          #endregion

          #region devices
          case "a":
            device = _node.Devices[0];
            device.ExecuteAction(new DeviceAction() { Device = device.Name, Action = "open" });
            break;
          case "b":
            device = _node.Devices[0];
            device.ExecuteAction(new DeviceAction() { Device = device.Name, Action = "close" });
            break;
          case "c":
            device = _node.Devices[1];
            device.ExecuteAction(new DeviceAction() { Device = device.Name, Action = "open" });
            break;
          case "d":
            device = _node.Devices[1];
            device.ExecuteAction(new DeviceAction() { Device = device.Name, Action = "close" });
            break;
          #endregion

          #region sensors
          case "Q"://remote sensor
            readSensor(_extSensors[0]);
            break;
          case "W"://remote sensor
            readSensor(_extSensors[1]);
            break;
          case "E":// local sensor
            readSensor(_node.Sensors[0]);
            break;
          case "R":// local sensor
            readSensor(_node.Sensors[1]);
            break;
          case "T":// local sensor
            readSensor(_node.Sensors[2]);
            break;
          #endregion

          case "x":
            Console.WriteLine("Closing node...");
            _node.Close();
            _node = null;

            Console.WriteLine("Bye.");
            return;
        }

      }

    }
    #endregion

    private static void readSensor(Sensor sensor)
    {
      Console.WriteLine("read sensor {0}...", sensor.Name);
      if (sensor.Address != _node.Address)
      {
        var data = FixedString.ToArray(sensor.Name, HBusSettings.NameLength);
        _bus.SendCommand(NodeCommands.CMD_READ_SENSOR, sensor.Address, data);

        Console.WriteLine("remote sensor {0} sent read command", sensor.Name);
      }
      else
      {
        var read = sensor.Read();

        Console.WriteLine("local sensor {0} read = {1} @ {2}", read.Name, read.Value, read.Time);
      }
    }
  }
}