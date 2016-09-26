using System;
using System.Net;
using System.Net.Sockets;

namespace HBus.Ports
{
  /// <summary>
  ///   udp port implementation
  /// </summary>
  public class PortUdp : Port
  {
    private readonly string _ip;

    private readonly int _port;
    private bool _listen;
    private UdpState _state;

    public PortUdp(int port, string ipAddress, int portnumber, bool asyncmessages = false)
      : base(portnumber, asyncmessages)
    {
      _port = port;
      _ip = ipAddress;

      IsMulticast = true;
      IsFullDuplex = true;
      HasRoutes = true;

      Log.Debug(string.Format("PortUdp({0}) done.", port));
    }

    #region Implementation of IDisposable

    public override void Dispose()
    {
      Stop();
      base.Dispose();
    }

    #endregion

    /// <summary>
    ///   Start listening on udp port
    /// </summary>
    public override void Start()
    {
      Log.Debug("PortUdp.start");

      try
      {
        Log.Debug("Start waiting for a connection...");
        _listen = true;


        var ip = GetLocalIp();
        var ep = new IPEndPoint(ip, _port);
        var u = new UdpClient();
        u.EnableBroadcast = true;
        u.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        u.Client.Bind(ep);
        _state = new UdpState();
        _state.e = ep;
        _state.u = u;
        u.BeginReceive(ReceiveCallback, _state);

        base.Start();
      }
      catch (Exception e)
      {
        Log.Error("PortUdp.Start error", e);
      }
    }

    public override void Stop()
    {
      try
      {
        if (!_listen) return;

        _listen = false;

        if (_state != null)
          _state.u.Client.Shutdown(SocketShutdown.Both);

        base.Stop();
      }
      catch (Exception e)
      {
        Log.Error("PortUdp.Stop error", e);
      }
    }

    #region Tx routines

    protected override void WritePort(byte[] buffer, int i, int length, string hwaddress)
    {
      try
      {
        if ((buffer == null) || (buffer.Length < i + length))
        {
          //Status = Status.Error;
          Log.Error("PortUdp: message size error.");

          return;
        }

        var client = new UdpClient();
        client.EnableBroadcast = true;
        client.ExclusiveAddressUse = false;

        client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
        client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontRoute, true);
        var ip = !string.IsNullOrEmpty(hwaddress) ? hwaddress : _ip;
        // Sends a message to the host to which you have connected.
        var sentBytes = client.Send(buffer, buffer.Length,
          new IPEndPoint(!string.IsNullOrEmpty(ip) ? IPAddress.Parse(ip) : IPAddress.Broadcast, _port));
        client.Close();

        Log.Debug(string.Format("PortUdp.WritePort: " + buffer.Length + " bytes sent to tcp address " + hwaddress));
      }
      catch (Exception ex)
      {
        Log.Error("PortUdp.WritePort: Something wrong while sending buffer", ex);
        throw;
      }
    }

    #endregion

    private static IPAddress GetLocalIp()
    {
      var host = Dns.GetHostEntry(Dns.GetHostName());
      foreach (var ip in host.AddressList)
        if (ip.AddressFamily == AddressFamily.InterNetwork)
          return ip;
      throw new Exception("Local IP Address Not Found!");
    }

    private class UdpState
    {
      public IPEndPoint e;
      public UdpClient u;
    }

    #region Rx routines

    private void StartListener()
    {
      if (_listen) return;

      var ep = new IPEndPoint(IPAddress.Any, _port);
      var u = new UdpClient(ep);

      _state = new UdpState();
      _state.e = ep;
      _state.u = u;
      u.BeginReceive(ReceiveCallback, _state);
    }

    private void ReceiveCallback(IAsyncResult ar)
    {
      try
      {
        var u = ((UdpState) ar.AsyncState).u;
        var e = ((UdpState) ar.AsyncState).e;

        var s = new UdpState();
        s.e = e;
        s.u = u;
        var data = u.EndReceive(ar, ref e);

        //Process received data
        if (!ProcessData(data, data.Length, e.Address.ToString()))
          Log.Error("ProcessData from UdpListener failed");

        //Start again
        if (_listen)
          u.BeginReceive(ReceiveCallback, s);
      }
      catch (Exception e)
      {
        //Status = Status.Error;
        Log.Error("PortUdp: receive error.", e);
      }
    }

    #endregion
  }
}