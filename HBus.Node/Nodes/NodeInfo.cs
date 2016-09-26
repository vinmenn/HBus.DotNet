using System;
using HBus.Utilities;

namespace HBus.Nodes
{
    /// <summary>
    /// Node information:
    /// Response of command GetInfo
    /// </summary>
    public class NodeInfo
    {
        //----------------------------------------------------
        //Local properties
        //----------------------------------------------------
        public uint Id { get; set; }
        //----------------------------------------------------
        //Shared properties
        //----------------------------------------------------
        /// <summary>
        /// Node name
        /// </summary>
        public string Name { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public Address Address { get; set; }
        public string Type { get; set; }
        public string Hardware { get; set; }
        public string Version { get; set; }
        public byte DigitalInputs { get; set; }
        public byte DigitalOutputs { get; set; }
        public byte PwmOutputs { get; set; }
        public byte AnalogInputs { get; set; }
        public byte CounterInputs { get; set; }
        public byte WiresCount { get; set; }
        public byte DevicesCount { get; set; }
        public byte SensorsCount { get; set; }
        public bool HasKeypad { get; set; }
        public byte ResetPin { get; set; }

        public NodeInfo()
        {
            //
        }
        public NodeInfo(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            if (data.Length < 28)
                throw new ArgumentException("data length not sufficient to fill NodeInfo");

            var stack = new SimpleStack(data);
            Name = stack.PopName();
            Description = stack.PopString();
            Location = stack.PopString();
            Address = stack.PopAddress();
            Type = stack.PopString();
            Hardware = stack.PopName();
            Version = stack.PopName();
            DigitalInputs = stack.PopByte();
            DigitalOutputs = stack.PopByte();
            AnalogInputs = stack.PopByte();
            CounterInputs = stack.PopByte();
            PwmOutputs = stack.PopByte();
            WiresCount = stack.PopByte();
            DevicesCount = stack.PopByte();
            SensorsCount = stack.PopByte();
            ResetPin = stack.PopByte();
        }
        public byte[] ToArray()
        {
            var stack = new SimpleStack();

            stack.PushName(Name);
            stack.Push(Description);
            stack.Push(Location);
            stack.Push(Address);
            stack.Push(Type);
            stack.PushName(Hardware);
            stack.PushName(Version);
            stack.Push(DigitalInputs);
            stack.Push(DigitalOutputs);
            stack.Push(AnalogInputs);
            stack.Push(CounterInputs);
            stack.Push(PwmOutputs);
            stack.Push(WiresCount);
            stack.Push(DevicesCount);
            stack.Push(SensorsCount);
            //stack.Push(SupportedCommands);
            stack.Push(ResetPin);

            return stack.Data;
        }
        public override bool Equals(object obj)
        {
            if (!(obj is NodeInfo))
                throw new ArgumentException("obj");

            var node = (NodeInfo) obj;

            return Name == node.Name &&
                   Address == node.Address &&
                   Description == node.Description &&
                   Type == node.Type &&
                   Hardware == node.Hardware &&
                   Version == node.Version &&
                   DigitalInputs == node.DigitalInputs &&
                   DigitalOutputs == node.DigitalOutputs &&
                   AnalogInputs == node.AnalogInputs &&
                   PwmOutputs == node.PwmOutputs &&
                   CounterInputs == node.CounterInputs &&
                   DevicesCount == node.DevicesCount &&
                   SensorsCount == node.SensorsCount &&
                   HasKeypad == node.HasKeypad &&
                   ResetPin == node.ResetPin;
        }

        public bool Equals(NodeInfo other)
        {
            if (ReferenceEquals(null, other)) return false;
            return ReferenceEquals(this, other) || Equals((object) other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = (Name != null ? Name.GetHashCode() : 0);
                result = (result*397) ^ (Description != null ? Description.GetHashCode() : 0);
                result = (result*397) ^ Address.GetHashCode();
                result = (result*397) ^ (Type != null ? Type.GetHashCode() : 0);
                result = (result*397) ^ (Hardware != null ? Hardware.GetHashCode() : 0);
                result = (result*397) ^ (Version != null ? Version.GetHashCode() : 0);
                result = (result*397) ^ DigitalInputs.GetHashCode();
                result = (result*397) ^ DigitalOutputs.GetHashCode();
                result = (result*397) ^ PwmOutputs.GetHashCode();
                result = (result*397) ^ AnalogInputs.GetHashCode();
                result = (result*397) ^ CounterInputs.GetHashCode();
                result = (result*397) ^ SensorsCount.GetHashCode();
                result = (result*397) ^ DevicesCount.GetHashCode();
                result = (result*397) ^ HasKeypad.GetHashCode();
                result = (result * 397) ^ ResetPin.GetHashCode();
                return result;
            }
        }

        public static bool operator ==(NodeInfo left, NodeInfo right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(NodeInfo left, NodeInfo right)
        {
            return !Equals(left, right);
        }
        
        public override string ToString()
        {
          return Name;
        }
    }
}