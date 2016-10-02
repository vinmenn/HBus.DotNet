using System;
using System.Collections.Generic;
using System.Configuration;
using System.Reflection;
using HBus.Nodes;
using HBus.Nodes.Common;
using HBus.Nodes.Configuration;
using HBus.Nodes.Devices;
using HBus.Nodes.Pins;
using HBus.Nodes.Sensors;
using HBus.Utilities;
using log4net;
using log4net.Core;
using log4net.Repository.Hierarchy;
using XmlConfigurator = log4net.Config.XmlConfigurator;

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
      XmlConfigurator.Configure();

#if DEBUG
      //set log level to DEBUG
      var logLevel = Level.Debug;
      ((Hierarchy)LogManager.GetRepository()).Root.Level = logLevel;
      ((Hierarchy)LogManager.GetRepository()).RaiseConfigurationChanged(EventArgs.Empty);
#endif

      Console.WriteLine("-----------------------------------------");
      Console.WriteLine("HBus Console node version {0}", Assembly.GetExecutingAssembly().GetName().Version);
#if ARTIK_DEMO_LOCAL
      Console.WriteLine("ARTIK DEMO with local node");
#endif
#if ARTIK_DEMO_RASPBERRY
      Console.WriteLine("ARTIK DEMO with raspberry node");
#endif
      Console.WriteLine("-----------------------------------------");

      //Startup parameters
      Console.WriteLine("Configuring node.");
      var defaultConfig = Convert.ToBoolean(ConfigurationManager.AppSettings["node.defaultConfiguration"]);
      var xconfig = ConfigurationManager.AppSettings["node.configurationFile"];

      //Create configurator
      _configurator = new Nodes.Configuration.XmlConfigurator(xconfig);

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
#if ARTIK_DEMO_LOCAL || ARTIK_DEMO_RASPBERRY
      var addr7 = Address.Parse(7);
      _extSensors = new List<Sensor>
      {
        new Sensor { Name = "SN701", Address = addr7 }, //arduino node DHT11 temperature
        new Sensor { Name = "SN702", Address = addr7 }, //arduino node DHT11 humidity
        new Sensor { Name = "SN703", Address = addr7 } //arduino node photoresistor light
    };
#endif
      //Add handlers to sensor events
      _node.OnSensorRead += (arg1, arg2) =>
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
          case "0":
            _bus.SendCommand(NodeCommands.CMD_PING, Address.HostAddress);
            break;

#if ARTIK_DEMO_LOCAL || ARTIK_DEMO_RASPBERRY
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
            pin = _node.Pins[9];
            pin.Activate();
            break;
          case "e":
            pin = _node.Pins[10];
            pin.Activate();
            break;
          case "r":
            pin = _node.Pins[11];
            pin.Activate();
            break;
          #endregion
#endif

          #region devices
#if ARTIK_DEMO_LOCAL || ARTIK_DEMO_RASPBERRY
          case "t":
            device = _node.Devices[0];
            device.ExecuteAction(new DeviceAction { Device = device.Name, Action = "open" });
            break;
          case "y":
            device = _node.Devices[0];
            device.ExecuteAction(new DeviceAction { Device = device.Name, Action = "close" });
            break;
          case "u":
            device = _node.Devices[1];
            device.ExecuteAction(new DeviceAction { Device = device.Name, Action = "open" });
            break;
          case "i":
            device = _node.Devices[1];
            device.ExecuteAction(new DeviceAction { Device = device.Name, Action = "close" });
            break;
#endif
          #endregion

          #region sensors
#if ARTIK_DEMO_LOCAL || ARTIK_DEMO_RASPBERRY
          case "a":// local sensor
            readSensor(_node.Sensors[0]);
            break;
          case "s":// local sensor
            readSensor(_node.Sensors[1]);
            break;
          case "d"://remote sensor
            readSensor(_extSensors[0]);
            break;
          case "f"://remote sensor
            readSensor(_extSensors[1]);
            break;
          case "g"://remote sensor
            readSensor(_extSensors[2]);
            break;
#endif
          #endregion

          case "h":
            ShowHelp();
            break;

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

    private static void ShowHelp()
    {
      Console.WriteLine("-----------------------------------------");
      Console.WriteLine("HBus Console node version {0}", Assembly.GetExecutingAssembly().GetName().Version);
#if ARTIK_DEMO_LOCAL
      Console.WriteLine("ARTIK DEMO with local node");
#endif
#if ARTIK_DEMO_RASPBERRY
      Console.WriteLine("ARTIK DEMO with raspberry node");
#endif
      Console.WriteLine("-----------------------------------------");
      Console.WriteLine("");
      Console.WriteLine("Node commands:");
      Console.WriteLine("\t0: send ping to host node");
#if ARTIK_DEMO_LOCAL || ARTIK_DEMO_RASPBERRY
      Console.WriteLine("\t1: activate input pin {0}", _node.Pins[0].Name);// button 0
      Console.WriteLine("\t2: activate input pin {0}", _node.Pins[1].Name);// button 1
      Console.WriteLine("\t3: activate input pin {0}", _node.Pins[2].Name);// button 2
      Console.WriteLine("\t4: activate input pin {0}", _node.Pins[3].Name);// button 3
      Console.WriteLine("\t5: activate input pin {0}", _node.Pins[4].Name);// button 4
      Console.WriteLine("\t6: activate input pin {0}", _node.Pins[5].Name);// button 5
      Console.WriteLine("\t7: activate input pin {0}", _node.Pins[6].Name);// button 6
      Console.WriteLine("\t8: activate input pin {0}", _node.Pins[7].Name);// button 7
      Console.WriteLine("\tq: activate output pin {0}", _node.Pins[8].Name);// output 0
      Console.WriteLine("\tw: activate output pin {0}", _node.Pins[9].Name);// output 1
      Console.WriteLine("\te: activate output pin {0}", _node.Pins[10].Name);// output 2
      Console.WriteLine("\tr: activate output pin {0}", _node.Pins[11].Name);// output 3
      Console.WriteLine("\tt: device {0} action open", _node.Devices[0].Name);
      Console.WriteLine("\ty: device {0} action close", _node.Devices[0].Name);
      Console.WriteLine("\tu: device {0} action open", _node.Devices[1].Name);
      Console.WriteLine("\ti: device {0} action close", _node.Devices[1].Name);
      Console.WriteLine("\ta: read local sensor {0}", _node.Sensors[0].Address);
      Console.WriteLine("\ts: read local sensor {0}", _node.Sensors[1].Address);
      Console.WriteLine("\td: read remote sensor {0} @ {1}", _extSensors[0].Name, _extSensors[0].Address);
      Console.WriteLine("\tf: read remote sensor {0} @ {1}", _extSensors[1].Name, _extSensors[1].Address);
      Console.WriteLine("\tg: read remote sensor {0} @ {1}", _extSensors[2].Name, _extSensors[2].Address);
#endif
      Console.WriteLine("\th: show this help");
      Console.WriteLine("\tx: exit");
    }
  }
}