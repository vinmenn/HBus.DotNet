using System;
using HBus.Utilities;

namespace HBus
{
  /// <summary>
  ///   Message type (part of flags byte)
  /// </summary>
  public enum MessageTypes
  {
    Normal = 0,
    Immediate = 1,
    AckResponse = 2,
    NackResponse = 3
  }

  /// <summary>
  ///   Message payload values
  /// </summary>
  public enum Payload
  {
    // ReSharper disable InconsistentNaming
    Bytes_0 = 0x00,
    Bytes_1 = 0x10,
    Bytes_2 = 0x20,
    Bytes_3 = 0x30,
    Bytes_4 = 0x40,
    Bytes_5 = 0x50,
    Bytes_6 = 0x60,
    Bytes_7 = 0x70,
    Bytes_8 = 0x80,
    Bytes_16 = 0x90,
    Bytes_32 = 0xA0,
    Bytes_64 = 0xB0,
    Bytes_128 = 0xC0,
    Bytes_256 = 0xD0,
    Bytes_512 = 0xE0,
    UserDefined = 0xF0
  }

  /// <summary>
  ///   External received message handler
  /// </summary>
  /// <param name="sender">Sender of message</param>
  /// <param name="args">Message arguments</param>
  public delegate void MessageHandler(object sender, MessageEventArgs args);

  /// <summary>
  ///   Message arguments for event handler
  /// </summary>
  public class MessageEventArgs : EventArgs
  {
    //public string HwAddress { get; set; }

    public MessageEventArgs(Message message, int port)
    {
      Message = message;
      Port = port;
      //HwAddress = hwAddress;
    }

    public Message Message { get; set; }
    public int Port { get; set; }

    public override string ToString()
    {
      return string.Format("arg(message) = {0} from connector {1}", Message, Port);
    }
  }

  /// <summary>
  ///   Home bus message packet
  ///   Start = START_BYTE
  ///   Flags = 7 6 5 4 3 2 1 0
  ///   7 = PAYLOAD_3
  ///   6 = PAYLOAD_2
  ///   5 = PAYLOAD_1
  ///   4 = PAYLOAD_0
  ///   3 = ADDRESS_WIDTH_1
  ///   2 = ADDRESS_WIDTH_0
  ///   1 = MSG_TYPE_BIT_1
  ///   0 = MSG_TYPE_BIT_0
  ///   PAYLOAD = PAYLOAD DATA LENGTH
  ///   0x00 = 0 bytes
  ///   ...
  ///   0x80 = 8 bytes
  ///   0x90 = 16 bytes
  ///   0xA0 = 32 bytes
  ///   0xB0 = 64 bytes
  ///   0xC0 = 128 bytes
  ///   0xD0 = 256 bytes
  ///   0xE0 = 512 bytes
  ///   0xF0 = user defined length (2 bytes following)
  ///   ADDRESS_WIDTH = Destination/source address width
  ///   0x00 (0000) = 0 byte (direct connection)
  ///   0x04 (0100) = 1 byte (255 addressable devices)
  ///   0x08 (1000) = 2 bytes (65535 addressable devices)
  ///   0x0C (1100) = 4 bytes (full address range (could be overlapped with other standards (ip, knx, etc).
  ///   MSG_TYPES = 00 = NORMAL MESSAGE (WITH ACK RESPONSE)
  ///   01 = IMMEDIATE MESSAGE (WITHOUT ACK RESPONSE)
  ///   02 = ACK RESPONSE
  ///   03 = NACK_RESPONSE
  ///   CRC is calculated on all bytes (excluding crc itself).
  /// </summary>
  public class Message
  {
    #region protected properties

    protected static int MinLength
    {
      get
      {
        return 5; //Start + Flags + Command + 2 * Crc
      }
    }

    protected static byte StartByte
    {
      get { return 0xAA; }
    }

    #endregion

    #region public properties

    //public const byte MessageLength = 5; //START + FLAGS + COMMAND + CRC x 2

    public byte Start { get; protected set; }
    public byte Flags { get; protected set; }
    public Address Source { get; protected set; }
    public Address Destination { get; protected set; }
    public byte Command { get; protected set; }
    public byte[] Data { get; protected set; }
    public ushort Crc { get; protected set; }

    #endregion

    #region flags

    //Flags explained
    public Payload PayloadLength
    {
      get { return (Payload) (Flags & 0xF0); }
    }

    public AddressWidth AddressWidth
    {
      get { return (AddressWidth) (Flags & 0x0C); }
    }

    public MessageTypes MessageType
    {
      get { return (MessageTypes) (Flags & 0x03); }
    }

    #endregion

    #region constructors

    public Message(MessageTypes type, Address destination, Address source, byte command, byte[] data)
    {
      Start = StartByte;

      AddressWidth width;
      if ((destination == Address.Empty) || (source == Address.Empty))
        width = AddressWidth.NoAddress;
      else
        width = Address.Width;
      //get payload
      var payload = LengthToPayload(data != null ? data.Length : 0);

      Flags = (byte) ((byte) type | (byte) width | (byte) payload);
      Destination = width != AddressWidth.NoAddress ? destination : Address.Empty;
      Source = width != AddressWidth.NoAddress ? source : Address.Empty;
      Command = command;
      Data = data;
      //Reset crc
      Crc = 0;
    }

    /// <summary>
    ///   Default constructor
    /// </summary>
    /// <param name="buffer">buffer data</param>
    /// <param name="size">Buffer size</param>
    /// <param name="offset">Buffer start index</param>
    /// <exception cref="ArgumentException">Thrown when buffer size is wrong</exception>
    public Message(byte[] buffer, ref int size, ref int offset)
    {
      var oldOffset = offset;

      if ((buffer == null) || (buffer.Length < HBusSettings.MessageLength))
        throw new ArgumentException("wrong buffer size");

      //var array = new byte[length];
      //if (length > 0 && buffer.Length > length)
      //    Array.Copy(buffer, 0, array, 0, length);
      //else
      //    array = buffer;

      Start = buffer[offset++];
      Flags = buffer[offset++];

      var payload = (Payload) (Flags & 0xF0);
      var width = (AddressWidth) (Flags & 0x0C);
      var dataLength = PayloadToLength(payload);

      //Set user Length
      if (dataLength < 0)
        dataLength = (buffer[offset++] << 8) | buffer[offset++];

      //Set destination & source
      switch (width)
      {
        case AddressWidth.NoAddress:
          Destination = Address.Empty;
          Source = Address.Empty;
          break;
        case AddressWidth.OneByte:
          Destination = new Address(buffer[offset++]);
          Source = new Address(buffer[offset++]);
          break;
        case AddressWidth.TwoBytes:
          Destination = new Address((uint) (buffer[offset++] << 8) | buffer[offset++]);
          Source = new Address((uint) (buffer[offset++] << 8) | buffer[offset++]);
          break;
        case AddressWidth.FourBytes:
          Destination =
            new Address(
              (uint) ((buffer[offset++] << 24) | (buffer[offset++] << 16) | (buffer[offset++] << 8) | buffer[offset++]));
          Source =
            new Address(
              (uint) ((buffer[offset++] << 24) | (buffer[offset++] << 16) | (buffer[offset++] << 8) | buffer[offset++]));
          break;
      }
      //Set command
      Command = buffer[offset++];
      if (dataLength > 0)
      {
        Data = new byte[dataLength];

        for (var i = 0; i < dataLength; i++)
          Data[i] = buffer[offset + i];

        offset += dataLength;
      }
      Crc = (ushort) ((buffer[offset++] << 8) | buffer[offset++]);
      //_readIndex = 0;
      size = offset - oldOffset;
    }

    #endregion

    #region methods

    /// <summary>
    ///   Convert packet to buffer array
    /// </summary>
    /// <returns></returns>
    public byte[] ToArray()
    {
      var index = 0;

      //Set length
      var length = HBusSettings.MessageLength + (Data != null ? Data.Length : 0);

      if (AddressWidth == AddressWidth.OneByte)
        length += 2;
      if (AddressWidth == AddressWidth.TwoBytes)
        length += 4;
      if (AddressWidth == AddressWidth.FourBytes)
        length += 8;


      var lengthPayload = Data != null ? LengthToPayload(Data.Length) : Payload.Bytes_0;

      if (PayloadLength != lengthPayload)
        Flags = (byte) ((byte) AddressWidth | (byte) lengthPayload | (byte) MessageType);

      if (PayloadLength == Payload.UserDefined)
        length += 2;

      var buffer = new byte[length];

      buffer[index++] = StartByte;
      buffer[index++] = Flags;

      if ((Data != null) && (lengthPayload == Payload.UserDefined))
      {
        //Add data length
        buffer[index++] = (byte) ((Data.Length >> 8) & 0xff);
        buffer[index++] = (byte) (Data.Length & 0xff);
      }

      //Add destination & source
      //Set destination & source
      switch (AddressWidth)
      {
        case AddressWidth.OneByte:
          buffer[index++] = (byte) Destination.Value;
          buffer[index++] = (byte) Source.Value;
          break;
        case AddressWidth.TwoBytes:
          buffer[index++] = (byte) (Destination.Value >> 8);
          buffer[index++] = (byte) (Destination.Value & 0xff);
          buffer[index++] = (byte) (Source.Value >> 8);
          buffer[index++] = (byte) (Source.Value & 0xff);
          break;
        case AddressWidth.FourBytes:
          buffer[index++] = (byte) (Destination.Value >> 24);
          buffer[index++] = (byte) (Destination.Value >> 16);
          buffer[index++] = (byte) (Destination.Value >> 8);
          buffer[index++] = (byte) (Destination.Value & 0xff);
          buffer[index++] = (byte) (Source.Value >> 24);
          buffer[index++] = (byte) (Source.Value >> 16);
          buffer[index++] = (byte) (Source.Value >> 8);
          buffer[index++] = (byte) (Source.Value & 0xff);
          break;
      }

      //Add command
      buffer[index++] = Command;

      //Add payload
      if (Data != null)
      {
        for (var i = 0; i < Data.Length; i++)
          buffer[index + i] = Data[i];

        index += Data.Length;
      }

      var crc = Crc != 0 ? Crc : Crc16.XModemCrc(ref buffer, 0, buffer.Length - 2);

      buffer[index++] = (byte) ((crc >> 8) & 0xFF);
      buffer[index] = (byte) (crc & 0xFF);

      //Return buffer array
      return buffer;
    }

    /// <summary>
    ///   Convert message to string
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
      return string.Format("{0}  @{1} => {2}", MessageType, Source, Destination);
    }

    public override bool Equals(object obj)
    {
      if (ReferenceEquals(null, obj)) return false;
      if (ReferenceEquals(this, obj)) return true;
      if (obj.GetType() != typeof(Message)) return false;
      return Equals((Message) obj);
    }

    public bool Equals(Message other)
    {
      if (ReferenceEquals(null, other)) return false;
      if (ReferenceEquals(this, other)) return true;
      return (other.Start == Start) &&
             (other.Flags == Flags) &&
             other.Source.Equals(Source) &&
             other.Destination.Equals(Destination) &&
             (other.Command == Command) &&
             Equals(other.Data, Data);
    }

    public override int GetHashCode()
    {
      unchecked
      {
        var result = Start.GetHashCode();
        result = (result*397) ^ Flags.GetHashCode();
        result = (result*397) ^ Source.GetHashCode();
        result = (result*397) ^ Destination.GetHashCode();
        result = (result*397) ^ Command.GetHashCode();
        result = (result*397) ^ (Data != null ? Data.GetHashCode() : 0);
        return result;
      }
    }

    public static bool operator ==(Message left, Message right)
    {
      return Equals(left, right);
    }

    public static bool operator !=(Message left, Message right)
    {
      return !Equals(left, right);
    }

    public static Message Parse(byte[] data, ref int length, ref int index)
    {
      //--------------------------------
      //Check length
      //--------------------------------
      if ((data == null) || (data.Length < MinLength))
        return null;

      //--------------------------------
      //Check start
      //--------------------------------
      if (data[index] != StartByte)
        return null;

      Message msg;
      try
      {
        msg = new Message(data, ref length, ref index);
      }
      catch (Exception)
      {
        return null;
      }

      //--------------------------------
      //return message
      //--------------------------------
      return msg;
    }

    #endregion

    #region private methods

    private int PayloadToLength(Payload payload)
    {
      //Set Length
      if (payload < Payload.Bytes_16)
        return (byte) payload >> 4;
      //Fixed length
      switch (payload)
      {
        case Payload.Bytes_16:
          return 16;
        case Payload.Bytes_32:
          return 32;
        case Payload.Bytes_64:
          return 64;
        case Payload.Bytes_128:
          return 128;
        case Payload.Bytes_256:
          return 256;
        case Payload.Bytes_512:
          return 512;
      }
      return -1;
    }

    private Payload LengthToPayload(int length)
    {
      //Set Length
      if (length < 9)
        return (Payload) (length << 4);
      //Fixed lengths
      if (length == 16)
        return Payload.Bytes_16;
      if (length == 32)
        return Payload.Bytes_32;
      if (length == 64)
        return Payload.Bytes_64;
      if (length == 128)
        return Payload.Bytes_128;
      if (length == 256)
        return Payload.Bytes_256;
      if (length == 512)
        return Payload.Bytes_512;

      return Payload.UserDefined;
    }

    #endregion
  }
}