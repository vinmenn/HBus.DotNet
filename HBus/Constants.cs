namespace HBus
{
  /// <summary>
  ///   HBus default global settings
  /// </summary>
  public static class HBusSettings
  {
    //--------------------------------------------
    //HBus versions
    //--------------------------------------------
    public static string HBusVersion = "2.4";
    public static string ProtocolVersion = "2.0"; //Default protocol version

    static HBusSettings()
    {
      NameLength = 5;
      NameEmpty = "     ";
      CommandTimeout = 3;
#if ADDRESS_WIDTH_0
            AddressWidth = AddressWidth.NoAddress;
#endif
#if ADDRESS_WIDTH_1
      AddressWidth = AddressWidth.OneByte;
#endif
#if ADDRESS_WIDTH_2
            AddressWidth = AddressWidth.TwoBytes;
#endif
#if ADDRESS_WIDTH_4
            AddressWidth = AddressWidth.FourBytes;
#endif
      MessageLength = 5; //START + FLAGS + COMMAND + CRC x 2
    }

    //--------------------------------------------
    //Fixed strings settings (used for names)
    //--------------------------------------------
    public static int NameLength { get; set; }
    public static string NameEmpty { get; set; }

    //--------------------------------------------
    //Messages settings
    //--------------------------------------------
    public static int MessageLength { get; set; }
    public static int CommandTimeout { get; set; }
    public static AddressWidth AddressWidth { get; set; }
  }

  /// <summary>
  ///   Default HBus error codes
  /// </summary>
  public static class HBusErrors
  {
    //--------------------------------------------
    //Errors
    //--------------------------------------------
    public const byte ERR_UNKNOWN = 0xFF; // Error unknown
    public const byte ERR_BUS_BUSY = 0xF0; // Bus is sending/receiving
    public const byte ERR_MESSAGE_CORRUPTED = 0xF1; // Message corrupted
    public const byte ERR_RX_OVERSIZE = 0xF2; // Rx buffer overrun
    public const byte ERR_RX_TIMEOUT = 0xF3; // Rx timeout
    public const byte ERR_BAD_CRC = 0xF4; // crc error
    public const byte ERR_ROUND_CHECK = 0xF5; // Message originated from this node returned
    public const byte ERR_ACK_LOST = 0xF6; // Ack received from different source
    public const byte ERR_MESSAGE_UNKNOWN = 0xF7; // Message unknown /unsupported
  }
}