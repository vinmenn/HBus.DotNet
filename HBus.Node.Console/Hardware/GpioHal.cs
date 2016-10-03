using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HBus.Nodes.Hardware;
using HBus.Nodes.Pins;
using log4net;
using Raspberry.IO.GeneralPurpose;
using System.Collections.Generic;
using System.Linq;

namespace HBus.Nodes.Hardware
{

  /// <summary>
  /// Hardware abstraction Layer
  /// for Raspberry GPIO
  /// </summary>
  public class GpioHal : IHardwareAbstractionLayer
  {
    protected static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

    private bool _inUse;
    private readonly IGpioConnectionDriver _driver;
    private readonly PinResistor _inputResistor;
    private readonly Dictionary<string, ProcessorPin> _pins;
    private ProcessorPins _globalPins = ProcessorPins.None;
    private ProcessorPins _pinValues;

    public GpioHal(IList<Pin> pins, string driverName = "", bool useProcessorPinsNames = false, PinResistor inputResistor = PinResistor.PullUp)
    {
      _inUse = false;
      _driver = GetDriver(driverName);  // GPIO driver
      _inputResistor = inputResistor;   // Pullup/pulldown input resistors
      _pins = new Dictionary<string, ProcessorPin>(); // Global pins

      foreach (var pin in pins)
      {
        ProcessorPin procPin;

        if (useProcessorPinsNames)
        {
          ConnectorPin userPin;

          if (!Enum.TryParse(pin.Source, true, out userPin))
          {
            throw new HardwareException(string.Format("raspberry connector pin {0} not found", pin.Source));
          }

          procPin = userPin.ToProcessor();
        }
        else
        {
          if (!Enum.TryParse(pin.Source, true, out procPin))
          {
            throw new HardwareException(string.Format("raspberry processor pin {0} not found", pin.Source));
          }
        }

        switch (pin.Type)
        {
          case PinTypes.Input:
          case PinTypes.Counter:
          case PinTypes.Analog:

            //Allocate pin
            _driver.Allocate(procPin, PinDirection.Input);

            //Set pullup/pulldown resistor
            if (_inputResistor != PinResistor.None && (_driver.GetCapabilities() & GpioConnectionDriverCapabilities.CanSetPinResistor) > 0)
              _driver.SetPinResistor(procPin, _inputResistor);

            break;
          case PinTypes.Output:
          case PinTypes.Pwm:
            //Allocate output pin
            _driver.Allocate(procPin, PinDirection.Output);
            break;
        }

        //set input pins in global input pins
        _globalPins |= (ProcessorPins)((uint)1 << (int)procPin);

        //Add proessor pin
        _pins.Add(pin.Source, procPin);

      }
      Info = new HardwareInfo
      {
        Name = "Raspberry model " + Raspberry.Board.Current.Model + "GPIO HAL",
        Inputs = GpioConnectionSettings.ConnectorPinout == Raspberry.ConnectorPinout.Rev2 ? 26 : 17,
        Outputs = GpioConnectionSettings.ConnectorPinout == Raspberry.ConnectorPinout.Rev2 ? 26 : 17,
        Analogs = 0,
        Pwms = 0,
        Counters = 0,
        Vendor = "Raspberry foundation"
      };
    }

    ~GpioHal()
    {
      foreach (var pin in _pins.Values)
      {
        _driver.Release(pin);
      }
    }

    #region Implementation of IHardwareAbstractionLayer
    /// <summary>
    /// Read from input/output pin
    /// </summary>
    /// <param name="connectorPin">Connector Pin name</param>
    /// <param name="type">Pin to be read</param>
    /// <returns></returns>
    public int Read(string connectorPin, PinTypes type)
    {
      if (!_pins.ContainsKey(connectorPin))
      {
        throw new HardwareException(string.Format("raspberry pin {0} not found", connectorPin));
      }
      if (type != PinTypes.Output && type != PinTypes.Input)
      {
        throw new HardwareException(string.Format("Pin {0} is not an input or output", connectorPin));
      }

      var procPin = _pins[connectorPin];

      var inputPin = (ProcessorPins)((uint)1 << (int)procPin);

      var read = (_globalPins & inputPin) != ProcessorPins.None;

      return read ? 1 : 0;
    }

    /// <summary>
    /// Write pin
    /// </summary>
    /// <param name="connectorPin">Connector Pin name</param>
    /// <param name="type">Type pin (only digital output allowed)</param>
    /// <param name="value">value 1 = on , 0 = off</param>
    public void Write(string connectorPin, PinTypes type, int value)
    {
      if (!_pins.ContainsKey(connectorPin))
      {
        throw new HardwareException(string.Format("raspberry pin {0} not found", connectorPin));
      }
      var procPin = _pins[connectorPin];

      if (type != PinTypes.Output)
      {
        throw new HardwareException(string.Format("Pin {0} is not an output", connectorPin));
      }

      _driver.Write(procPin, value == 1);
    }

    public HardwareInfo Info { private set; get; }
    #endregion

    public void Update()
    {
      _pinValues = _driver.Read(_globalPins);
    }
    private static IGpioConnectionDriver GetDriver(string driverName)
    {
      switch (driverName)
      {
        case "default":
          return new GpioConnectionDriver();
        case "memory":
          return new MemoryGpioConnectionDriver();
        case "file":
          return new FileGpioConnectionDriver();
        default:
          return GpioConnectionSettings.GetBestDriver(GpioConnectionDriverCapabilities.None);
      }
    }
  }
}