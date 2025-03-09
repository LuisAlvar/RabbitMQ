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

namespace RabbitMQMessagePatterns.PublishSubscribe;

/// <summary>
/// Used to bind a specific queue to a fanout exchange and receive messages from it
/// </summary>
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

  /// <summary>
  /// Used to initialize the message sender
  /// </summary>
  /// <returns></returns>
  public bool Initialize()
  {
    try
    {
      // Creating a ConnectionFactory that is used to create AMQP connections to 
      // a running RabbitMQ server instance; in this case, this is an instance running on 
      // localhost and accepting connections on the default port (5672)
      var factory = new ConnectionFactory()
      {
        HostName = HOST_NAME,
        Port = HOST_PORT,
        UserName = "guest",
        Password = "guest",
        VirtualHost = "/",
        AutomaticRecoveryEnabled = true
      };

      // Creating a new connection using the connection factory 
      // Using Polly to retry 5 times trying to establish a connection
      _connection = Policy.Handle<BrokerUnreachableException>()
                               .Or<SocketException>()
                               .WaitAndRetry(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))
                               .Execute(() => factory.CreateConnection());

      // Creating a new channel for sending messages in the created connection
      _channel = _connection.CreateModel();
      if (_connection == null || _channel == null)
      {
        Console.WriteLine("[R<--!] For some odd reason unable to create a connections or channels.");
        return false;
      }
      Console.WriteLine("[R<--] Initialized connection to RabbitMQ");
      return true;
    }
    catch (Exception ex)
    {
      Console.WriteLine("[R<--!] While initialization error out: " + ex.ToString());
      return false;
    }
  }

  /// <summary>
  /// Used to retrieve a message froma queue that is bound to the pubsub_exchange fanout exchange
  /// </summary>
  /// <param name="queue"></param>
  /// <returns></returns>
  public Task<string> Receive(string queue)
  {
    if (_channel == null) Initialize();
    try
    {
      Console.WriteLine($"[R<--] {_id} waiting for messages ....");

      // Creates the pubsub_exchagne, if not already created
      _channel!.ExchangeDeclare(exchange: EXCHANGE_NAME, type: "fanout");

      // Creates the specific queue if not already created
      _channel!.QueueDeclare(queue: queue, durable: false, exclusive: false, autoDelete: false, arguments: null);

      // Binds the queue to the pubsub_exchange using this method, we dont specify any particular binding key
      _channel.QueueBind(queue: queue, exchange: EXCHANGE_NAME, routingKey: " ");

      // It takes some time to create this exchange and queue and binding the first time around. 
      Console.Write($"[R<--] {_id} waiting ... seting up binding with queue [{queue}] and exchange [{EXCHANGE_NAME}] ... ");
      Task.Delay(5000).Wait();
      Console.WriteLine("Done");

      EventingBasicConsumer consumer = new EventingBasicConsumer(_channel);
      TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();
      consumer.Received += (sender, args) =>
      {
        var body = args.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);
        Console.WriteLine($"[R<--] {_id} received message \"{message}\" from queue [{queue}]");
        tcs.SetResult(message);
      };

      // Registering the EventingBasicConsumer as a message consumer using the BasicConsume method
      // of the _channel instance that represents the AMQP channel to the message broker. 
      _channel.BasicConsume(queue: queue, autoAck: true, consumer: consumer);
      return tcs.Task;
    }
    catch (IOException ex)
    {
      return Task.FromResult($"[Error in {_id}]: {ex.Message}");
    }
    catch (OperationInterruptedException ex)
    {
      return Task.FromResult($"[Error in {_id}]: {ex.Message}");
    }
    catch (Exception ex)
    {
      return Task.FromResult($"[Error in {_id}]: {ex.Message}");
    }
  }

  /// <summary>
  /// Used to close the AMQP connection and must be called explicitly when needed; closing the
  /// connection closes all AMQP channels created in that connection:
  /// </summary>
  public void Destroy()
  {
    try
    {
      if (_connection != null)
      {
        _channel?.Close();
        _channel?.Dispose();
        _connection.Close();
        _connection.Dispose();
        Console.WriteLine($"[R<--] {_id} closed the connection.");
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine(ex.ToString());
    }
  }

}
