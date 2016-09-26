using System;
using System.IO;
using System.Linq;
using System.Text;
using HBus.Nodes;
using HBus.Nodes.Sensors;
using System.Diagnostics;

namespace HBus.Nodes.Sensors
{
  /// <summary>
  /// Ds18b20 temperature sensor
  /// </summary>
  public class Ds18b20Sensor : Sensor
  {
    private readonly Scheduler _scheduler;
    private readonly string _iddevice;

    public new event EventHandler<SensorEventArgs> OnSensorRead;

    public Ds18b20Sensor(string iddevice, byte index, string name, string description, string location, ushort interval, Scheduler scheduler)
    {
      Index = index;
      Name = name;
      Description = description;
      Location = location;
      Interval = interval;
      Unit = "°C";
      Hardware = "ds18b20";
      MinRange = -55;
      MaxRange = 125;
      Scale = 1.0f;
      Function = FunctionType.None;
      _iddevice = iddevice;
      _scheduler = scheduler;
    }


    public override SensorRead Read()
    {
      var read = new SensorRead();

      try
      {
#if DEBUG      
        var deviceDir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        var w1file = deviceDir.GetFiles("test.txt").FirstOrDefault();
#else
        var deviceDir = new DirectoryInfo("/sys/bus/w1/devices/" + _iddevice);
        if (deviceDir == null) return read;

        var w1file = deviceDir.GetFiles("w1_slave").FirstOrDefault();
        if (w1file == null) return read;
#endif

        string text = string.Empty;

        var process = new Process();
        process.StartInfo = new ProcessStartInfo("cat", w1file.FullName);
        process.StartInfo.RedirectStandardInput = false;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow     = true;
        process.Start();
        text = process.StandardOutput.ReadToEnd();

        process.WaitForExit(2000);

        /*
        // for reading the file
        Log.Debug("read from file to end");
        using (StreamReader sr = new StreamReader(w1file.FullName))
        {
          text = sr.ReadToEnd();
        }
        */

        var temptext = text.Substring(text.IndexOf("t=") + 2);

        float value = float.Parse(temptext) / 1000;

        read = new SensorRead { Time = _scheduler.TimeIndex, Name = Name, Value = value };

        if (OnSensorRead != null)
          OnSensorRead(this, new SensorEventArgs(read));
      }
      catch (Exception ex)
      {
        Log.Error(string.Format("Ds18b20Sensor read eror"), ex);
      }

      return read;
    }

    private void readDone(IAsyncResult ar)
    {
      throw new NotImplementedException();
    }

    public override string ToString()
    {
      return Name;
    }
  }
}