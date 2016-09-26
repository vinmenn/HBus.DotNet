using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HBus.Nodes;
using HBus.Nodes.Devices;
using HBus.Server.Data;
using log4net;
using UsbUirt;

namespace HBus.Server.Devices
{
    public struct IrCommand
    {
        public string Name { get; set; }
        public string IrCode { get; set; }
        public CodeFormat Format { get; internal set; }
        public int Repeat { get; internal set; }
        public TimeSpan WaitTime { get; internal set; }
    }
    public class Uirt : Device
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private Controller _ctrl;
        private readonly IList<IrCommand> _commands;
        public Uirt(string name, IList<IrCommand> commands, Scheduler scheduler)
        {
            _commands = commands;

            _ctrl = new Controller();
            _ctrl.Received += OnReceived;
        }

        public void OnNext(Event value)
        {
            if (_commands.Any(c => c.Name == value.Name))
            {
                var cmd = _commands.First( c=> c.Name == value.Name);

                _ctrl.TransmitAsync(cmd.IrCode, cmd.Format, cmd.Repeat, cmd.WaitTime );
            }
        }

        public void OnError(Exception error)
        {
            Log.Error("Error from iot source", error);
        }

        public void OnCompleted()
        {
            Log.Info("Iot source ended");
        }

        private void OnReceived(object sender, ReceivedEventArgs e)
        {
            if (_commands.Any(c => c.IrCode == e.IRCode))
            {
                var cmd = _commands.First(c => c.IrCode == e.IRCode);
                var iot = new Event
                {
                    Name = Name,
                    Channel = "ircommand",
                    Source = cmd.Name,
                    Status = cmd.IrCode,
                    Timestamp = DateTime.Now
                };

                //foreach (var o in Consumers)
                //    o.OnNext(iot);
            }
        }
    }
}
