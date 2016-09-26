using System;
using System.IO.Ports;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HBus.Nodes.Hardware;
using HBus.Nodes.Pins;
using log4net;

namespace HBus.Nodes.Hardware
{

    /// <summary>
    /// Devantech board type
    /// </summary>
    public enum BoardType
    {
        Rly88 = 12,
        Rly816 = 13
    }
    /// <summary>
    /// Hardware abstraction Layer
    /// for board Devantech RLY816/88
    /// </summary>
    public class Rly16Hal : IHardwareAbstractionLayer
    {
        protected static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly SerialPort _port;
        private byte[] _inputs;
        private byte[] _outputs;
        private bool _inUse;
        private DateTime _lastReadIn;
        private DateTime _lastReadOut;

        public Rly16Hal(string comPort)
        {
            if (comPort == string.Empty)
            {
                return;
            }
            _port = new SerialPort(comPort, 19200, Parity.None, 8, StopBits.Two) { ReadTimeout = 150, WriteTimeout = 150 };
            _port.Open();

            _inUse = false;

            Info = new HardwareInfo() { Inputs = 8, Outputs = 8 };
            Log.Debug("Rly16Hal started");
        }
        ~Rly16Hal()
        {
            _port.Close();
        }

        #region Implementation of IHardwareAbstractionLayer
        public int Read(string pin, PinTypes type)
        {
            var num = Convert.ToByte(pin);
            if (num > 7)
                throw new ArgumentOutOfRangeException("pin");
            if (type != PinTypes.Output && type != PinTypes.Input)
                throw new ArgumentOutOfRangeException("type");

            if (type == PinTypes.Output)
            {
                return GetRelay(num) ? 1 : 0;
            }
            return GetInput(num) ? 1 : 0;
        }

        /// <summary>
        /// Write pin
        /// </summary>
        /// <param name="pin">Pin index 0-7</param>
        /// <param name="type">Type pin (only digital output allowed)</param>
        /// <param name="value">value 1 = on , 0 = off</param>
        public void Write(string pin, PinTypes type, int value)
        {
            var num = Convert.ToByte(pin);
            if (num > 7)
                throw new ArgumentOutOfRangeException("pin");
            if (type != PinTypes.Output)
                throw new ArgumentOutOfRangeException("type");

            SetRelay(num, value == 1);
        }

        public HardwareInfo Info { private set; get; }
        #endregion

        public void Update()
        {
            if (!_inUse)
            {
                lock (_port)
                {
                    _inUse = true;
                    Transmit(new byte[] { 0x19 });
                    _inputs = Receive(1);
                    Thread.Sleep(500);
                    Transmit(new byte[] { 0x5b });
                    _outputs = Receive(1);
                    Thread.Sleep(500);
                    _inUse = false;
                }
            }
        }

        private BoardType GetBoardType()
        {
            while (_inUse)
                Thread.Sleep(100);

            Transmit(new byte[] { 0x5a });
            var result = Receive(1);
            _inUse = false;

            if (result != null && result.Length > 0)
                if (Enum.IsDefined(typeof(BoardType), result[0]))
                {
                    return (BoardType)result[0];
                }

            throw new Exception("Get board type failed");
        }

        private void SetRelay(byte relay, bool value)
        {
            if (relay > 7)
                throw new ArgumentOutOfRangeException("relay");

            var code = (byte)(0x65 + relay + (value ? 0x00 : 0x0a));

            _inUse = true;
            Transmit(new[] { code });
            _inUse = false;

        }
        private bool GetRelay(byte relay)
        {
            if (relay > 7)
                throw new ArgumentOutOfRangeException("relay");

            Transmit(new byte[] { 0x5b });
            _outputs = Receive(1);
            _lastReadOut = DateTime.Now;

            return _outputs != null && _outputs.Length > 0 && (_outputs[0] & (1 << relay)) != 0;
        }
        private bool GetInput(byte pin)
        {
            if (pin > 7)
                throw new ArgumentOutOfRangeException("pin");
            Transmit(new byte[] { 0x19 });
            _inputs = Receive(1);
            _lastReadIn = DateTime.Now;

            return _inputs != null && _inputs.Length > 0 && (_inputs[0] & (1 << pin)) != 0;
        }

        private void Transmit(byte[] bytes)
        {
            var cts = new CancellationTokenSource(1000);
            cts.CancelAfter(200);

            try
            {
                var th = new Task(() => {
                    if (_port == null)
                        return;

                    if (cts.Token.IsCancellationRequested)
                        return;

                    lock (_port)
                    {
                        _port.Write(bytes, 0, bytes.Length);
                    }
                }, cts.Token);
                th.Start();
                th.Wait(500);
            }
            catch(Exception ex)
            {
                Log.Error(ex);
            }
        }
        private byte[] Receive(byte bytes)
        {
            var cts = new CancellationTokenSource(1000);
            cts.CancelAfter(200);

            if (_port == null)
                return new byte[bytes];

            try
            {
                var array = new byte[bytes];

                var th = new Task(() => {
                    if (_port == null)
                        return;

                    if (cts.Token.IsCancellationRequested)
                        return;

                    lock (_port)
                    {
                        _port.Read(array, 0, bytes);
                    }
                }, cts.Token);
                th.Start();
                th.Wait(500);

                return array;
            }
            catch
            {
                return new byte[] { 0x00 };
            }
        }

    }
}