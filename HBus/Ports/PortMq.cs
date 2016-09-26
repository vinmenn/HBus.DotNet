using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace HBus.Ports
{
  /// <summary>
  ///   udp port implementation
  /// </summary>
  public class PortMq : Port
  {
    private const string Queue = "hbus";
    private readonly ConnectionFactory _factory;
    private IModel _channel;
    private IConnection _connection;
    private bool _listen;
    //TODO: user & password esterni
    public PortMq(string host, string user, string password, int portnumber)
      : base(portnumber, false)
    {
      _factory = new ConnectionFactory {HostName = host, UserName = user, Password = password};

      IsMulticast = true;
      IsFullDuplex = true;
      HasRoutes = true;

      Log.Debug(string.Format("PortMq({0}) created.", host));
    }

    #region Implementation of IDisposable

    public override void Dispose()
    {
      Stop();
      base.Dispose();
    }

    #endregion

    /// <summary>
    ///   Start listening on udp port
    /// </summary>
    public override void Start()
    {
      if (_listen) return;


      try
      {
        Log.Info("PortMq starting");

        _listen = true;

        base.Start();

        _connection = _factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.ExchangeDeclare("hbus-message", "fanout");

        //while (_listen)
        //new Thread(() =>
        //{
        //var queueName = _channel.QueueDeclare().QueueName;
        var rxQueue = _channel.QueueDeclare(Queue,
          false,
          false,
          true,
          null);

        //var queueName = rxQueue.QueueName;

        _channel.QueueBind(rxQueue.QueueName,
          "hbus-message",
          "");

        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += (model, ea) =>
        {
          //Process received data
          if (!ProcessData(ea.Body, ea.Body.Length))
            Log.Error("ProcessData failed");
        };

        //Consume queue from rx channel
        _channel.BasicConsume(Queue,
          true,
          consumer);

        //}).Start();
      }
      catch (Exception e)
      {
        Log.Error("PortMq start error", e);
      }
    }

    public override void Stop()
    {
      try
      {
        if (!_listen) return;

        Log.Info("PortMq stopping");

        _channel.QueueUnbind(Queue, "hbus-message", "", null);
        _channel.Close();
        _connection.Close(10);
        _listen = false;
        base.Stop();
      }
      catch (Exception e)
      {
        Log.Error("PortMq stop error", e);
      }
    }

    protected override void WritePort(byte[] buffer, int i, int length, string hwaddress)
    {
      try
      {
        if ((buffer == null) || (buffer.Length < i + length))
        {
          //Status = Status.Error;
          Log.Error("PortMq: message size error.");

          return;
        }

        using (var connection = _factory.CreateConnection())
        {
          using (var channel = connection.CreateModel())
          {
            channel.ExchangeDeclare("hbus-message", "fanout");

            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;

            channel.BasicPublish("hbus-message",
              "",
              null,
              buffer);
          }
        }
        Log.Debug(
          string.Format("PortMq.WritePort: " + buffer.Length + " bytes sent to message queue server " +
                        _factory.HostName));
      }
      catch (Exception ex)
      {
        Log.Error("PortMq.WritePort: failed sending buffer", ex);
        throw;
      }
    }
  }
}