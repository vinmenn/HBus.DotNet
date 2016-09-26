using System;
using System.Threading;
using NetMQ;
using NetMQ.Sockets;

namespace HBus.Ports
{
  /// <summary>
  ///   ZeroMq port implementation
  ///   Uses ZeroMq distribuited message library to send HBus messages
  /// </summary>
  public class PortZMq : Port
  {
    private const string Queue = "hbus";
    private readonly string _endpoint;
    private readonly string _localpoint;
    private string _channel;
    private bool _listen;

    private PublisherSocket _pub;
    private ResponseSocket _req;
    private ResponseSocket _res;

    public PortZMq(string localpoint, string endpoint, int portnumber)
      : base(portnumber, false)
    {
      _localpoint = localpoint;
      _endpoint = endpoint;

      _channel = Queue;
      IsMulticast = true;
      IsFullDuplex = true;
      HasRoutes = true;

      Log.Debug(string.Format("PortZMq({0}) created.", endpoint));
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
      if (_listen) return;

      try
      {
        Log.Info("PortZMq starting");

        Log.Debug(string.Format("Publisher binding on {0}", _endpoint));


        _res = new ResponseSocket();
        _res.Bind(_localpoint);

        _listen = true;

        new Thread(() =>
        {
          while (_listen)
          {
            byte[] data;

            while ((_res != null) && _res.HasIn)
              if (_res.TryReceiveFrameBytes(TimeSpan.FromSeconds(30), out data))
              {
                Log.Debug(string.Format("Received {0} bytes", data.Length));

                //Process received data
                if (!ProcessData(data, data.Length))
                {
                  _res.SignalError();

                  Log.Error(string.Format("Processing {0} bytes failed", data.Length));
                }
                else
                {
                  _res.SignalOK();

                  Log.Debug(string.Format("Processed {0} bytes", data.Length));
                }
              }
          }
        }).Start();

        base.Start();
      }
      catch (Exception e)
      {
        Log.Error("PortZMq start error", e);
      }
    }

    public override void Stop()
    {
      try
      {
        if (!_listen) return;

        Log.Info("PortZMq stopping");

        _listen = false;

        _res.Close();

        base.Stop();
      }
      catch (Exception e)
      {
        Log.Error("PortMq stop error", e);
      }
    }

    protected override void WritePort(byte[] buffer, int i, int length, string hwaddress)
    {
      try
      {
        byte[] data;

        if (i > 0)
        {
          data = new byte[length];
          Array.Copy(buffer, 0, data, 0, length);
        }
        else
        {
          data = buffer;
        }

        var req = new RequestSocket();

        req.Connect(_endpoint);

        req.SendFrame(data);

        while (req.HasOut)
          Thread.Sleep(500);

        Log.Debug(string.Format("PortZMq.WritePort: " + length + " bytes sent to " + _endpoint));
      }
      catch (Exception ex)
      {
        Log.Error("PortZMq.WritePort: failed sending buffer", ex);
        throw;
      }
    }
  }
}