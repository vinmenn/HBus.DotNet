using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace HBus.Ports
{
  /// <summary>
  ///   Serial port implementation
  /// </summary>
  public class PortSerial : Port
  {
    #region private members

    private readonly SerialPort _port;

    #endregion

    // ReSharper disable InconsistentNaming
    private const int MAX_TRIES = 10;
    private const int MAX_BUFFER = 1024;
    // ReSharper restore InconsistentNaming

    #region public default Properties

    //--------------------------------------------
    // HBus default serial parameters
    //--------------------------------------------
    public const int Baudrate = 57600;
    public const Parity DataParity = Parity.None;
    public const int DataBits = 8;
    public const StopBits DataStopBits = StopBits.One;
    private readonly string _name;
    private readonly int _baud;
    private readonly Parity _parity;
    private readonly int _databits;
    private readonly StopBits _stopbits;
    private bool _done;
    private Thread _readThread;

    #endregion

    #region constructors

    /// <summary>
    ///   SerialConnector default constructor
    /// </summary>
    /// <param name="portname">serial port name</param>
    /// <param name="portnumber">Port number</param>
    /// <param name="asyncmessages">Port works asyncrously</param>
    /// <param name="baudrate">Baudrate</param>
    /// <param name="parity">Parity type</param>
    /// <param name="databits">Data bits</param>
    /// <param name="stopbits">Stop bits</param>
    public PortSerial(string portname, int portnumber, bool asyncmessages = false, int baudrate = Baudrate,
      Parity parity = DataParity, int databits = DataBits, StopBits stopbits = DataStopBits)
      : base(portnumber, asyncmessages)
    {
      _name = portname;
      _baud = baudrate;
      _parity = parity;
      _databits = databits;
      _stopbits = stopbits;
      _port = new SerialPort(portname, baudrate, parity, databits, stopbits)
      {
        ReadBufferSize = 2048
      };

      _done = false;
      IsMulticast = true;
      IsFullDuplex = true;
      HasRoutes = false;

      Log.Debug("PortSerial(...) done.");
    }

    /// <summary>
    ///   SerialConnector default constructor
    /// </summary>
    /// <param name="portname">serial port name</param>
    /// <param name="portnumber">Port number</param>
    public PortSerial(string portname, int portnumber)
      : this(portname, portnumber, false, Baudrate, DataParity, DataBits, DataStopBits)
    {
      //
    }

    public override void Start()
    {
      if ((_port != null) && !_port.IsOpen)
        _port.Open();

      //Separate read thread working on MONO
      _done = false;
      _readThread = new Thread(ReadThread);

      _readThread.Start();
      Task.Delay(200);

      base.Start();
    }

    public override void Stop()
    {
      if ((_port != null) && _port.IsOpen)
        _port.Close();

      _done = true;
      _readThread.Abort();

      base.Stop();
    }

    public override void Dispose()
    {
      Stop();

      if (_port != null)
        _port.Dispose();

      base.Dispose();
    }

    #endregion

    #region specific functions

    protected override void WritePort(byte[] buffer, int i, int length, string hwAddress)
    {
      try
      {
        _port.Write(buffer, i, length);

        while (_port.BytesToWrite > 0)
          Thread.Sleep(1);

        Log.Debug(string.Format("Written {0} bytes on serial port {1}", length, _name));
      }
      catch (Exception ex)
      {
        FlushPort(_port);
        Log.Error("ReadThread unexpected error", ex);
      }
    }

    private void ReadThread()
    {
      try
      {
        while (!_done)
        {
          Thread.Sleep(200);
          if (_port.IsOpen)
            if (_port.BytesToRead >= HBusSettings.MessageLength)
            {
              //Read all bytes
              var buffer = new byte[_port.BytesToRead];

              //Status = Status.ReceiveMessage;
              _port.Read(buffer, 0, buffer.Length);
              //Status = Status.Ready;

              //Process received data
              if (!ProcessData(buffer))
                FlushPort(_port);
            }
        }
      }
      catch (Exception ex)
      {
        FlushPort(_port);
        Log.Error("ReadThread unexpected error", ex);
      }
    }

    /// <summary>
    ///   Received data from main serial port
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void SerialDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
      var sp = (SerialPort) sender;
      try
      {
        //Check received bytes
        var tries = 0;
        while ((sp.BytesToRead < HBusSettings.MessageLength) && (tries < 10))
        {
          Thread.Sleep(100);
          tries++;
        }
        if (tries == MAX_TRIES)
        {
          Log.Warn("DataReceived: failed to receive data, probable malformed message");

          //Too many tries => exit
          FlushPort(sp);
          return;
        }

        if (sp.BytesToRead > MAX_BUFFER)
        {
          //Buffer overflow
          Log.Warn("DataReceived: too many bytes received : " + sp.BytesToRead);
          FlushPort(sp);
          return;
        }
        //Wait the rest of message
        Thread.Sleep(100);

        //Read all bytes
        var buffer = new byte[sp.BytesToRead];

        //Status = Status.ReceiveMessage;
        sp.Read(buffer, 0, buffer.Length);
        //Status = Status.Ready;

        //Process received data
        if (!ProcessData(buffer))
          FlushPort(sp);
      }
      catch (Exception ex)
      {
        FlushPort(sp);
        //Status = Status.Ready;
        //LastError = HBusErrors.ERR_UNKNOWN;
        Log.Error("DataReceived unexpected error", ex);
      }
    }

    /// <summary>
    ///   Flush out received data
    /// </summary>
    /// <param name="port"></param>
    private static void FlushPort(SerialPort port)
    {
      if ((port == null) || !port.IsOpen) return;
      port.DiscardInBuffer();
      port.DiscardOutBuffer();
    }

    private void SerialPinChanged(object sender, SerialPinChangedEventArgs e)
    {
      Log.Debug(string.Format("bytes available {0}", _port.BytesToRead));
    }

    #endregion
  }
}