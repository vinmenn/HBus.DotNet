using System;
using System.Collections.Generic;
using System.Globalization;
using HBus.Server.Data;
using Newtonsoft.Json;
using System.Threading;
using System.Linq;
using WebSocket4Net;
using HBus;
namespace HBus.Server.Processors
{
  #region TODO: use official Artik c# library
  public struct ArtikDeviceField
  {
    public string Name { get; set; }
    public string Type { get; set; }
    public string Unit { get; set; }
    public string[] Tags { get; set; }
  }

  public struct ArtikDeviceType
  {
    public string Id { get; set; }
    public string Name { get; set; }
    public string UniqueName { get; set; }
    public string Description { get; set; }
    public string[] Categories { get; set; }
    public string[] Tags { get; set; }
    public ArtikDeviceField[] Fields { get; set; }
  }

  public struct ArtikDevice
  {
    public ArtikDeviceType Type { get; set; }
    public string Id { get; set; }
    public string Token { get; set; }
    public string Name { get; set; }
    public string Source { get; set; }
    public string Address { get; set; }
    public string Channel { get; set; }
  }
  #endregion

  public class ArtikEvent : Event
  {
    public string Id { get; set; }
    public string TypeId { get; set; }
    public string Token { get; set; }

    public ArtikEvent(string id, string typeId, string token, string name, string source, string address, string channel)
    {
      Id = id;
      TypeId = typeId;
      Token = token;
      Name = name;
      Source = source;
      Address = address;
      Channel = channel;
    }
  }

  public class ArtikProcessor : BaseProcessor
  {
    private const string WebSocketUrl = "wss://api.artik.cloud/v1.1/websocket?ack=true";

    private WebSocket _client;
    private readonly IList<ArtikEvent> _events;

    public ArtikProcessor(IList<ArtikEvent> events)
    {
      _events = events;
      //Websocket start
      _client = new WebSocket(WebSocketUrl);
      _client.Opened += ClientOpened;
      _client.Closed += ClientClosed;
      _client.MessageReceived += MessageReceived;
    }

    #region Artik methods
    public override void Start()
    {
      try
      {
        //Event from source
        OnSourceEvent = SendDataToArtik;

        _client.Open();

        base.Start();
      }
      catch (Exception ex)
      {
        Log.Error("Failed to open websocket connection", ex);
      }
    }

    public override void Stop()
    {
      _client.Close();

      //Event from ep source
      OnSourceEvent = null;

      //_ws.Dispose();
      base.Stop();
    }

    private void RegisterAllDevices()
    {
      Log.Info("Registering artik devices on the websocket connection");
      try
      {
        foreach (var @event in _events)
        {

          var register = new
          {
            type = "register",
            sdid = @event.Id,
            Authorization = "bearer " + @event.Token,
            cid = TotalMilliseconds()
          };

          var json = JsonConvert.SerializeObject(register, Formatting.Indented);

          Log.Info("Sending register message " + json);

          _client.Send(json);

          Thread.Sleep(100);
        }
      }
      catch (Exception ex)
      {
        Log.Error("Failed to register messages. Error in registering message", ex);
      }
    }

    private long TotalMilliseconds()
    {
      return (long) (DateTime.Now.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0)).TotalMilliseconds;
      //return (long) (DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0)).TotalMilliseconds;
    }

    private void SendDataToArtik(Event @event, BaseProcessor sender)
    {
      try
      {

        string read = "";

        var myevent = _events.FirstOrDefault(e => e.Source == @event.Source);

        if (myevent == null)
        {
          Log.Warn(string.Format("Event for source {0} not found", @event.Source));
          return;
        }

        var val = @event.Value.ToString("0.00", CultureInfo.InvariantCulture);
        var time = ((double) @event.Timestamp.TimeOfDay.TotalSeconds).ToString("0.00", CultureInfo.InvariantCulture);
        switch (@event.Name)
        {
          case "sensor-read":
            read = string.Format("\"timestamp\" : {0}, \"value\" : {1}", time, val);
            break;
          case "pin-change":
            read = string.Format("\"timestamp\" : {0}, \"value\" : {1}, \"status\" : \"{2}\"", time,
              @event.Value, @event.Status);
            break;
          case "device-event":
            read = string.Format("\"timestamp\" : {0}, \"status\" : \"{1}\"", time, @event.Status);
            break;
          default:
            return;
        }

        var ts = TotalMilliseconds();

        var json = "{ " +
                   "\n\t\"sdid\": \"" + myevent.Id + "\"," +
                   "\n\t\"ts\": " + ts + "," +
                   "\n\t\"data\": {" + read + "}," +
                   "\n\t\"cid\": \"" + ts + "\"" +
                   "\n}";

        Log.Info("Sending artik data to the cloud: " + json);
        _client.Send(json);
        Thread.Sleep(100);

      }
      catch (Exception ex)
      {
        Log.Error("Failed to send data message", ex);
      }
    }

    #endregion

    #region Websocket client event handlers

    private void ClientOpened(object sender, EventArgs e)
    {
      Log.Info("Websocket connection opening");
      RegisterAllDevices();
    }

    private void MessageReceived(object sender, MessageReceivedEventArgs e)
    {
      Log.Info("Websocket received: " + e.Message);

      try
      {

        dynamic action = JsonConvert.DeserializeObject(e.Message);

        if (action.type != "action") return;

        var devevt = _events.FirstOrDefault(d => d.Id == action.ddid.Value);

        if (devevt == null) return;
        //Translate artik event data into HBus Server event
        var evt = new Event()
        {
          Name = "device-" + action.data.actions[0].name,
          Address = devevt.Address,
          Source = devevt.Source,
          MessageType = "event",
          Channel = devevt.Channel,
          Data = devevt.Data
        };
        Send(evt, this);
      }
      catch (Exception ex)
      {
        Log.Error("Received artik message error", ex);
      }
    }

    private void ClientClosed(object sender, EventArgs e)
    {
      Log.Info("Websocket connection closing");
    }
    #endregion
  }
}