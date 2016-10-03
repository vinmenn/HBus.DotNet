using System;
using System.Collections.Generic;
using System.Configuration;
using System.Reflection;
using System.Threading;
using HBus.Nodes;
using HBus.Nodes.Common;
using HBus.Nodes.Devices;
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

      #region HBus scheduler processor: schedule all sensors readings
      Console.WriteLine("..Configuring Scheduler processor");
      var interval = Convert.ToInt32(ConfigurationManager.AppSettings["processor.scheduler.interval"]);
      var sproc = new SchedulerProcessor(new[]
      {
        //Read sensor 1 from console node
        new EventSchedule(DateTime.Now.AddSeconds(10), new Event()
        {
            Name = "sensor-read",
            Address = "2",
            Channel = "hbus",
            MessageType = "event",
            Source = "SN201",
        }, ScheduleTypes.Period, interval),

        //Read sensor 2 from console node
        new EventSchedule(DateTime.Now.AddSeconds(15), new Event()
        {
            Name = "sensor-read",
            Address = "2",
            Channel = "hbus",
            MessageType = "event",
            Source = "SN202",
        }, ScheduleTypes.Period, interval),

        //Read sensor 0 from remote Arduino node
        new EventSchedule(DateTime.Now.AddSeconds(20), new Event()
        {
            Name = "sensor-read",
            Address = "7",
            Channel = "hbus",
            MessageType = "event",
            Source = "SN701",
        }, ScheduleTypes.Period, interval),
        //Read sensor 1 from remote Arduino node
        new EventSchedule(DateTime.Now.AddSeconds(25), new Event()
        {
            Name = "sensor-read",
            Address = "7",
            Channel = "hbus",
            MessageType = "event",
            Source = "SN702",
        }, ScheduleTypes.Period, interval),
        //Read sensor 3 from remote Arduino node
        new EventSchedule(DateTime.Now.AddSeconds(30), new Event()
        {
            Name = "sensor-read",
            Address = "7",
            Channel = "hbus",
            MessageType = "event",
            Source = "SN703",
        }, ScheduleTypes.Period, interval),
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
      var hbusproc = new HBusProcessor(bus);
      hbusproc.OnSourceEvent(new Event()
      {
        Name = "pin-subscribe",
        MessageType = "event",
        Channel = "hbus",
        Source = "LS201",
        Address = "2"
      }, null);

      #endregion

      #region Websocket processor
      Console.WriteLine("..Configuring websocket processor");
      var wsproc = new WebsocketProcessor("ws://0.0.0.0:5050");
      #endregion

      #region ThingSpeak processor
      Console.WriteLine("..Configuring ThingSpeak processor");
      var tsKey = ConfigurationManager.AppSettings["processor.thingspeak.key"];
      var tsproc = new ThingSpeakProcessor(tsKey, new[] { "SN303", "SN701" });
      #endregion

      #region Artik processor
      Console.WriteLine("..Configuring Artik processor");
      //Local console sensor
      var deviceTypeId1 = ConfigurationManager.AppSettings["processor.artik.device1.type.id"];
      var deviceId1 = ConfigurationManager.AppSettings["processor.artik.device1.id"];
      var deviceToken1 = ConfigurationManager.AppSettings["processor.artik.device1.token"];
      var deviceName1 = ConfigurationManager.AppSettings["processor.artik.device1.name"];
      var deviceSource1 = ConfigurationManager.AppSettings["processor.artik.device1.source"];
      var deviceAddress1 = ConfigurationManager.AppSettings["processor.artik.device1.address"];
      //Remote arduino sensor temperature
      var deviceTypeId2 = ConfigurationManager.AppSettings["processor.artik.device2.type.id"];
      var deviceId2 = ConfigurationManager.AppSettings["processor.artik.device2.id"];
      var deviceToken2 = ConfigurationManager.AppSettings["processor.artik.device2.token"];
      var deviceName2 = ConfigurationManager.AppSettings["processor.artik.device2.name"];
      var deviceSource2 = ConfigurationManager.AppSettings["processor.artik.device2.source"];
      var deviceAddress2 = ConfigurationManager.AppSettings["processor.artik.device2.address"];
      //Remote arduino sensor humidity
      var deviceTypeId3 = ConfigurationManager.AppSettings["processor.artik.device3.type.id"];
      var deviceId3 = ConfigurationManager.AppSettings["processor.artik.device3.id"];
      var deviceToken3 = ConfigurationManager.AppSettings["processor.artik.device3.token"];
      var deviceName3 = ConfigurationManager.AppSettings["processor.artik.device3.name"];
      var deviceSource3 = ConfigurationManager.AppSettings["processor.artik.device3.source"];
      var deviceAddress3 = ConfigurationManager.AppSettings["processor.artik.device3.address"];
      //Remote arduino sensor light
      var deviceTypeId4 = ConfigurationManager.AppSettings["processor.artik.device4.type.id"];
      var deviceId4 = ConfigurationManager.AppSettings["processor.artik.device4.id"];
      var deviceToken4 = ConfigurationManager.AppSettings["processor.artik.device4.token"];
      var deviceName4 = ConfigurationManager.AppSettings["processor.artik.device4.name"];
      var deviceSource4 = ConfigurationManager.AppSettings["processor.artik.device4.source"];
      var deviceAddress4 = ConfigurationManager.AppSettings["processor.artik.device4.address"];
      //Node console shutter device
      var deviceTypeId5 = ConfigurationManager.AppSettings["processor.artik.device5.type.id"];
      var deviceId5 = ConfigurationManager.AppSettings["processor.artik.device5.id"];
      var deviceToken5 = ConfigurationManager.AppSettings["processor.artik.device5.token"];
      var deviceName5 = ConfigurationManager.AppSettings["processor.artik.device5.name"];
      var deviceSource5 = ConfigurationManager.AppSettings["processor.artik.device5.source"];
      var deviceAddress5 = ConfigurationManager.AppSettings["processor.artik.device5.address"];
      //Node console output pin
      var deviceTypeId6 = ConfigurationManager.AppSettings["processor.artik.device6.type.id"];
      var deviceId6 = ConfigurationManager.AppSettings["processor.artik.device6.id"];
      var deviceToken6 = ConfigurationManager.AppSettings["processor.artik.device6.token"];
      var deviceName6 = ConfigurationManager.AppSettings["processor.artik.device6.name"];
      var deviceSource6 = ConfigurationManager.AppSettings["processor.artik.device6.source"];
      var deviceAddress6 = ConfigurationManager.AppSettings["processor.artik.device6.address"];

      var artikproc = new ArtikProcessor(
        new[]
        {
          new ArtikEvent(deviceId1, deviceTypeId1, deviceToken1, deviceName1, deviceSource1, deviceAddress1, "hbus"),
          new ArtikEvent(deviceId2, deviceTypeId2, deviceToken2, deviceName2, deviceSource2, deviceAddress2, "hbus"),
          new ArtikEvent(deviceId3, deviceTypeId3, deviceToken3, deviceName3, deviceSource3, deviceAddress3, "hbus"),
          new ArtikEvent(deviceId4, deviceTypeId4, deviceToken4, deviceName4, deviceSource4, deviceAddress4, "hbus"),
          new ArtikEvent(deviceId5, deviceTypeId5, deviceToken5, deviceName5, deviceSource5, deviceAddress5, "hbus"),
          new ArtikEvent(deviceId6, deviceTypeId6, deviceToken6, deviceName6, deviceSource6, deviceAddress6, "hbus"),
        }
      );
      #endregion

      #region UsbUirt processor
#if USE_USBUIRT
      /*
         1500002D1FCA power
         170000821ECA 1
         150000231ECA 2       
       */
      var ircmd = new IrCommandEvent
      {
        IrCode = "170000821ECA", //button 1 on remote
        Name = "pin-activate",
        MessageType = "event",
        Channel = "hbus",
        Source = "CS201",
        Address = "2"
      };
      var irproc = new UsbUirtProcessor(new List<IrCommandEvent> { ircmd });
#endif
      #endregion

      Console.WriteLine("Build processors connections");

      //Scheduler => HBus
      Console.WriteLine("..Connect scheduler to HBus");
      sproc.AddSubscriber(hbusproc);

      //HBus => websocket
      Console.WriteLine("..Connect websocket to HBus");
      hbusproc.AddSubscriber(wsproc);

      //websocket => HBus
      Console.WriteLine("..Connect HBus to websocket");
      wsproc.AddSubscriber(hbusproc);

      //HBus => ThingsSpeak
      Console.WriteLine("..Connect ThingsSpeak to HBus");
      hbusproc.AddSubscriber(tsproc);

      //HBus => Artik Cloud
      Console.WriteLine("..Connect Artik Cloud to HBus");
      hbusproc.AddSubscriber(artikproc);

      //Artik Cloud => HBus
      Console.WriteLine("..Connect HBus to Artik Cloud ");
      artikproc.AddSubscriber(hbusproc);

#if USE_USBUIRT
      //UsbUirt => HBus
      Console.WriteLine("..Connect UsbUirt to HBus");
      irproc.AddSubscriber(hbusproc);
#endif

      Console.WriteLine("Starting processors");
      hbusproc.Start();
      wsproc.Start();
      tsproc.Start();
      artikproc.Start();
#if USE_USBUIRT
      irproc.Start();
#endif
      Console.WriteLine("Press enter to stop");
      Console.ReadLine();
      Console.WriteLine("Stopping processors");

      wsproc.Stop();
      hbusproc.Stop();
#if USE_USBUIRT
      irproc.Stop();
#endif

    }
  }
}
