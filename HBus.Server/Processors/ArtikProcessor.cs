using System;
using System.Collections.Generic;
using System.Globalization;
using HBus.Server.Data;
using Newtonsoft.Json;
using System.Threading;
using System.Linq;
using WebSocket4Net;

namespace HBus.Server.Processors
{
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

  public class ArtikProcessor : BaseProcessor
  {
    private const string WebSocketUrl = "wss://api.artik.cloud/v1.1/websocket?ack=true";
    //private const string BaseUrl = "https://api.artik.cloud/v1.1";


    private WebSocket _client;
    //private WebSocketServer _ws;
    private readonly IList<ArtikDevice> _devices;
    //private readonly IList<IWebSocketConnection> _clients;

    public ArtikProcessor(IList<ArtikDevice> devices)
    {
      _devices = devices;
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
        //Event from ep source
        OnSourceEvent = SendDataToArtik;

        _client.Open();

        //_ws.Start(socket =>
        //{
        //    socket.OnOpen = () => OnOpen(socket);
        //    socket.OnClose = () => OnClose(socket);
        //    socket.OnMessage = message => OnWsMessage(socket, message);
        //    socket.OnError = (exception) => OnError(exception);
        //});
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

    public void RegisterAllDevices()
    {
      Log.Info("Registering artik devices on the websocket connection");
      try
      {
        foreach (var device in _devices)
        {

          var register = new
          {
            type = "register",
            sdid = device.Id,
            Authorization = "bearer " + device.Token,
            cid = TotalMilliseconds()
          };

          var json = JsonConvert.SerializeObject(register, Formatting.Indented);

          Log.Info("Sending register message " + json);

          _client.Send(json);

          //foreach (var client in _clients)
          //{
          //    client.Send(json);
          //}
          Thread.Sleep(10);
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
        var device = _devices.FirstOrDefault(d => d.Source == @event.Source);
        if (string.IsNullOrEmpty(device.Id))
        {
          Log.Warn(string.Format("Device {0} not found", @event.Name));
          return;
        }

        var val = @event.Value.ToString("0.00", CultureInfo.InvariantCulture);

        switch (@event.Name)
        {
          case "sensor-read":
            read = string.Format("\"timestamp\" : {0}, \"value\" : {1}", 0, val);
            break;
          case "pin-change":
            read = string.Format("\"timestamp\" : {0}, \"value\" : {1}, \"status\" : \"{2}\"", @event.Timestamp.Ticks,
              @event.Value, @event.Status);
            break;
          case "device-event":
            read = string.Format("\"timestamp\" : {0}, \"status\" : \"{1}\"", @event.Timestamp.Ticks, @event.Status);
            break;
          default:
            return;
        }

        var ts = TotalMilliseconds();

        var json = "{ " +
                   "\n\t\"sdid\": \"" + device.Id + "\"," +
                   "\n\t\"ts\": " + ts + "," +
                   "\n\t\"data\": {" + read + "}," +
                   "\n\t\"cid\": \"" + ts + "\"" +
                   "\n}";

        Log.Info("Sending artik data to the cloud: " + json);
        _client.Send(json);
        //foreach (var client in _clients)
        //        {
        //            client.Send(json);
        //        }
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

        var dev = _devices.FirstOrDefault(d => d.Id == action.ddid.Value);
        //TODO: var actionData = action.data.actions[0].parameters to byte array
        if (dev.Id != null)
        {
          var evt = new Event()
          {
            Name = "device-" + action.data.actions[0].name,
            Address = dev.Address,
            Source = dev.Source,
            MessageType = "event",
            Channel = dev.Channel,
            Data = null
          };
          Send(evt, this);
        }
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