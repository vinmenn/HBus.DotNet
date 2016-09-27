using System;
using System.Collections.Generic;
using HBus.Nodes;
using HBus.Nodes.Common;
using HBus.Nodes.Devices;
using HBus.Nodes.Pins;
using HBus.Nodes.Sensors;
using HBus.Server.Data;
using HBus.Utilities;

namespace HBus.Server.Processors
{
  /// <summary>
  /// HBus events processor
  /// Receives following messages from HBus and forward to subscribers:
  ///     CMD_PUSH_PIN_EVENT      => pin-change
  ///     CMD_PUSH_DEVICE_EVENT   => device-event
  ///     CMD_PUSH_SENSOR_READ    => sensor-read
  ///     CMD_READ_PIN            => pin-read
  ///     CMD_GET_DEVICE_STATUS   => device-status
  ///     CMD_READ_SENSOR         => sesnor-read
  /// 
  /// Transmit following commands from subscribers:
  ///     node-subscribe          => CMD_ADD_NODE_LISTENER
  ///     node-unsubscribe        => CMD_DELETE_NODE_LISTENER
  ///     pin-activate            => CMD_ACTIVATE, 
  ///     pin-deactivate          => CMD_DEACTIVATE
  ///     pin-subscribe           => CMD_ADD_PIN_LISTENER
  ///     pin-unsubscribe         => CMD_DELETE_PIN_LISTENER
  ///     device-subscribe        => CMD_ADD_DEVICE_LISTENER
  ///     device-unsubscribe      => CMD_DELETE_DEVICE_LISTENER
  ///     sensor-subscribe        => CMD_ADD_SENSOR_LISTENER
  ///     sensor-unsubscribe      => CMD_DELETE_SENSOR_LISTENER
  ///     sensor-read             => CMD_PUSH_SENSOR_READ
  /// 
  /// </summary>
  public class HBusProcessor : BaseProcessor
  {
    private const string Channel = "hbus";
    private readonly BusController _bus;
    private readonly Scheduler _scheduler;

    private readonly Dictionary<string, IList<Event>> _hbusEvents;

    public HBusProcessor(BusController hbus)
    {
      _bus = hbus;
      _bus.CommandReceived += OnCommandReceived;
      _bus.AckReceived += OnAckReceived;
      _scheduler = Scheduler.GetScheduler();
      _hbusEvents = new Dictionary<string, IList<Event>>();
      //Event from ep source
      OnSourceEvent = (@event, point) =>
      {
        if (@event.Channel != Channel && !string.IsNullOrEmpty(@event.Channel)) return;

        var address = !string.IsNullOrEmpty(@event.Address) ? Address.Parse(@event.Address) : Address.BroadcastAddress;

        var stack = new SimpleStack();

        ////Create new page event list
        //if (!_hbusEvents.ContainsKey(@event.Subscriber))
        //    _hbusEvents.Add(@event.Subscriber, new List<Event>());

        switch (@event.Name)
        {
          case "node-subscribe":
            _bus.SendCommand(NodeCommands.CMD_ADD_NODE_LISTENER, address);
            break;
          case "node-unsubscribe":
            _bus.SendCommand(NodeCommands.CMD_DELETE_NODE_LISTENER, address);
            break;
          case "pin-activate":
            stack.PushName(@event.Source);
            _bus.SendCommand(NodeCommands.CMD_ACTIVATE, address, stack.Data);
            break;
          case "pin-deactivate":
            stack.PushName(@event.Source);
            _bus.SendCommand(NodeCommands.CMD_DEACTIVATE, address, stack.Data);
            break;
          case "pin-subscribe":
            stack.PushName(@event.Source);
            _bus.SendCommand(NodeCommands.CMD_ADD_PIN_LISTENER, address, stack.Data);
            break;
          case "pin-unsubscribe":
            stack.PushName(@event.Source);
            _bus.SendCommand(NodeCommands.CMD_DELETE_PIN_LISTENER, address, stack.Data);
            break;
          case "device-subscribe":
            stack.PushName(@event.Source);
            _bus.SendCommand(NodeCommands.CMD_ADD_DEVICE_LISTENER, address, stack.Data);
            break;
          case "device-unsubscribe":
            stack.PushName(@event.Source);
            _bus.SendCommand(NodeCommands.CMD_DELETE_DEVICE_LISTENER, address, stack.Data);
            break;
          case "sensor-subscribe":
            //This is extracted only for explantion
            //@event.Data could be used as it is
            var interval = @event.Data[0];
            var expires = (ushort) (@event.Data[2] << 8 + @event.Data[1]);
            stack.PushName(@event.Source);
            stack.Push(interval);
            stack.Push(expires);
            _bus.SendCommand(NodeCommands.CMD_ADD_SENSOR_LISTENER, address, stack.Data);
            break;
          case "sensor-unsubscribe":
            stack.PushName(@event.Source);
            _bus.SendCommand(NodeCommands.CMD_DELETE_SENSOR_LISTENER, address, stack.Data);
            break;
          case "sensor-read":
            stack.PushName(@event.Source);
            _bus.SendCommand(NodeCommands.CMD_READ_SENSOR, address, stack.Data);
            break;
          //TODO: other HBus commands
          default:
            if (@event.Name.IndexOf("device-") == 0)
            {
              //Send device action
              var devaction = new DeviceAction(@event.Source, @event.Name.Substring(7), @event.Data);
              _bus.SendCommand(NodeCommands.CMD_EXECUTE_DEVICE_ACTION, address, devaction.ToArray());
            }
            break;
        }
      };

      //Error from ep source
      OnSourceError = (exception, sender) =>
      {
        Log.Error("Error from source endpoint", exception);
      };

      //Close connection with ep source
      OnSourceClose = (sender) =>
      {
        //Close HBus endpoint
        Stop();

        Log.Debug("closed on source close");
      };
    }

    #region HBus events

    /// <summary>
    /// Process HBus command messages
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="message"></param>
    /// <param name="port"></param>
    /// <returns></returns>
    private bool OnCommandReceived(object sender, Message message, int port)
    {
      Log.Debug(string.Format("HBus command from {0}:{1}", message.Source, port));

      try
      {
        Event evt = null;

        //Clear bus & node errors
        _bus.ClearErrors();

        switch (message.Command)
        {
          case NodeCommands.CMD_PUSH_PIN_EVENT:
            var pe = new PinEvent(message.Data);
            evt = new Event
            {
              Name = "pin-change",
              Source = pe.Pin,
              //Channel = "hbus",
              Value = pe.Value,
              Status = pe.IsActive ? "active" : "inactive",
              Timestamp = DateTime.Now
            };
            break;
          case NodeCommands.CMD_PUSH_DEVICE_EVENT:
            var de = new DeviceEvent(message.Data);
            evt = new Event
            {
              Name = "device-event",
              Source = de.Device,
              //Channel = "hbus",
              Status = de.Status,
              Data = de.Values,
              Timestamp = DateTime.Now
            };
            break;
          case NodeCommands.CMD_PUSH_SENSOR_READ:
            var sr = new SensorRead(message.Data);
            evt = new Event
            {
              Name = "sensor-read",
              Source = sr.Name,
              //Channel = "hbus",
              Value = sr.Value,
              Unit = "", //sr.Unit, //TODO: add to HBus protocol
              Timestamp = DateTime.Now
            };
            break;
        }

        if (evt != null)
          //Send event to subscribers
          Send(evt, this);

        //HBus command processed
        return true;
      }
      catch (Exception ex)
      {
        Log.Error(string.Format("HBus: error with command {0} from {1}", message.Command, message.Source), ex);

        //Propagate errors to observers
        Error(ex, this);
      }
      return false;
    }

    /// <summary>
    /// Process HBus ack messages
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="ack"></param>
    /// <param name="port"></param>
    /// <returns></returns>
    private bool OnAckReceived(object sender, Message ack, int port)
    {
      try
      {
        Event evt = null;

        //if (Status.NodeStatus != NodeStatusValues.Active) return false;
        if (ack.MessageType == MessageTypes.NackResponse)
        {
          var err = ack.Data.Length > 1 ? ack.Data[0] << 1 | ack.Data[1] : 0;
          Log.Debug(string.Format("HBus: nack from {0}:{1} with error {2}", ack.Source, port, err));
          return false;
        }

        Log.Debug(string.Format("HBus: ack from {0}:{1}", ack.Source, port));

        switch (ack.Command)
        {
          //case NodeCommands.CMD_GET_PIN_INFO:
          //    var pp = new Pin();
          //    var pi = PinSerializer.DeSerialize(ack.Data, ref pp);
          //    break;
          case NodeCommands.CMD_READ_PIN:
            var pe = new PinEvent(ack.Data);
            evt = new Event
            {
              Name = "pin-read",
              Source = pe.Pin,
              Channel = "hbus",
              Value = pe.Value,
              Status = pe.IsActive ? "active" : "inactive",
              Timestamp = DateTime.Now
            };
            break;
          case NodeCommands.CMD_GET_DEVICE_STATUS:
            var ds = new DeviceStatus(ack.Data);
            evt = new Event
            {
              Name = "device-status",
              Source = ds.Device,
              Channel = "hbus",
              Status = ds.Status,
              Timestamp = DateTime.Now
            };
            break;
          //case NodeCommands.CMD_GET_DEVICE_INFO:
          //    var dd = new Device();
          //    var di = DeviceSerializer.DeSerialize(ack.Data, ref dd);
          //    break;
          //case NodeCommands.CMD_GET_SENSOR_INFO:
          //    break;
          case NodeCommands.CMD_READ_SENSOR:
            var sr = new SensorRead(ack.Data);
            evt = new Event
            {
              Name = "sensor-read",
              Source = sr.Name,
              Channel = "hbus",
              Value = sr.Value,
              Unit = "", //sr.Unit, //TODO: add to HBus protocol
              Timestamp = DateTime.Now
            };
            break;
        }

        if (evt != null)
          //Send event to subscribers
          Send(evt, this);

        return true;
      }
      catch (Exception ex)
      {
        Log.Error(string.Format("HBus: error occurred with ack {0} from {1}", ack.Command, ack.Source), ex);

        //Propagate errors to observers
        Error(ex, this);
      }
      return false;
    }

    #endregion

    private static byte[] ConvertToBytes(object[] data)
    {
      byte[] barray = null;
      if (data != null && data.Length > 0)
      {
        barray = new byte[data.Length];
        for (var i = 0; i < data.Length; i++)
          barray[i] = (byte) data[i];
      }
      return barray;
    }

    private static object[] ConvertToObjects(byte[] data)
    {
      object[] barray = null;
      if (data != null && data.Length > 0)
      {
        barray = new object[data.Length];
        for (var i = 0; i < data.Length; i++)
          barray[i] = data[i];
      }
      return barray;
    }
  }
}
