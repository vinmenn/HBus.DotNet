using System;
using System.Collections.Generic;
using HBus.Nodes;
using HBus.Server.Data;
using UsbUirt;
using System.Linq;
using UsbUirt.EventArgs;
using UsbUirt.Enums;

namespace HBus.Server.Processors
{
  public class IrCommandEvent : Event
  {
    public string IrCode { get; set; }

  }

  public class UsbUirtProcessor : BaseProcessor
  {
    private const string Channel = "ir";
    private readonly Driver _driver;
    private readonly Receiver _receiver;
    private readonly Transmitter _transmitter;
    private readonly Learner _learner;
    private IList<IrCommandEvent> _commands;
    private IrCommandEvent _learnCommand;

    public UsbUirtProcessor(IList<IrCommandEvent> commands)
    {
      _driver = new Driver();
      Log.Debug(string.Format("Usbuirt found version {0}", Driver.GetVersion(_driver)));
      _receiver = new Receiver(_driver);
      _receiver.GenerateLegacyCodes = false;
      _receiver.Received += OnIrReceive;
      _transmitter = new Transmitter(_driver);
      _learner = new Learner(_driver);

      _commands = commands;
      _learnCommand = null;

      //Event from ep source
      OnSourceEvent = (@event, sender) =>
      {
        if (@event.Channel != Channel && !string.IsNullOrEmpty(@event.Channel)) return;

        switch (@event.Name)
        {
          case "ir-transmit":
            var ir = System.Text.Encoding.Default.GetString(@event.Data);
            _transmitter.TransmitAsync(ir);
            break;
          case "ir-learn":
            _learnCommand = @event as IrCommandEvent;
            _learner.LearnAsync(CodeFormat.Uuirt, LearnCodeModifier.Default, null);
            break;
        }
      };

      //Error from ep source
      OnSourceError = (exception, sender) =>
      {
        Log.Error("Error from source endpoint", exception);
      };

      //Close connection with ep source
      OnSourceClose = (sender) =>
      {
        //Close HBus endpoint
        Stop();

        Log.Debug("closed on source close");
      };

    }

    public override void Start()
    {
      base.Start();
    }
    public override void Stop()
    {
      base.Stop();
    }

    private void OnIrReceive(object sender, ReceivedEventArgs e)
    {
      try
      {
        if (_commands.Any(c => c.IrCode == e.IRCode))
        {
          var cmd = _commands.First(c => c.IrCode == e.IRCode);
          Send(cmd, this);
        }
      }
      catch (Exception ex)
      {
        Log.Error("Ir receivefailed", ex);
      }
    }

    private void LearnCompleted(object sender, LearnCompletedEventArgs e)
    {
      if (_learnCommand != null)
        _learnCommand.IrCode = e.Code;

      if (_commands.Any(c => c.IrCode == e.Code))
      {
        var cmd = _commands.First(c => c.IrCode == e.Code);
        _commands.Remove(cmd);
      }

      //Add new command
      if (_commands == null)
        _commands = new List<IrCommandEvent>();

      _commands.Add(_learnCommand);
      _learnCommand = null;
    }
  }
}