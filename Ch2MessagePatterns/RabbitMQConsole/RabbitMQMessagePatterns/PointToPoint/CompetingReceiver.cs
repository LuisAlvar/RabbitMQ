using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using RabbitMQ.Client.Exceptions;
using System.Net.Sockets;
using Polly;

namespace RabbitMQMessagePatterns.PointToPoint;

/// <summary>
/// Used to subscirbe to a particular queue and receive messages from that queue. 
/// </summary>
public class CompetingReceiver
{
  private const string QUEUE_NAME = "event_queue";
  private const string HOST_NAME = "localhost";
  private const int HOST_PORT = 5672;

  private readonly ILogger<CompetingReceiver> _logger;
  private readonly IConfiguration _configuration;

  private IConnection _connection;
  private IModel? _channel;

  private readonly string _id;

  public CompetingReceiver(string id)
  {
    _id = id;
  }

  public CompetingReceiver(ILogger<CompetingReceiver> logger, IConfiguration configuration)
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
  /// Used to receive a message from the queue named event_queue
  /// </summary>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  public async Task<string> Receive(CancellationToken cancellationToken)
  {
    // _channel instance that represents the AMQP channel to the message broker.
    if (_channel == null) Initialize();

    try
    {
      // Creating the event_queue in the message broker, if not already created using QueueDeclare
      _channel!.QueueDeclare(queue: QUEUE_NAME, durable: false, exclusive: false, autoDelete: false, arguments: null);
      Console.WriteLine($"[R<--] {_id} waiting for messages ...");

      // Creating a instance that is used as the handler for messages from the event_queue queue
      var consumer = new EventingBasicConsumer(_channel); 

      // Given that we dont know when we will receive the data and we are using Tasks
      // Then we want the Task running this to wait for that data. 
      var tcs = new TaskCompletionSource<string>();

      // EventHandler declared here as oppose to creating a EventHandler type function
      // We are telling the EventingBasicConsumer instance that any time RabbitMQ detect a message for us;
      // please execute the following logic.
      consumer.Received += (model, ea) =>
      {
        var body = ea.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);
        Console.WriteLine($"[R<--] {_id} received \"{message}\" from queue [{QUEUE_NAME}]");
        tcs.SetResult(message);
      };

      // Registering the EventingBasicConsumer as a message consumer using the BasicConsume method
      // of the _channel instance that represents the AMQP channel to the message broker. 
      _channel.BasicConsume(queue: QUEUE_NAME, autoAck: true, consumer: consumer);

      // If other Task has received the message, then we will cancel the wait on the data within this Task
      // we will return 
      using (cancellationToken.Register(() => tcs.SetCanceled()))
      {
        Console.WriteLine($"[R<--] {_id} canceling");
        return await tcs.Task;
      }
    }
    catch (TaskCanceledException)
    {
      return $"{_id}: Task canceled.";
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
  }


  /// <summary>
  /// Used to close the AMQP connection and must be called explicitly when needed; closing the
  /// connection closes all AMQP channels created in that connection:
  /// </summary>
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
        Console.WriteLine($"[R<--] {_id} closed connections.");
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine(ex.ToString());
    }
  }



}
