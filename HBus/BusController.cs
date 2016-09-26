using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using HBus.Ports;
using log4net;

namespace HBus
{
  /// <summary>
  ///   Bus controller states
  /// </summary>
  public enum BusStatus
  {
    Reset = 0x00,
    Ready,
    Send,
    WaitAck
  }

  /// <summary>
  ///   Delegate that receives HBus messages
  /// </summary>
  /// <param name="source"></param>
  /// <param name="message"></param>
  public delegate void OnMessageHandler(object source, Message message);

  public delegate bool OnCommandHandler(object source, Message message, int port);

  /// <summary>
  ///   HBus controller
  ///   Manage communications
  ///   through different nodes
  /// </summary>
  public class BusController
  {
    #region private members

    private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

    private enum ProcessActionTypes
    {
      DoNothing,
      SendAck,
      SendNack,
      ForwardMessage,
      ForwardMessageWithPayload
    }

    //Communcation ports
    private readonly Port[] _ports;

    private static readonly Mutex RxMutex = new Mutex();

    #endregion

    #region constructors

    /// <summary>
    ///   BusController constructor
    /// </summary>
    /// <param name="address">Node address</param>
    /// <param name="ports">HBus communication ports</param>
    public BusController(Address address, Port[] ports)
    {
      Status = BusStatus.Reset;
      Address = address;
      _ports = ports;

      foreach (var port in _ports)
        port.MessageReceived += MessageReceived;

      Status = BusStatus.Ready;
    }

    /// <summary>
    ///   BusController 1 port contructor
    /// </summary>
    /// <param name="address">Node address</param>
    /// <param name="port">HBus communication single port</param>
    public BusController(Address address, Port port)
      : this(address, new[] {port})
    {
    }

    #endregion

    #region Bus events

    /// <summary>
    ///   Sent command through port
    /// </summary>
    /// object sender, Message message, int port
    public event Action<object, Message> CommandSent;

    /// <summary>
    ///   Sent ack/nack through port
    /// </summary>
    /// object sender, Message message, int port
    public event Action<object, Message> AckSent;

    /// <summary>
    ///   Received command from port
    /// </summary>
    /// object sender, Message message, int port
    public event Func<object, Message, int, bool> CommandReceived;

    /// <summary>
    ///   Received ack from port
    /// </summary>
    /// object sender, Message message, int port
    /// returns message process result
    // Func<object, Message, int, bool> 
    public event OnCommandHandler AckReceived;

    /// <summary>
    ///   Event delegate for multiple handlers
    /// </summary>
    public event OnMessageHandler OnMessageReceived;

    /// <summary>
    ///   Event delegate for multiple handlers
    /// </summary>
    public event OnMessageHandler OnMessageTransmited;

    #endregion

    #region public properties

    /// <summary>
    ///   Bus controller source address
    /// </summary>
    public Address Address { get; protected set; }

    /// <summary>
    ///   Address width
    /// </summary>
    public AddressWidth AddressWidth { get; set; }

    /// <summary>
    ///   Available ports
    /// </summary>
    public int Ports
    {
      get { return _ports.Length; }
    }

    /// <summary>
    ///   Flag ignore messages from my addrress
    /// </summary>
    public bool IgnoreOwnMessages { get; set; }

    /// <summary>
    ///   Message payload
    /// </summary>
    public byte[] Payload { get; set; }

    /// <summary>
    ///   Bus controller status
    /// </summary>
    public BusStatus Status { get; private set; }

    /// <summary>
    ///   Milliseconds delay after command is sent
    /// </summary>
    public int CommandDelay { get; set; }

    #endregion

    #region error functions

    /// <summary>
    ///   Last error occurred
    /// </summary>
    public byte LastError { get; private set; }

    /// <summary>
    ///   Total errors occured from reset
    /// </summary>
    public uint TotalErrors { get; private set; }

    /// <summary>
    ///   Set error message and excpetion
    /// </summary>
    /// <param name="source"></param>
    /// <param name="exception"></param>
    public void SetError(string source, Exception exception = null)
    {
      LastError = HBusErrors.ERR_UNKNOWN;
      TotalErrors++;
      Log.Error(string.Format("Unknown error in {0}", source), exception);
    }

    /// <summary>
    ///   Reset BusController last error
    /// </summary>
    public void ResetError()
    {
      LastError = 0;
      Log.Debug("Last error reset");
    }

    /// <summary>
    ///   Reset all errors
    /// </summary>
    public void ClearErrors()
    {
      LastError = 0;
      TotalErrors = 0;
      Log.Debug("errors cleared");
    }

    #endregion

    #region public functions

    /// <summary>
    ///   Send command
    /// </summary>
    /// <param name="command">Command code</param>
    /// <param name="data">Data payload</param>
    public void SendCommand(byte command, byte[] data = null)
    {
      SendMessage(new Message(MessageTypes.Normal, Address.Empty, Address, command, data));
    }

    /// <summary>
    ///   Send normal command with destination
    /// </summary>
    /// <param name="command">Command code</param>
    /// <param name="destination">Address destination</param>
    /// <param name="data">Data payload</param>
    public void SendCommand(byte command, Address destination, byte[] data = null)
    {
      SendMessage(new Message(MessageTypes.Normal, destination, Address, command, data));
    }

    /// <summary>
    ///   Send immediate command
    ///   without address
    /// </summary>
    /// <remarks>Is used fot point to point messages</remarks>
    /// <param name="command">Command code</param>
    /// <param name="data">Data payload</param>
    public void SendImmediate(byte command, byte[] data = null)
    {
      SendMessage(new Message(MessageTypes.Immediate, Address.Empty, Address, command, data));
    }

    /// <summary>
    ///   Send immediate command
    /// </summary>
    /// <param name="command">Command code</param>
    /// <param name="destination"></param>
    /// <param name="data">Data payload</param>
    public void SendImmediate(byte command, Address destination, byte[] data = null)
    {
      SendMessage(new Message(MessageTypes.Immediate, destination, Address, command, data));
    }

    /// <summary>
    ///   Send ACK response to destination
    /// </summary>
    /// <param name="command">Command sent from node</param>
    /// <param name="destination">Address destination</param>
    /// <param name="data">data payload</param>
    public void SendAck(byte command, Address destination, byte[] data)
    {
      SendMessage(new Message(MessageTypes.AckResponse, destination, Address, command, data));
    }

    /// <summary>
    ///   Send NACK response to destination
    /// </summary>
    /// <param name="command">Command sent from node</param>
    /// <param name="destination">Address destination</param>
    /// <param name="errorcode">Error code</param>
    public void SendNack(byte command, Address destination, byte errorcode)
    {
      SendMessage(new Message(MessageTypes.NackResponse, destination, Address, command, new[] {errorcode}));
    }

    /// <summary>
    ///   Open ports to send/receive messages
    /// </summary>
    public void Open()
    {
      foreach (var port in _ports)
        if (port != null)
          port.Start();
      Status = BusStatus.Ready;
    }

    /// <summary>
    ///   Close all ports
    /// </summary>
    public void Close()
    {
      foreach (var port in _ports)
        if (port != null)
        {
          port.Stop();
          port.Dispose();
        }
      Status = BusStatus.Reset;
    }

    #endregion

    #region message handling functions

    /// <summary>
    ///   Send HBus message to specific port
    ///   or broadcast to all available ports
    /// </summary>
    /// <param name="msg"></param>
    /// <param name="sourceport">Skip this port if value is passed</param>
    private void SendMessage(Message msg, int sourceport = -1)
    {
      if (Status == BusStatus.Reset) // || Status == BusStatus.Send)
      {
        Log.Warn(string.Format("SendMessage not possibile with status {0}", Status));
        return;
      }
      try
      {
        Status = BusStatus.Send;

        var buffer = msg.ToArray();

        if ((buffer == null) || (buffer.Length < HBusSettings.MessageLength))
        {
          LastError = HBusErrors.ERR_MESSAGE_CORRUPTED;
          Log.Error("SendMessage: wrong message size");
          return;
        }

        //Send message to all available ports
        foreach (var port in _ports.Where(p => p.Number != sourceport))
        {
          port.SendMessage(buffer);
          Log.Debug(string.Format("Message {0} sent on port {1}", msg, port.Number));
        }

        //Throw specific events
        if ((CommandSent != null) &&
            ((msg.MessageType == MessageTypes.Immediate) || (msg.MessageType == MessageTypes.Normal)))
          CommandSent(this, msg);

        if ((AckSent != null) &&
            ((msg.MessageType == MessageTypes.AckResponse) || (msg.MessageType == MessageTypes.NackResponse)))
          AckSent(this, msg);

        Thread.Sleep(CommandDelay);

        Status = msg.MessageType == MessageTypes.Normal ? BusStatus.WaitAck : BusStatus.Ready;

        //Throw message transmitted event
        if (OnMessageTransmited != null)
          OnMessageTransmited(this, msg);
      }
      catch (Exception ex)
      {
        SetError(MethodBase.GetCurrentMethod().Name, ex);
      }
    }

    /// <summary>
    ///   Process message according to following rules:
    ///   1) message for other nodes => retransmit to other port
    ///   2) message for this node => process message
    ///   3) message in broadcast => process message and if result is false retransmit
    /// </summary>
    /// <param name="sender">Source of message (Port)</param>
    /// <param name="eventArgs">Message received + port number</param>
    private void MessageReceived(object sender, MessageEventArgs eventArgs)
    {
      ResetError();

      try
      {
        RxMutex.WaitOne();

        var message = eventArgs.Message;
        var port = eventArgs.Port;

        //Throw message received event
        if (OnMessageReceived != null)
          OnMessageReceived(this, message);

        //Use address ?
        var noAddress = (message.Flags & 0x0c) == 0;

        //Message in broadcast
        var broadcast = message.Destination.Equals(Address.BroadcastAddress);
        //Message is for this node
        var forMe = message.Destination.Equals(Address) || noAddress;
        var fromMe = message.Source.Equals(Address);
        //Message no requires ack/nack
        var immediate = message.MessageType == MessageTypes.Immediate;
        //Message is ack/nack response
        var isAck = (message.MessageType == MessageTypes.AckResponse) ||
                    (message.MessageType == MessageTypes.NackResponse);
        //Get port
        var conn = _ports[port];
        //Command from expected source => OK
        Log.Debug(string.Format("{0} received from node: {1}", message, message.Source));
        //return true if message is processed from this node
        var processed = false;

        //Round check (abort message from myself)
        if (fromMe & IgnoreOwnMessages)
        {
          Log.Warn(string.Format("Message {0} originated from this node", message));
          return;
        }

        //Check message for me or in broadcast
        if (forMe || broadcast)
          if (isAck)
          {
            if (forMe)
              if (conn.CheckAck(message))
              {
                lock (message)
                {
                  if (AckReceived != null)
                    processed = AckReceived(this, message, port);
                  //Thread.Sleep(100);
                }
              }
              else
              {
                LastError = HBusErrors.ERR_ACK_LOST;
                //Check ack failed
                Log.Error(string.Format("Check ack from node {0} failed", message.Source));
              }
          }
          else
          {
            //Process command on Node
            processed = (CommandReceived != null) && CommandReceived(this, message, port);
          }
        //Default response action send ack if not broadcast
        var result = ProcessActionTypes.DoNothing;

        if (forMe && !broadcast && !immediate && !isAck)
        {
          //Processed without errors => ACK
          result = processed && (LastError == 0) ? ProcessActionTypes.SendAck : ProcessActionTypes.SendNack;
        }
        else
        {
          if (!fromMe && ((!forMe && !broadcast) || (!processed && broadcast)) &&
              ((message.Destination != Address.Empty) || noAddress))
            //if (broadcast && !processed)
            result = Ports > 1 ? ProcessActionTypes.ForwardMessage : ProcessActionTypes.DoNothing;
        }

        switch (result)
        {
          case ProcessActionTypes.DoNothing:
            break;
          case ProcessActionTypes.SendAck:
            //Send back ack to same port
            SendAck(message.Command, message.Source, Payload);
            break;
          case ProcessActionTypes.SendNack:
            //Send back nack to same port
            SendNack(message.Command, message.Source, LastError);
            break;
          case ProcessActionTypes.ForwardMessage:
            SendMessage(message, port);
            break;
          case ProcessActionTypes.ForwardMessageWithPayload:
            //If new payload recreate the message
            if (Payload != null)
              message = new Message(message.MessageType, message.Destination, message.Source,
                message.Command, Payload);
            //Forward message to other ports
            SendMessage(message, port);
            break;
        }
        Log.Debug(string.Format("message {0} processed", message));
      }
      catch (Exception ex)
      {
        SetError(MethodBase.GetCurrentMethod().Name, ex);
      }
      finally
      {
        RxMutex.ReleaseMutex();
        Status = BusStatus.Ready;
      }
    }

    #endregion
  }
}