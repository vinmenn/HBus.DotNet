namespace HBus.Utilities
{
  /// <summary>
  ///   Crc 16 library
  /// </summary>
  public static class Crc16
  {
    public static ushort XModemCrc(ref byte[] data, int start, int length)
    {
      return FastCrc16(ref data, start, length, false, false, 0x1021, 0x0000, 0x0000);
    }

    //'-------------------------------------------------------
    //'Crc calculations
    //'Crc 16 parameters:
    //'Kermit: 		width=16 poly=0x1021 init=0x0000 refin=true  refout=true  xorout=0x0000 check=0x2189
    //'Modbus: 		width=16 poly=0x8005 init=0xffff refin=true  refout=true  xorout=0x0000 check=0x4b37
    //'XModem: 		width=16 poly=0x1021 init=0x0000 refin=false refout=false xorout=0x0000 check=0x31c3
    //'CCITT-False:	width=16 poly=0x1021 init=0xffff refin=false refout=false xorout=0x0000 check=0x29b1
    //'-------------------------------------------------------
    public static ushort FastCrc16(ref byte[] data, int start, int length, bool reflectIn, bool reflectOut,
      ushort polynomial, ushort xorIn, ushort xorOut)
    {
      const ushort msbMask = 0x8000;
      const ushort mask = 0xFFFF;

      var crc = xorIn;

      int i;
      int j;
      byte c;
      ushort bit;

      if (length == 0) return crc;

      for (i = start; i < start + length; i++)
      {
        c = data[i];

        if (reflectIn)
          c = Reflect(c);

        j = 0x80;

        while (j > 0)
        {
          bit = (ushort) (crc & msbMask);
          crc <<= 1;

          if ((c & j) != 0)
            bit = (ushort) (bit ^ msbMask);

          if (bit != 0)
            crc ^= polynomial;

          j >>= 1;
        }
      }

      if (reflectOut)
        crc = (ushort) ((Reflect(crc) ^ xorOut) & mask);

      return crc;
    }

    #region reflect routines

    private static byte Reflect(byte data)
    {
      return (byte) Reflect(data, 8);
    }

    private static ushort Reflect(ushort data)
    {
      return (ushort) Reflect(data, 16);
    }

    private static uint Reflect(uint data, byte bits = 32)
    {
      uint reflection = 0x00000000;
      // Reflect the data about the center bit.
      for (byte bit = 0; bit < bits; bit++)
      {
        // If the LSB bit is set, set the reflection of it.
        if ((data & 0x01) != 0)
          reflection |= (uint) (1 << (bits - 1 - bit));

        data = (byte) (data >> 1);
      }

      return reflection;
    }

    #endregion
  }
}