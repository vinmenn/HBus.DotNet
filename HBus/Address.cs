using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;

namespace HBus
{
  /// <summary>
  ///   Address width ranges
  /// </summary>
  public enum AddressWidth
  {
    NoAddress = 0x00,
    OneByte = 0x04,
    TwoBytes = 0x08,
    FourBytes = 0x0C
  }

  /// <summary>
  ///   HBus Address immutable class
  /// </summary>
  public sealed class Address
  {
    // ReSharper disable InconsistentNaming
#if ADDRESS_WIDTH_0
// Address width defines how many bytes are used for node address
        public static AddressWidth Width = AddressWidth.NoAddress;
        private const uint HOST_ADDRESS = 0x00;
        private const uint BROADCAST_ADDRESS = 0x00;
#endif
#if ADDRESS_WIDTH_1
    // Address width defines how many bytes are used for node address
    public static AddressWidth Width = AddressWidth.OneByte;
    private const uint HOST_ADDRESS = 0x81;
    private const uint BROADCAST_ADDRESS = 0xFF;
#endif
#if ADDRESS_WIDTH_2
// Address width defines how many bytes are used for node address
        public static AddressWidth Width = AddressWidth.TwoBytes;
        private const uint BROADCAST_ADDRESS = 0xFFFF;
        private const uint HOST_ADDRESS = 0x81;
#endif
#if ADDRESS_WIDTH_4
// Address width defines how many bytes are used for node address
        public static AddressWidth Width = AddressWidth.FourBytes;
        private const uint HOST_ADDRESS = 0x81;
        private const uint BROADCAST_ADDRESS = 0xFFFFFFFF;
#endif

    // ReSharper restore InconsistentNaming
    public Address(uint value)
    {
      Value = value;
    }

    /// <summary>
    ///   Default constructor
    /// </summary>
    /// <param name="data"></param>
    /// <param name="index"></param>
    public Address(byte[] data, int index = 0)
    {
#if ADDRESS_WIDTH_1
      Value = data[index + 0];
#endif
#if ADDRESS_WIDTH_2
            _value = data[index + 0] << 8 | data[index + 1];
#endif
#if ADDRESS_WIDTH_4
            _value = data[index + 0] << 24 | data[index + 1] << 16 | data[index + 2] << 8 | data[index + 3];
#endif
    }

    public uint Value { get; }

    /// <summary>
    ///   Empty address
    /// </summary>
    public static Address Empty
    {
      get { return new Address(0); }
    }

    /// <summary>
    ///   Default host address
    /// </summary>
    public static Address HostAddress
    {
      get { return new Address(HOST_ADDRESS); }
    }

    /// <summary>
    ///   Default broadcast address
    /// </summary>
    public static Address BroadcastAddress
    {
      get { return new Address(BROADCAST_ADDRESS); }
    }

    /// <summary>
    ///   Serialize address to byte array
    /// </summary>
    public byte[] ToArray()
    {
#if ADDRESS_WIDTH_1
      return new[] {(byte) Value};
#endif
#if ADDRESS_WIDTH_2
            var val = (ushort) Value;
            return BigEndianConverter.GetBytes(val);
#endif
#if ADDRESS_WIDTH_4
            return BigEndianConverter.GetBytes(Value);
#endif
    }

    public override bool Equals(object obj)
    {
      return Value.Equals(((Address) obj).Value);
    }

    public override string ToString()
    {
      return Value.ToString(CultureInfo.InvariantCulture);
    }

    public bool Equals(Address other)
    {
      return other.Value == Value;
    }

    public static bool operator ==(Address a, Address b)
    {
      return a.Equals(b);
    }

    public static bool operator !=(Address a, Address b)
    {
      return !a.Equals(b);
    }


    public override int GetHashCode()
    {
      return Value.GetHashCode();
    }

    public static Address FromArray(byte[] data, int i)
    {
#if ADDRESS_WIDTH_1
      return new Address(data[i]);
#endif
#if ADDRESS_WIDTH_2
            return new Address(BigEndianConverter.ToUInt16(data, i));
#endif
#if ADDRESS_WIDTH_4
            return new Address(BigEndianConverter.ToUInt32(data, i));
#endif
    }

    public static Address Parse(uint getUInt32)
    {
      return new Address(getUInt32);
    }

    public static Address Parse(string addr)
    {
      return new Address(uint.Parse(addr));
    }
  }

  public class AddressConverter : TypeConverter
  {
    private static readonly HashSet<Type> NumericTypes = new HashSet<Type>
    {
      typeof(int),
      typeof(uint),
      typeof(short),
      typeof(ushort),
      typeof(byte),
      typeof(decimal)
    };

    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
      return (sourceType == typeof(string)) || IsNumericType(sourceType) ||
             base.CanConvertFrom(context, sourceType);
    }

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
      object result = null;

      var stringValue = value as string;

      if (!string.IsNullOrEmpty(stringValue))
        result = new Address(Convert.ToUInt32(stringValue));
      else if ((value != null) && IsNumericType(value.GetType()))
        result = new Address((uint) value);

      return result ?? base.ConvertFrom(context, culture, value);
    }


    private static bool IsNumericType(Type type)
    {
      return NumericTypes.Contains(type) ||
             NumericTypes.Contains(Nullable.GetUnderlyingType(type));
    }
  }
}