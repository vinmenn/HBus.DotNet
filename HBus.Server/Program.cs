using System;
using System.Collections.Generic;
using System.Configuration;
using System.Reflection;
using System.Threading;
using HBus.Nodes;
using HBus.Nodes.Common;
using HBus.Nodes.Sensors;
using HBus.Ports;
using HBus.Server.Data;
using HBus.Server.Processors;
using HBus.Utilities;
using log4net;
using log4net.Config;

namespace HBus.Server
{
  class Program
  {
    protected static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

    private const int Interval = 10;

    static void Main(string[] args)
    {
      //Log init
      XmlConfigurator.Configure();

      Console.WriteLine("HBus.Server v." + Assembly.GetExecutingAssembly().GetName().Version);

      //TODO: configurator from db/xml

      #region HBus scheduler processor
      Console.WriteLine("..Configuring Scheduler processor");
      var sp = new SchedulerProcessor(new[]
      {
        //Read sensor 1 from console node
        new EventSchedule(DateTime.Now.AddSeconds(10), new Event()
        {
            Name = "sensor-read",
            Address = "3",
            Channel = "hbus",
            MessageType = "event",
            Source = "SN301",
        }, ScheduleTypes.Period, 600),

        //Read sensor 2 from console node
        new EventSchedule(DateTime.Now.AddSeconds(15), new Event()
        {
            Name = "sensor-read",
            Address = "3",
            Channel = "hbus",
            MessageType = "event",
            Source = "SN303",
        }, ScheduleTypes.Period, 600),

        //Read sensor 0 from remote Arduino node
        new EventSchedule(DateTime.Now.AddSeconds(20), new Event()
        {
            Name = "sensor-read",
            Address = "7",
            Channel = "hbus",
            MessageType = "event",
            Source = "SN701",
        }, ScheduleTypes.Period, 600),
      });
      #endregion

      #region HBus commands processor
      Console.WriteLine("..Configuring HBus processor");
      var hbHost = ConfigurationManager.AppSettings["processor.hbus.host"];
      //HBus controller
      var bus = new BusController(Address.HostAddress,
          new Port[] {
            //new PortTcp(hbHost,5000, 5001, 0),
            new PortZMq("tcp://*:5555","tcp://127.0.0.1:5556", 0)
          });
      var hbusEp = new HBusProcessor(bus);
      hbusEp.OnSourceEvent(new Event()
      {
        Name = "pin-subscribe",
        MessageType = "event",
        Channel = "hbus",
        Source = "CS201",
        Address = "2"
      }, null);

      #endregion

      Console.WriteLine("..Configuring websocket processor");
      var wsEp = new WebsocketProcessor("ws://0.0.0.0:5050");

      #region ThingSpeak processor
      Console.WriteLine("..Configuring ThingSpeak processor");
      var tsKey = ConfigurationManager.AppSettings["processor.thingspeak.key"];
      var tsEp = new ThingSpeakProcessor(tsKey, new[] { "SN303", "SN701" });
      #endregion

      #region Artik cloud service
      Console.WriteLine("..Configuring Artik processor");
      var deviceTypeId1 = ConfigurationManager.AppSettings["processor.artik.device1.type.id"];
      var deviceId1 = ConfigurationManager.AppSettings["processor.artik.device1.id"];
      var deviceToken1 = ConfigurationManager.AppSettings["processor.artik.device1.token"];
      var deviceTypeId2 = ConfigurationManager.AppSettings["processor.artik.device2.type.id"];
      var deviceId2 = ConfigurationManager.AppSettings["processor.artik.device2.id"];
      var deviceToken2 = ConfigurationManager.AppSettings["processor.artik.device2.token"];
      var deviceTypeId3 = ConfigurationManager.AppSettings["processor.artik.device3.type.id"];
      var deviceId3 = ConfigurationManager.AppSettings["processor.artik.device3.id"];
      var deviceToken3 = ConfigurationManager.AppSettings["processor.artik.device3.token"];

      var tempType = new ArtikDeviceType()
      {
        Id = deviceTypeId1,
        Name = "",
        UniqueName = "",
        Description = "",
        Fields = new ArtikDeviceField[]
          {
                    new ArtikDeviceField() {Name = "timestamp", Type="Double", Unit="ms" },
                    new ArtikDeviceField() {Name = "value", Type="Double", Unit="°C" },
          }
      };
      var shutterType = new ArtikDeviceType()
      {
        Id = deviceTypeId2,
        Name = "",
        UniqueName = "",
        Description = "",
        Fields = new ArtikDeviceField[]
          {
                    new ArtikDeviceField() {Name = "status", Type="String" },
          }
      };
      var buttonType = new ArtikDeviceType()
      {
        Id = deviceTypeId3,
        Name = "",
        UniqueName = "",
        Description = "",
        Fields = new ArtikDeviceField[]
          {
                    new ArtikDeviceField() {Name = "isActive", Type="Boolean" },
                    new ArtikDeviceField() {Name = "value", Type="Integer" },
          }
      };

      var artikEp = new ArtikProcessor(
          new[] {
                    new ArtikDevice()
                    { Id = deviceId1,
                      Token = deviceToken1,
                      Type = tempType,
                      Name = "external temperature sensor",
                      Source = "SN701",
                      Address = "7",
                      Channel = "hbus"
                    },
                    new ArtikDevice()
                    { Id = deviceId2,
                      Token = deviceToken2,
                      Type = shutterType,
                      Name = "my shutter",
                      Source = "DS301",
                      Address = "3",
                      Channel = "hbus"
                    },
                    new ArtikDevice()
                    { Id = deviceId3,
                      Token = deviceToken3,
                      Type = buttonType,
                      Name = "test button",
                      Source = "CS201",
                      Address = "2",
                      Channel = "hbus"
                    },

          }
      );
      #endregion

      //Scheduler => HBus
      Console.WriteLine("..Connect scheduler to HBus");
      sp.AddSubscriber(hbusEp);

      //HBus => websocket
      Console.WriteLine("..Connect websocket to HBus");
      hbusEp.AddSubscriber(wsEp);

      //websocket => HBus
      Console.WriteLine("..Connect HBus to websocket");
      wsEp.AddSubscriber(hbusEp);

      //HBus => ThingsSpeak
      Console.WriteLine("..Connect ThingsSpeak to HBus");
      hbusEp.AddSubscriber(tsEp);

      //HBus => Artik Cloud
      Console.WriteLine("..Connect Artik Cloud to HBus");
      hbusEp.AddSubscriber(artikEp);

      //Artik Cloud => HBus
      Console.WriteLine("..Connect HBus to Artik Cloud ");
      artikEp.AddSubscriber(hbusEp);

      //Scheduler
      var scheduler = Scheduler.GetScheduler();

      Console.WriteLine("Starting bus controller, scheduler and endpoints");
      bus.Open();
      scheduler.Start();

      hbusEp.Start();
      wsEp.Start();
      tsEp.Start();
      artikEp.Start();

      Console.WriteLine("Press enter to stop");
      Console.ReadLine();
      Console.WriteLine("Stopping endpoints, scheduler and bus controller");

      wsEp.Stop();
      hbusEp.Stop();
      scheduler.Stop();
      bus.Close();

    }
  }
}
