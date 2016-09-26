using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace HBus.Ports
{
  /// <summary>
  ///   Tcp implementation
  ///   Parameters:
  ///   hostip: host ip node (for hub configurations)
  ///   listenport: ip port that receives HBus messages
  ///   sendport: ip port that transmits HBus messages
  ///   portnumber: HBus port index
  ///   asyncmessages: transmit asyncrously
  /// </summary>
  public class PortTcp : Port
  {
    private readonly string _hostIp;
    private readonly int _tcpListenPort;
    private readonly int _tcpSendPort;
    private string _lastIp;
    private bool _listen;
    private NetworkStream _stream;
    private TcpListener _tcpListener;

    public PortTcp(string hostip, int listenport, int sendport, int portnumber, bool asyncmessages = false)
      : base(portnumber, asyncmessages)
    {
      //Store host address
      _hostIp = hostip;
      _tcpListenPort = listenport;
      _tcpSendPort = sendport;
      IsMulticast = true;
      IsFullDuplex = true;
      HasRoutes = true;

      Log.Debug("PortTcp(...) done.");
    }

    #region Implementation of IDisposable

    public override void Dispose()
    {
      Stop();
      base.Dispose();
    }

    #endregion

    /// <summary>
    ///   Start listening on tcp port
    /// </summary>
    public override void Start()
    {
      Log.Debug("PortTcp.start");

      try
      {
        // Set the listener on the local IP address 
        // and specify the port.
        _tcpListener = new TcpListener(IPAddress.Any, _tcpListenPort);

        _listen = true;
        _tcpListener.Start();
        Log.Debug("Start waiting for a connection...");

        var thread = new Thread(CreateListener);
        thread.Start();

        base.Start();
      }
      catch (Exception e)
      {
        Log.Error("PortTcp.CreateListener error", e);
      }
    }

    public override void Stop()
    {
      _listen = false;

      if (_tcpListener != null)
        _tcpListener.Stop();

      base.Stop();
    }

    #region Rx routines

    /// <summary>
    ///   Create rx listener for tcp connections
    /// </summary>
    private void CreateListener()
    {
      while (_listen)
      {
        // Always use a Sleep call in a while(true) loop 
        // to avoid locking up your CPU.
        Thread.Sleep(10);
        try
        {
          // Create a TCP socket. 
          // If you ran this server on the desktop, you could use 
          // Socket socket = tcpListener.AcceptSocket() 
          // for greater flexibility.
          using (var tcpClient = _tcpListener.AcceptTcpClient())
          {
            // Read the data stream from the client. 
            var data = new byte[tcpClient.ReceiveBufferSize];

            _stream = tcpClient.GetStream();
            var bytesRead = _stream.Read(data, 0, data.Length);

            if (bytesRead > 0)
            {
              var ip = (IPEndPoint) tcpClient.Client.RemoteEndPoint;

              var ipvalue = ip.ToString();

              ipvalue = ipvalue.IndexOf(':') > 0
                ? ipvalue.Substring(0, ipvalue.IndexOf(':'))
                : ipvalue;

              _lastIp = ipvalue;

              //Process received data
              if (!ProcessData(data, bytesRead, ipvalue))
                Log.Error("ProcessData from TcpPort failed");
            }

            //Close client
            tcpClient.Close();
          }
        }
        catch (Exception ex)
        {
          Log.Error("CreateListener: unexpected socket end", ex);
        }
      }
      Log.Debug("CreateListener stop");
    }

    #endregion

    #region Tx routines

    protected override void WritePort(byte[] buffer, int i, int length, string hwaddress)
    {
      try
      {
        if ((buffer == null) || (buffer.Length < i + length))
        {
          //Status = Status.Error;
          Log.Error("PortTcp: message size error.");

          return;
        }

        if ((_stream != null) && _stream.CanWrite && ((hwaddress == _lastIp) || string.IsNullOrEmpty(hwaddress)))
        {
          _stream.Write(buffer, i, length);
          _stream.Flush();
          _stream.Close();
          _stream = null;
          _lastIp = string.Empty;
        }
        else
        {
          var ip = !string.IsNullOrEmpty(hwaddress) ? hwaddress : _hostIp;
          WriteData(ip, buffer);

          Log.Debug(string.Format("PortTcp.WritePort: " + buffer.Length + " bytes sent to tcp address " + ip));
        }
      }
      catch (Exception ex)
      {
        Log.Error("PortTcp.WritePort: Something wrong while sending buffer", ex);
        throw;
      }
    }

    private void WriteData(string ipAddress, byte[] buffer)
    {
      Socket socket = null;
      try
      {
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.SendTimeout = 5000;
        socket.Connect(ipAddress, _tcpSendPort);
        socket.Send(buffer);

        Thread.Sleep(500);
        if (socket.Available > 0)
        {
          var buf = new byte[socket.Available];
          socket.Receive(buf);
          ProcessData(buf, buf.Length, ipAddress);
        }

        socket.Close();
      }
      catch (Exception ex)
      {
        if (socket != null)
          socket.Close();

        Log.Error("PortTcp.SendData: Unexpected error", ex);
      }
    }

    #endregion
  }
}