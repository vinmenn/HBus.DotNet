using System;
using System.Collections.Generic;
using System.Linq;

namespace HBus.Utilities
{
  /// <summary>
  ///   Utility class for csv to array conversions
  /// </summary>
  public static class Csv
  {
    public static IList<T> CsvToList<T>(string p)
    {
      var list = new List<T>();

      if (string.IsNullOrEmpty(p)) return list;

      if (p.IndexOf(',') < 0)
      {
        //Single item
        list.Add((T) Convert.ChangeType(p, typeof(T)));
      }
      else
      {
        if (p.EndsWith(","))
          p = p.Remove(p.Length - 1);
        //Muliple items
        list.AddRange(p.
          Split(',').
          Select(item => (T) Convert.ChangeType(item, typeof(T))));
      }
      return list;
    }

    public static IDictionary<string, string> JsonToDictionary(string p)
    {
      var dict = new Dictionary<string, string>();

      if (string.IsNullOrEmpty(p)) return dict;

      var list = p.Split(',');

      foreach (var l in list)
      {
        var parts = l.Split('=');
        if (parts.Length < 2) continue;

        var key = parts[0].Replace("\"", "").Trim();
        var value = parts[1].Replace("\"", "").Trim();

        dict.Add(key, value);
      }

      return dict;
    }

    public static string ListToCsv<T>(IList<T> list)
    {
      if ((list == null) || (list.Count == 0))
        return string.Empty;
      var csv = list.Aggregate(string.Empty, (current, l) => current + l.ToString() + ",");

      var p = !string.IsNullOrEmpty(csv) ? csv.Remove(csv.Length - 1) : csv;
      if (p.EndsWith(","))
        p = p.Remove(p.Length - 1);

      return p;
    }

    public static string DictionaryToJson(IDictionary<string, string> dictionary)
    {
      if ((dictionary == null) || (dictionary.Count == 0))
        return string.Empty;

      var s = dictionary.Aggregate(string.Empty, (current, kv) =>
        current +
        string.Format("\"{0}\"=\"{1}\",", kv.Key, kv.Value));

      return !string.IsNullOrEmpty(s) ? s.Remove(s.Length - 1) : s;
    }
  }
}