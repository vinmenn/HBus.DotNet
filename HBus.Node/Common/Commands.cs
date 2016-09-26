namespace HBus.Nodes.Common
{
    /// <summary>
    /// HBus node commands
    /// </summary>
    public static class NodeCommands 
    {
        #region Command codes
        // ReSharper disable InconsistentNaming
        //------------------------------------------------
        // Commands
        //------------------------------------------------
        //Bus
        public const byte CMD_PING = 0xE0;
        public const byte CMD_DISCOVER_MAP = 0xE1;
        public const byte CMD_SET_MAP = 0xE2;
        public const byte CMD_GET_MAP = 0xE3;
        //public const byte CMD_GET_COMMANDS = 0xE4;
        //public const byte CMD_GET_STATUS = 0xE5;

       //Configuration
        public const byte CMD_RESET = 0x01;
        public const byte CMD_FACTORY_DEFAULT = 0x02;
        public const byte CMD_READ_CONFIG = 0x03;
        public const byte CMD_WRITE_CONFIG = 0x04;
        public const byte CMD_SET_PIN = 0x05;
        public const byte CMD_CONNECT = 0x06;
        public const byte CMD_START = 0x07;
        public const byte CMD_STOP = 0x08;
        public const byte CMD_ADD_NODE_LISTENER = 0x09;
        public const byte CMD_DELETE_NODE_LISTENER = 0x0A;
        public const byte CMD_PUSH_NODE_STATUS = 0x0B;

        //Write commands
        public const byte CMD_CHANGE_DIGITAL = 0x10;
        public const byte CMD_TOGGLE_DIGITAL = 0x11;
        public const byte CMD_TIMED_DIGITAL = 0x12;
        public const byte CMD_DELAY_DIGITAL = 0x13;
        public const byte CMD_PULSE_DIGITAL = 0x14;
        public const byte CMD_CYCLE_DIGITAL = 0x15;
        public const byte CMD_CHANGE_ALL_DIGITAL = 0x16;
        public const byte CMD_CHANGE_PWM = 0x17;
        public const byte CMD_CHANGE_PIN = 0x18;
        public const byte CMD_DELAY_TOGGLE_DIGITAL = 0x19;
        public const byte CMD_DELTA_PWM = 0x1A;
        public const byte CMD_FADE_PWM = 0x1B;

        //Read commands
        public const byte CMD_READ_PIN = 0x20;
        public const byte CMD_READ_KEY = 0x21;
        public const byte CMD_READ_ALL = 0x22;
        public const byte CMD_READ_ACTIVE = 0x23;
        public const byte CMD_READ_LAST_INPUT = 0x24;
        public const byte CMD_READ_LAST_ACTIVE = 0x25;
        
        //Information commands
        public const byte CMD_GET_INFO = 0x30;
        public const byte CMD_GET_PIN_INFO = 0x31;
        public const byte CMD_GET_CONNECT_INFO = 0x32;
        public const byte CMD_GET_SENSOR_INFO = 0x33;
        public const byte CMD_GET_DEVICE_INFO = 0x34;
        public const byte CMD_GET_NAME_INFO = 0x35;

        //Pin commands
        public const byte CMD_ACTIVATE = 0x40;
        public const byte CMD_DEACTIVATE = 0x41;
        public const byte CMD_MULTI_ACTIVATE = 0x42;
        public const byte CMD_ADD_PIN_LISTENER = 0x43;
        public const byte CMD_DELETE_PIN_LISTENER = 0x44;
        public const byte CMD_PUSH_PIN_EVENT = 0x45;

        //Sensor commands
        public const byte CMD_READ_SENSOR = 0x50;
        public const byte CMD_ADD_SENSOR_LISTENER = 0x51;
        public const byte CMD_DELETE_SENSOR_LISTENER = 0x52;
        public const byte CMD_PUSH_SENSOR_READ = 0x53;
        public const byte CMD_RESET_SENSOR = 0x54;

        //Device commands
        public const byte CMD_GET_DEVICE_STATUS = 0x60;
        public const byte CMD_EXECUTE_DEVICE_ACTION = 0x61;
        public const byte CMD_ADD_DEVICE_LISTENER = 0x62;
        public const byte CMD_DELETE_DEVICE_LISTENER = 0x63;
        public const byte CMD_PUSH_DEVICE_EVENT = 0x64;
        // ReSharper restore InconsistentNaming
        #endregion
    }
    /// <summary>
    /// HBus node errors codes
    /// </summary>
    public enum NodeErrorCodes
    {
        GenericError = 0x10,
        PinNotFound = 0x70,
        SensorNotFound = 0x71,
        PinTypeInvalid = 0x72,
        AddressNotFound = 0x73,
        MaxListeners = 0x74,
        SensorReadFailed = 0x75,
        DeviceNotFound = 0x76,
        DeviceActionUnknown = 0x77,
        WireNotFound = 0x78,
        CommandFailed = 0x79,
        CommandNotSupported = 0x7a,
        NodeNotFound = 0x7b
    }
}
