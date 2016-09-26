using System;

namespace HBus.Utilities
{
  /// <summary>
  ///   BigEndian conversion from/to various data types
  /// </summary>
  public static class BigEndianConverter
  {
    /// <summary>
    ///   Convert byte array to Int16
    /// </summary>
    /// <param name="data">byte array in BigEndian order</param>
    /// <param name="index">start of array</param>
    /// <returns>Converted Int16</returns>
    public static short ToInt16(byte[] data, int index = 0)
    {
      if (data == null)
        throw new ArgumentNullException("data");
      if (data.Length < index + 2)
        throw new ArgumentOutOfRangeException("index");

      return (short) ((data[index] << 8) | data[index + 1]);
    }

    public static ushort ToUInt16(byte[] data, int index = 0)
    {
      if (data == null)
        throw new ArgumentNullException("data");
      if (data.Length < index + 2)
        throw new ArgumentOutOfRangeException("index");

      return (ushort) ((data[index] << 8) | data[index + 1]);
    }

    public static int ToInt32(byte[] data, int index = 0)
    {
      if (data == null)
        throw new ArgumentNullException("data");
      if (data.Length < index + 4)
        throw new ArgumentOutOfRangeException("index");

      return (data[index] << 24) | (data[index + 1] << 16) | (data[index + 2] << 8) | data[index + 3];
    }

    public static uint ToUInt32(byte[] data, int index = 0)
    {
      if (data == null)
        throw new ArgumentNullException("data");
      if (data.Length < index + 4)
        throw new ArgumentOutOfRangeException("index");

      return (uint) ((data[index] << 24) | (data[index + 1] << 16) | (data[index + 2] << 8) | data[index + 3]);
    }

    public static float ToSingle(byte[] data, int index = 0)
    {
      var array = new byte[sizeof(float)];
      const int size = sizeof(float);

      for (var i = 0; i < size; i++)
        array[size - i - 1] = data[index + i];

      return BitConverter.ToSingle(array, 0);
    }

    public static byte[] GetBytes(short value)
    {
      var udata = BitConverter.GetBytes(value);
      var data = new byte[2];
      data[0] = udata[1];
      data[1] = udata[0];
      return data;
    }

    public static byte[] GetBytes(ushort value)
    {
      var udata = BitConverter.GetBytes(value);
      var data = new byte[2];
      data[0] = udata[1];
      data[1] = udata[0];
      return data;
    }

    public static byte[] GetBytes(int value)
    {
      var udata = BitConverter.GetBytes(value);
      var data = new byte[4];
      data[0] = udata[3];
      data[1] = udata[2];
      data[2] = udata[1];
      data[3] = udata[0];
      return data;
    }

    public static byte[] GetBytes(uint value)
    {
      var udata = BitConverter.GetBytes(value);
      var data = new byte[4];
      data[0] = udata[3];
      data[1] = udata[2];
      data[2] = udata[1];
      data[3] = udata[0];
      return data;
    }

    public static byte[] GetBytes(float value)
    {
      var udata = BitConverter.GetBytes(value);

      if (!BitConverter.IsLittleEndian)
        return udata;

      var array = new byte[udata.Length];
      var n = udata.Length;

      foreach (var t in udata)
        array[--n] = t;

      return array;
    }
  }
}