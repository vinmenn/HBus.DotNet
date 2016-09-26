using System;
using System.Collections.Generic;
using System.Text;

namespace HBus.Utilities
{
  /// <summary>
  ///   FIFO stack utility class
  /// </summary>
  public class SimpleStack
  {
    private readonly bool _littleEndian;
    private List<byte> _list;

    public SimpleStack()
    {
      _littleEndian = false;
      Data = null;
    }

    public SimpleStack(byte[] data, int startIndex = 0)
    {
      _littleEndian = false;
      Data = data;
      ReadIndex = startIndex;
    }

    public SimpleStack(bool isLittleEndian = false, byte[] data = null, int startIndex = 0)
    {
      _littleEndian = isLittleEndian;
      Data = data;
      ReadIndex = startIndex;
    }

    public byte[] Data
    {
      get { return _list != null ? _list.ToArray() : null; }
      set
      {
        _list = new List<byte>();
        if (value != null)
          foreach (var b in value)
            _list.Add(b);
      }
    }

    public int ReadIndex { get; private set; }

    public int WriteIndex
    {
      get { return _list != null ? _list.Count : 0; }
    }

    public void ClearRead()
    {
      ReadIndex = 0;
    }

    public byte PopByte()
    {
      return Data[ReadIndex++];
    }

    public ushort PopUInt16()
    {
      var value = (ushort) ((Data[ReadIndex++] << 8) | Data[ReadIndex++]);
      return value;
    }

    public short PopInt16()
    {
      var value = (short) ((Data[ReadIndex++] << 8) | Data[ReadIndex++]);
      return value;
    }

    public int PopInt32()
    {
      var value = (Data[ReadIndex++] << 24) |
                  (Data[ReadIndex++] << 16) |
                  (Data[ReadIndex++] << 8) |
                  Data[ReadIndex++];
      return value;
    }

    public uint PopUInt32()
    {
      var value = (uint) ((Data[ReadIndex++] << 24) |
                          (Data[ReadIndex++] << 16) |
                          (Data[ReadIndex++] << 8) |
                          Data[ReadIndex++]);
      return value;
    }

    public float PopSingle()
    {
      var value = BigEndianConverter.ToSingle(Data, ReadIndex);
      ReadIndex += sizeof(float);

      return value;
    }

    public Address PopAddress()
    {
#if ADDRESS_WIDTH_1
      return new Address(PopByte());
#endif
#if ADDRESS_WIDTH_2
            return new Address(PopShort());
#endif
#if ADDRESS_WIDTH_4
            return new Address(PopInt());
#endif
    }

    public byte[] PopArray()
    {
      var length = (Data[ReadIndex++] << 8) | Data[ReadIndex++];

      if (length == 0)
        return null;

      var array = new byte[length];
      for (var i = 0; i < length; i++)
        array[i] = Data[ReadIndex + i];

      ReadIndex += length;

      return array;
    }

    public string PopString()
    {
      var str = string.Empty;
      while (true)
      {
        var value = _list[ReadIndex++];

        if ((value == 0) || (ReadIndex == _list.Count))
          break;

        str += (char) value;
      }

      return str;
    }

    public string PopFixedString()
    {
      var size = Data[ReadIndex++];
      if (size == 0)
        return null;

      var st = Encoding.UTF8.GetString(Data, ReadIndex, size);

      ReadIndex += size;

      return st;
    }

    public string PopName()
    {
      var value = FixedString.FromArray(Data, ReadIndex, HBusSettings.NameLength);
      ReadIndex += HBusSettings.NameLength;
      return value.Trim();
    }

    public string[] PopNames()
    {
      var length = Data[ReadIndex++];
      if (length == 0)
        return null;

      var array = new string[length];
      for (var i = 0; i < length; i++)
        array[i] = PopName();

      return array;
    }

    public string[] PopStringArray()
    {
      var length = Data[ReadIndex++];
      if (length == 0)
        return null;

      var array = new string[length];
      for (var i = 0; i < length; i++)
      {
        var value = string.Empty;
        while (Data[ReadIndex] != 0)
          value += (char) Data[ReadIndex++];
        array[i] = value;
        ReadIndex++; //Skip null temination
      }

      return array;
    }

    public void ClearWrite()
    {
      _list = new List<byte>();
    }

    public void Push(byte value)
    {
      if (_list == null) _list = new List<byte>();
      _list.Add(value);
    }

    public void Push(ushort value)
    {
      if (_list == null) _list = new List<byte>();
      _list.AddRange(_littleEndian ? BitConverter.GetBytes(value) : BigEndianConverter.GetBytes(value));
    }

    public void Push(short value)
    {
      if (_list == null) _list = new List<byte>();
      _list.AddRange(_littleEndian ? BitConverter.GetBytes(value) : BigEndianConverter.GetBytes(value));
    }


    public void Push(uint value)
    {
      if (_list == null) _list = new List<byte>();
      _list.AddRange(_littleEndian ? BitConverter.GetBytes(value) : BigEndianConverter.GetBytes(value));
    }

    public void Push(int value)
    {
      if (_list == null) _list = new List<byte>();
      _list.AddRange(_littleEndian ? BitConverter.GetBytes(value) : BigEndianConverter.GetBytes(value));
    }

    public void Push(float value)
    {
      if (_list == null) _list = new List<byte>();
      _list.AddRange(_littleEndian ? BitConverter.GetBytes(value) : BigEndianConverter.GetBytes(value));
    }

    public void Push(Address address)
    {
      if (_list == null) _list = new List<byte>();
      _list.AddRange(address.ToArray());
    }

    public void Push(byte[] array)
    {
      if (_list == null) _list = new List<byte>();
      var size = array != null ? array.Length : 0;

      _list.Add((byte) (size >> 8));
      _list.Add((byte) (size & 0xff));
      if (size > 0)
        _list.AddRange(array);
    }

    /// <summary>
    ///   Push fixed length string
    /// </summary>
    /// <param name="value"></param>
    /// <param name="size"></param>
    public void Push(string value, int size)
    {
      if (_list == null) _list = new List<byte>();
      size = value != null ? Math.Min(value.Length, size) : 0;

      _list.Add((byte) size);
      if (size > 0)
        _list.AddRange(Encoding.UTF8.GetBytes(value.ToCharArray(), 0, size));
    }

    public void Push(string[] values, int size)
    {
      if (_list == null) _list = new List<byte>();

      var length = values != null ? values.Length : 0;

      _list.Add((byte) length);

      foreach (var value in values)
        Push(value, size);
    }

    public void Push(string value)
    {
      if (_list == null) _list = new List<byte>();

      if (value != null)
        _list.AddRange(Encoding.UTF8.GetBytes(value));
      _list.Add(0); //Null termination
    }

    /// <summary>
    ///   Push null terminated strings array
    /// </summary>
    /// <param name="values"></param>
    public void Push(string[] values)
    {
      if (_list == null) _list = new List<byte>();

      var length = values != null ? values.Length : 0;

      _list.Add((byte) length);

      foreach (var value in values)
        Push(value);
    }

    public void PushName(string value)
    {
      if (_list == null) _list = new List<byte>();

      _list.AddRange(FixedString.ToPaddedArray(value, HBusSettings.NameLength, ' '));
    }

    public void PushNames(string[] values)
    {
      if (_list == null) _list = new List<byte>();
      var length = values != null ? values.Length : 0;

      _list.Add((byte) length);

      foreach (var value in values)
        PushName(value);
    }

    public void PushStringArray(string[] values)
    {
      if (_list == null) _list = new List<byte>();
      var length = values != null ? values.Length : 0;

      _list.Add((byte) length);

      if (length > 0)
        foreach (var value in values)
          Push(value);
    }

    public void Clear()
    {
      ReadIndex = 0;
      _list = new List<byte>();
    }
  }
}