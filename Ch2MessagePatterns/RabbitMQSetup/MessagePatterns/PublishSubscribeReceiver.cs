using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQSetup.MessagePatterns;

public class PublishSubscribeReceiver
{
  private const string EXCHANGE_NAME = "pubsub_exchange";
  private const string HOST_NAME = "localhost";
  private const int HOST_PORT = 5672;

  private readonly ILogger<PublishSubscribeReceiver> _logger;
  private readonly IConfiguration _configuration;

  private IConnection _connection;
  private IModel? _channel;

  private readonly string _id;

  public PublishSubscribeReceiver(string Id)
  {
      _id = Id;
  }

  public PublishSubscribeReceiver(ILogger<PublishSubscribeReceiver> logger, IConfiguration configuration)
  {
      _logger = logger;
      _configuration = configuration;
      _id = Guid.NewGuid().ToString();
  }

  public bool Initialize()
  {
    try
    {
      var factory = new ConnectionFactory()
      {
        HostName = HOST_NAME,
        Port = HOST_PORT,
        UserName = "guest",
        Password = "guest",
        VirtualHost = "/",
        AutomaticRecoveryEnabled = true,
      };
      _connection = Policy.Handle<BrokerUnreachableException>()
                          .Or<SocketException>()
                          .WaitAndRetry(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))
                          .Execute(() => factory.CreateConnection());
      _channel = _connection.CreateModel();
      if (_connection == null || _channel == null)
      {
        Console.WriteLine("[!] For some odd reason unable to create a connections or channels.");
        return false;
      }
      Console.WriteLine("[x] Initialized connection to RabbitMQ");
      return true;
    }
    catch (Exception ex)
    {
      Console.WriteLine("[!] While initialization error out: " + ex.ToString());
      return false;
    }
  }

  public string Receive(string queue)
  {
    if (_channel == null) Initialize();
    string message = string.Empty;
    try
    {
      _channel!.ExchangeDeclare(exchange: EXCHANGE_NAME, type: "fanout");
      _channel!.QueueDeclare(queue: queue, durable: false, exclusive: false, autoDelete: false, arguments: null);
      _channel.QueueBind(queue: queue, exchange: EXCHANGE_NAME, routingKey: " ");

      EventingBasicConsumer consumer = new EventingBasicConsumer(_channel);
      consumer.Received += (sender, args) =>
      {
          var body = args.Body.ToArray();
          var message = Encoding.UTF8.GetString(body);
          Console.WriteLine($"[x] {_id} received {message}");
      };
      _channel.BasicConsume(queue: queue, autoAck: true, consumer: consumer);
    }
    catch (IOException ex)
    {
      return $"[Error in {_id}]: {ex.Message}";
    }
    catch (OperationInterruptedException ex)
    {
      return $"[Error in {_id}]: {ex.Message}";
    }
    catch (Exception ex)
    {
      return $"[Error in {_id}]: {ex.Message}";
    }

    return message;
  }

  public void Destory()
  {
    try
    {
      if (_connection != null)
      {
        _channel?.Close();
        _channel?.Dispose();
        _connection.Close();
        _connection.Dispose();
        Console.WriteLine($"[x] {_id} closed the connection.");
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine(ex.ToString());
    }
  }

}
