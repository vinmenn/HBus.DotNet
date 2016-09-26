using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using log4net;

namespace HBus.Ports
{
  /// <summary>
  ///   Base HBus port
  ///   This class is abstract and should be implemented
  ///   with specific communication implementations
  ///   It's scope is to handle rx/tx message at high level
  /// </summary>
  public abstract class Port : IDisposable
  {
    #region private members

    protected static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

#if DEBUG
    private const int AckTimeout = 1000; //Seconds
#else
        private const int AckTimeout = 10; //Seconds
#endif
    private readonly IList<RouteInfo> _routes;
    private readonly bool _asyncMessages;
    private Address _waitAckFrom;
    private ushort _waitAckId;
    private DateTime _startWait;
    private Thread _thWrite;

    #endregion

    #region constructors

    /// <summary>
    ///   Base port default constructor
    /// </summary>
    protected Port(int portNumber, bool asyncMessages = false)
    {
      Number = portNumber;
      _waitAckFrom = Address.Empty;
      _waitAckId = 0;
      _asyncMessages = asyncMessages;
      _routes = new List<RouteInfo>();

      IsMulticast = false;
      IsFullDuplex = false;
      HasRoutes = false;

      //Status = Status.Reset;

      Log.Debug("HBus.Port(...) done.");
    }

    #endregion

    #region public properties & methods

    /// <summary>
    ///   Port number
    /// </summary>
    public int Number { get; protected set; }

    /// <summary>
    ///   Port is waiting for ack
    /// </summary>
    public bool WaitAck
    {
      get { return _waitAckFrom.Value != 0; }
    }

    /// <summary>
    ///   Last received message
    /// </summary>
    public Message LastMessage { get; protected set; }

    /// <summary>
    ///   Message handler
    /// </summary>
    public MessageHandler MessageReceived;

    /// <summary>
    ///   Full duplex communications
    ///   Can transmit and receive at same time
    /// </summary>
    public bool IsFullDuplex { get; protected set; }

    /// <summary>
    ///   Port can handle multicast messages
    ///   e.g. one message arrives to multiple destinations
    /// </summary>
    public bool IsMulticast { get; protected set; }

    /// <summary>
    ///   Port could determine if destination is reachable
    /// </summary>
    public bool HasRoutes { get; protected set; }

    /// <summary>
    ///   Send message (command/ack/nack) through port
    /// </summary>
    /// <param name="message"></param>
    public void SendMessage(byte[] buffer)
    {
      try
      {
        if (_asyncMessages)
        {
          //Start write thread
          _thWrite = new Thread(() => AsyncWritePort(buffer, string.Empty));
          _thWrite.Start();
        }
        else
        {
          //Write directly
          WritePort(buffer, 0, buffer.Length, string.Empty);
        }
        Log.Debug("SendMessage done.");
      }
      catch (Exception ex)
      {
        Log.Error("SendMessage unexpected error", ex);
      }
    }

    /// <summary>
    ///   Verify waited ack
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public bool CheckAck(Message message)
    {
      //Exit if no ack waited
      if (_waitAckFrom.Value == 0) return true;

      //Get ack id
      var id = (message.Data != null) && (message.Data.Length > 1)
        ? (ushort) ((message.Data[message.Data.Length - 2] << 8) | message.Data[message.Data.Length - 1])
        : 0;
      //Compare with waited ack id
      return (_waitAckFrom == message.Source) &&
             (_waitAckId == id) &&
             ((DateTime.Now - _startWait).TotalSeconds <= AckTimeout);
    }

    /// <summary>
    ///   Reset waited ack
    /// </summary>
    public void ClearAck()
    {
      _waitAckFrom = Address.Empty;
      _waitAckId = 0;
      _startWait = DateTime.MinValue;
    }

    /// <summary>
    ///   Start communictions trough port
    /// </summary>
    public virtual void Start()
    {
      //Status = Status.Ready;
      Log.Debug("Start called");
    }

    /// <summary>
    ///   Stop communictions trough port
    /// </summary>
    public virtual void Stop()
    {
      //Status = Status.Halt;
      Log.Debug("Stop called");
    }

    /// <summary>
    ///   Dispose port
    /// </summary>
    public virtual void Dispose()
    {
      if (_thWrite != null)
        if (_thWrite.IsAlive)
        {
          _thWrite.Join(500);
          if (_thWrite.IsAlive)
            _thWrite.Abort();
        }
      //Status = Status.Halt;
    }

    #endregion

    #region protected methods

    /// <summary>
    ///   Process incoming data
    /// </summary>
    /// <param name="data"></param>
    /// <param name="length"></param>
    /// <param name="hwAddress"></param>
    /// <returns></returns>
    protected bool ProcessData(byte[] data, int length = 0, string hwAddress = null)
    {
      try
      {
        var index = 0;
        //--------------------------------
        //Get length to be received according to flags value
        //--------------------------------
        do
        {
          var message = Message.Parse(data, ref length, ref index);

          if (message == null)
          {
            Log.Warn("DataReceived: failed to parse data, probable malformed message");
            return false;
          }


          //--------------------------------
          //Store route info
          //Each message received from this port
          //Is stored because is reachable
          //Hardware address is lower level address
          //And is dependant from specific implementations
          //--------------------------------
          if (hwAddress != null)
          {
            var route = _routes.FirstOrDefault(r => r.Address == message.Source);

            if ((route == null) || (route.HwAddress != hwAddress))
            {
              if (route != null)
                _routes.Remove(route);

              //new route (dynamic route update)
              //Node name is not available at message transport layer
              // but route info could be used also in application layer
              _routes.Add(new RouteInfo(message.Source, hwAddress));
            }
          }

          //--------------------------------
          //Process message at node level
          //--------------------------------
          if (MessageReceived != null)
            MessageReceived(this, new MessageEventArgs(message, Number));

          //Store message
          LastMessage = message;

          Log.Debug(string.Format("DataReceived: extracted message {0} from port {1}", LastMessage, Number));
        } while (index < length);
        return true;
      }
      catch (Exception e)
      {
        Log.Error("ProcessData error", e);
        return false;
      }
    }

    /// <summary>
    ///   Transmit data through port
    /// </summary>
    /// <remarks>This method should be implemented with specific code</remarks>
    /// <param name="buffer"></param>
    /// <param name="start"></param>
    /// <param name="length"></param>
    /// <param name="hwAddress"></param>
    protected virtual void WritePort(byte[] buffer, int start, int length, string hwAddress)
    {
      Log.Debug(string.Format("Written {0} bytes on port port {1}", length, Number));
    }

    #endregion

    #region protected methods

    /// <summary>
    ///   Async send message through port
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="hwAddress"></param>
    private void AsyncWritePort(byte[] buffer, string hwAddress)
    {
      try
      {
        WritePort(buffer, 0, buffer.Length, hwAddress);
      }
      catch (ThreadAbortException ex)
      {
        Log.Error("Error while port is being closed", ex);
      }
    }

    #endregion
  }
}