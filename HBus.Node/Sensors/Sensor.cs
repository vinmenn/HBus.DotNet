using System;
using System.Reflection;
using log4net;

namespace HBus.Nodes.Sensors
{
  /// <summary>
  /// Function used to convert sensor read
  /// </summary>
  public enum FunctionType
  {
    None,
    Linear,
    Logarithmic
  }

  /// <summary>
  /// Base sensor interface
  /// </summary>
  public class Sensor
  {
    protected static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

    //Local information
    /// <summary>
    /// Sensor id (PK)
    /// </summary>
    public uint Id { get; set; }
    /// <summary>
    /// Node id (FK)
    /// </summary>
    public uint NodeId { get; set; }

    //Shared information
    /// <summary>
    /// Sensor specific name
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// Sensor index
    /// </summary>
    public byte Index { get; set; }

    /// <summary>
    /// Sensor HBus address
    /// </summary>
    public Address Address { get; set; }
    /// <summary>
    /// Sensor class
    /// </summary>
    /// <example>Temperature, Humidity, etc</example>
    public string Class { get; set; }
    /// <summary>
    /// Sensor extended description
    /// </summary>
    public string Description { get; set; }
    /// <summary>
    /// Sensor real location
    /// </summary>
    /// <example>living room</example>
    public string Location { get; set; }
    /// <summary>
    /// Measured unit
    /// </summary>
    /// <example>°C</example>
    public string Unit { get; set; }
    /// <summary>
    /// Hardware type
    /// </summary>
    /// <example>DS18B20</example>
    public string Hardware { get; set; }
    /// <summary>
    /// Min read value
    /// </summary>
    /// <example>-60.0</example>
    public float MinRange { get; set; }
    /// <summary>
    /// Max read value
    /// </summary>
    /// <example>100.0</example>
    public float MaxRange { get; set; }
    /// <summary>
    /// Scale factor of returned value
    /// default 1.0
    /// </summary>
    /// <example>2.0</example>
    public float Scale { get; set; }
    /// <summary>
    /// Type of scale of returned values
    /// default: Linear
    /// </summary>
    /// <example>Linear</example>
    public FunctionType Function { get; set; }
    /// <summary>
    /// Sensor reading interval in seconds
    /// </summary>
    public ushort Interval { get; set; }
    /// <summary>
    /// Sensor read function
    /// </summary>
    /// <returns>Measured value</returns>
    public virtual SensorRead Read()
    {
      var read = new SensorRead();
      if (OnSensorRead != null)
        OnSensorRead(this, new SensorEventArgs(read));
      return read;
    }
    /// <summary>
    /// Sensor reset handler
    /// </summary>
    public virtual void Reset()
    {
      //throw new NotImplementedException();
    }
    /// <summary>
    /// Sensor read complete
    /// </summary>
    public virtual event EventHandler<SensorEventArgs> OnSensorRead;

  }
}