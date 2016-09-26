using System;
using System.Collections.Generic;
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

      Console.WriteLine("..Configuring HBus processor");
      //HBus controller
      var bus = new BusController(Address.HostAddress,
          new Port[] {
            new PortTcp("INSERT REMOTE NODE IP",5000, 5001, 0),
          });
      var hbusEp = new HBusProcessor(bus);

      Console.WriteLine("..Configuring websocket processor");
      var wsEp = new WebsocketProcessor("ws://0.0.0.0:5050");

      Console.WriteLine("..Configuring ThingSpeak processor");
      var tsEp = new ThingSpeakProcessor("INSERT THINGSPEAK KEY", new[] { "SN303", "SN701" });

      #region Artik cloud service
      Console.WriteLine("..Configuring Artik processor");

      var tempType = new ArtikDeviceType()
      {
        Id = "INSERT DEVICE TYPE ID",
        Name = "",
        UniqueName = "",
        Description = "",
        Fields = new ArtikDeviceField[]
          {
                    new ArtikDeviceField() {Name = "timestamp", Type="Double", Unit="ms" },
                    new ArtikDeviceField() {Name = "value", Type="Double", Unit="°C" },
          }
      };
      var artikEp = new ArtikProcessor(
          new[] {
                    new ArtikDevice()
                    { Id = "INSERT DEVICE ID",
                      Token = "INSERT DEVICE TOKEN",
                      Type = tempType,
                      Name = "external temperature sensor",
                      Source = "SN701"
                    }
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

      return;

    }
  }
}
