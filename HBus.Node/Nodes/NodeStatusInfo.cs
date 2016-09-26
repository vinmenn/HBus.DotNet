using HBus.Nodes.Common;
using HBus.Utilities;

namespace HBus.Nodes
{
    /// <summary>
    /// Node's devices overall status
    /// Response of command ReadAll
    /// </summary>
    public class NodeStatusInfo
    {
        public byte[] Inputs { get; set; }
        public byte[] Outputs { get; set; }
        public ushort[] Analogs { get; set; }
        public uint[] Counters { get; set; }
        public ushort[] Pwms { get; set; }
        public string[] Devices { get; set; }
        public float[] Sensors { get; set; }
        public NodeStatusValues NodeStatus { get; set; }
        public BusStatus BusStatus { get; set; }
        public string LastActivatedInput { get; set; }
        public string LastActivatedOutput { get; set; }
        public NodeErrorCodes? LastError { get; set; }
        public uint TotalErrors { get; set; }
        public uint Time { get; set; }
        public byte Mask { get; set; }

        public NodeStatusInfo()
        {
            NodeStatus = NodeStatusValues.Unknown;
            BusStatus = BusStatus.Ready;
            LastError = 0;
            TotalErrors = 0;
            Time = 0;
            LastActivatedInput = string.Empty;
            LastActivatedOutput = string.Empty;
            Mask = 0;
            Inputs = null;
            Outputs = null;
            Analogs = null;
            Counters = null;
            Pwms = null;
            Devices = null;
            Sensors = null;
        }
        public NodeStatusInfo(byte[] data)
        {
            var stack = new SimpleStack(data);
            //Bit Mask:
            //----------
            //00: Inputs
            //01: Outputs
            //02: Analogs
            //04: Counters
            //08: Pwms
            //16: Devices
            //32: Sensors
            Mask = stack.PopByte();

            //Node global status
            Time = stack.PopUInt32();

            NodeStatus = (NodeStatusValues)stack.PopByte();
            BusStatus = (BusStatus)stack.PopByte();

            LastError = (NodeErrorCodes)stack.PopByte();
            TotalErrors = stack.PopUInt32();
            
            var size = stack.PopByte(); Inputs = size > 0 ? new byte[size] : null;
            size = stack.PopByte(); Outputs = size > 0 ? new byte[size] : null;
            size = stack.PopByte(); Analogs = size > 0 ? new ushort[size] : null;
            size = stack.PopByte(); Counters = size > 0 ? new uint[size] : null;
            size = stack.PopByte(); Pwms = size > 0 ? new ushort[size] : null;
            size = stack.PopByte(); Devices = size > 0 ? new string[size] : null;
            size = stack.PopByte(); Sensors = size > 0 ? new float[size] : null;

            LastActivatedInput = stack.PopName();
            LastActivatedOutput = stack.PopName();

            //Inputs
            if ((Mask & 0x01) != 0 && Inputs!=null)
                for (var i = 0; i < Inputs.Length; i++)
                    Inputs[i] = stack.PopByte();
            //Outputs
            if ((Mask & 0x02) != 0 && Outputs != null)
                for (var i = 0; i < Outputs.Length; i++)
                    Outputs[i] = stack.PopByte();
            //Analogs
            if ((Mask & 0x04) != 0 && Analogs != null)
                for (var i = 0; i < Analogs.Length; i++)
                    Analogs[i] = stack.PopUInt16();
            //Counters
            if ((Mask & 0x08) != 0 && Counters != null)
                for (var i = 0; i < Counters.Length; i++)
                    Counters[i] = stack.PopUInt32();
            //Pwms
            if ((Mask & 0x10) != 0 && Pwms != null)
                for (var i = 0; i < Pwms.Length; i++)
                    Pwms[i] = stack.PopUInt16();
            //Devices
            if ((Mask & 0x20) != 0 && Devices!= null)
                for (var i = 0; i < Devices.Length; i++)
                    //Get status string
                    Devices[i] = stack.PopString();
            //Sensors
            if ((Mask & 0x40) != 0 && Sensors != null)
                for (var i = 0; i < Sensors.Length; i++)
                    //Get last sensor read
                    Sensors[i] = stack.PopSingle();
        }

        /// <summary>
        /// Serialize node status to byte array
        /// </summary>
        /// <returns>data stream (byte array)</returns>
        public byte[] ToArray()
        {
            var stack = new SimpleStack();

            //Node global status
            stack.Push(Mask);
            stack.Push(Time);
            stack.Push((byte)NodeStatus);
            stack.Push((byte)BusStatus);
            stack.Push((byte)LastError);
            stack.Push(TotalErrors);

            stack.Push((byte)(Inputs!= null ? Inputs.Length : 0));
            stack.Push((byte)(Outputs != null ? Outputs.Length : 0));
            stack.Push((byte)(Analogs != null ? Analogs.Length : 0));
            stack.Push((byte)(Counters != null ? Counters.Length : 0));
            stack.Push((byte)(Pwms != null ? Pwms.Length : 0));
            stack.Push((byte)(Devices != null ? Devices.Length : 0));
            stack.Push((byte)(Sensors != null ? Sensors.Length : 0));

            stack.PushName(LastActivatedInput);
            stack.PushName(LastActivatedOutput);

            //Inputs
            if ((Mask & 0x01) != 0 && Inputs != null && Inputs.Length > 0)
                foreach (var input in Inputs)
                    stack.Push(input);

            //Outputs
            if ((Mask & 0x02) != 0 && Outputs != null && Outputs.Length > 0)
                foreach (var output in Outputs)
                    stack.Push(output);
            //Analogs
            if ((Mask & 0x04) != 0 && Analogs != null && Analogs.Length > 0)
                foreach (var analog in Analogs)
                    stack.Push(analog);
            //Counters
            if ((Mask & 0x08) != 0 && Counters != null && Counters.Length > 0)
                foreach (var counter in Counters)
                    stack.Push(counter);
            //Pwms
            if ((Mask & 0x10) != 0 && Pwms != null && Pwms.Length > 0)
                foreach (var pwm in Pwms)
                    stack.Push(pwm);
            //Devices
            if ((Mask & 0x20) != 0 && Devices != null && Devices.Length > 0)
                foreach (var device in Devices)
                    stack.Push(device);
            //Sensors
            if ((Mask & 0x40) != 0 && Sensors != null && Sensors.Length > 0)
                foreach (var sensor in Sensors)
                    stack.Push(sensor);

            return stack.Data;
        }

    }
}
