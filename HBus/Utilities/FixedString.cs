using System;
using System.Text;

namespace HBus.Utilities
{
  /// <summary>
  ///   Fixed length strings utility class
  /// </summary>
  public static class FixedString
  {
    public static string FromArray(byte[] buffer, int start, int length)
    {
      if ((buffer == null) || (buffer.Length < start + length))
        return string.Empty;
      var data = new byte[length];
      Array.Copy(buffer, start, data, 0, length);
      return new string(Encoding.UTF8.GetChars(data));
    }

    public static byte[] ToArray(string text)
    {
      byte[] data = null;
      if (!string.IsNullOrEmpty(text))
      {
        data = new byte[text.Length];
        Array.Copy(Encoding.UTF8.GetBytes(text), 0, data, 0, text.Length);
      }
      return data;
    }

    public static byte[] ToArray(string text, int length)
    {
      byte[] data = null;
      if ((text.Length > 0) && (length > 0))
      {
        data = new byte[length];
        Array.Copy(Encoding.UTF8.GetBytes(text), 0, data, 0, length > text.Length ? text.Length : length);
      }
      return data;
    }

    public static byte[] ToPaddedArray(string text, int length, char pad)
    {
      if (string.IsNullOrEmpty(text))
      {
        var bytes = new byte[length];
        for (var i = 0; i < length; i++)
          bytes[i] = (byte) pad;
        return bytes;
      }
      if (text.Length > length)
        text = text.Substring(0, length);
      while (text.Length < length)
        text += pad;

      byte[] data = null;
      if (text.Length > 0)
      {
        data = new byte[text.Length];
        Array.Copy(Encoding.UTF8.GetBytes(text), 0, data, 0, text.Length);
      }
      return data;
    }

    public static string ToPaddedString(string text, int length, char pad)
    {
      if (string.IsNullOrEmpty(text))
        return null;

      if (text.Length > length)
        text = text.Substring(0, length);
      while (text.Length < length)
        text += pad;

      return text;
    }
  }
}