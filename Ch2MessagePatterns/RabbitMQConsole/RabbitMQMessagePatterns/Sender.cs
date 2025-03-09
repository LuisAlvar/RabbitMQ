using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System.Data.Common;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace RabbitMQMessagePatterns;

public class Sender
{
  /// <summary>
  /// Used for either PointToPoint and RequestReply
  /// </summary>
  private const string DEFAULT_EXCHANGE = "";

  #region PointToPoint Properties
  private const string QUEUE_NAME = "event_queue";
  #endregion 

  #region PublishSubscribe Properites
  private const string REQUEST_QUEUE = "request_queue";
  private const string RESPONSE_QUEUE = "response_queue";
  #endregion

  #region MessageRouter Properties
  private const string SEMINAR_QUEUE = "seminar_queue";
  private const string HACKATON_QUEUE = "hackaton_queue";
  private const string TOPIC_EXCHANGE = "topic_exchange";
  #endregion

  private const string HOST_NAME = "localhost";
  private const int HOST_PORT = 5672;

  private readonly ILogger<Sender> _logger;
  private readonly IConfiguration _configuration;

  private IConnection _connection;
  private IModel _channel;

  public Sender() { }

  public Sender(ILogger<Sender> logger, IConfiguration configuration)
  {
    _logger = logger;
    _configuration = configuration;
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
        Console.WriteLine("[S-->] For some odd reason unable to create a connections or channels.");
        return false;
      }
      Console.WriteLine("[S-->] Initialized connection to RabbitMQ");
      return true;
    }
    catch (Exception ex)
    {
      Console.WriteLine("[S!] While initialization error out: " + ex.ToString());
      return false;
    }
  }

  /// <summary>
  /// Appropriate for Point-to-Point type of communication
  /// <br />
  /// Use to accepts a message and sends it to the default queue
  /// </summary>
  /// <param name="message"></param>
  public bool Send(string message)
  {
    try
    {
      Console.WriteLine($"[S-->] Sending Message ---> {message}");
      byte[] body = Encoding.UTF8.GetBytes(message);

      // Declares a queue in the message broker using this method; if the queue is already created
      // then it is not recreated by the method.
      _channel.QueueDeclare(queue: QUEUE_NAME, durable: false, exclusive: false, autoDelete: false, arguments: null);
      
      // Publishes a message on the default exchange that is delivered to that queue
      _channel.BasicPublish(exchange: DEFAULT_EXCHANGE, routingKey: QUEUE_NAME, basicProperties: null, body: body);
    }
    catch (Exception ex)
    {
      Console.WriteLine(ex.ToString());
      return false;
    }
    return true;
  }

  /// <summary>
  /// Appropriate for Publish-Subscriber type of communication 
  /// </summary>
  /// <param name="exchange"></param>
  /// <param name="type"></param>
  /// <param name="message"></param>
  public bool Send(string exchange, string type, string message)
  {
    try
    {
      Console.WriteLine($"[S-->] Sending Message ---> {message}");
      byte[] body = Encoding.UTF8.GetBytes(message);

      // Declares the specified exchange along with its type on the message bus using this method;
      // the exchange is not recreated if it exists on the message bus. 
      _channel.ExchangeDeclare(exchange: exchange, type: type);

      Console.Write($"[S-->] Sender waiting ... seting up exchange [{exchange}] of type [{type}] ... ");
      Task.Delay(4000).Wait();
      Console.WriteLine("Done");

      // Sends a message to this exchange with a routing key equal to string.empty
      // (indicating that we will not use the the routing key)
      _channel.BasicPublish(exchange: exchange, routingKey: "", basicProperties: null, body: body);
    }
    catch (Exception ex)
    {
      Console.WriteLine(ex.ToString());
      return false;
    }
    return true;
  }


  /// <summary>
  /// Appropriate for request-reply communication
  /// <br/>
  /// The sender will send a message to the default exchange with a routing key that 
  /// matches the name of the designated request queue. 
  /// </summary>
  /// <param name="requestqueue"></param>
  /// <param name="message"></param>
  /// <param name="correlationId"></param>
  /// <returns></returns>
  public bool SendRequest(string requestqueue, string message, string correlationId)
  {
    try
    {
      var body = Encoding.UTF8.GetBytes(message);

      _channel.QueueDeclare(queue: REQUEST_QUEUE, durable: false, exclusive: false, autoDelete: false, arguments: null);
      _channel.QueueDeclare(queue: RESPONSE_QUEUE, durable: false, exclusive: false, autoDelete: false, arguments: null);

      // It takes some time to create this exchange and queue and binding the first time around. 
      Console.Write($"[S-->] Sender waiting ... seting up queues [{REQUEST_QUEUE},{RESPONSE_QUEUE}] with default exchange ... ");
      Task.Delay(4000).Wait();
      Console.WriteLine("Done");

      var properties = _channel.CreateBasicProperties();
      properties.CorrelationId = correlationId.ToString();
      properties.ReplyTo = RESPONSE_QUEUE;

      _channel.BasicPublish(exchange: DEFAULT_EXCHANGE, routingKey: REQUEST_QUEUE, mandatory: false, basicProperties: properties, body: body);
      Console.WriteLine($"[S-->] sending message ({message}) to [{REQUEST_QUEUE}]");
      return true;
    }
    catch (Exception ex)
    {
      Console.WriteLine(ex.ToString());
      return false;
    }
  }

  /// <summary>
  /// Used to as a way to ack that receiver received its request. This completes the roundtrip.
  /// </summary>
  /// <param name="correlationId"></param>
  /// <returns></returns>
  public async Task<string> WaitForResponse(string correlationId)
  {
    var tcs = new TaskCompletionSource<string>();

    try
    {
      var consumer = new EventingBasicConsumer(_channel);
      _channel.BasicConsume(queue: RESPONSE_QUEUE, autoAck: false, consumer: consumer);

      consumer.Received += (model, ea) => {
        var body = ea.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);
        var props = ea.BasicProperties;

        Console.WriteLine($"[S<--] receiving the following messsage: {message} from queue [{RESPONSE_QUEUE}]");

        if (props != null)
        {
          var msgCorrelationId = props.CorrelationId;

          if (!correlationId.Equals(msgCorrelationId))
          {
            Console.WriteLine("[S<--] Received response of another request.");
          }
          else
          {
            Console.WriteLine("[S<--] Acquired data flag Tasks that we have messsage");
            tcs.SetResult(message);
            _channel.BasicAck(ea.DeliveryTag, false);
          }
        }
      };

      Console.WriteLine($"[S<--] waiting on message from {RESPONSE_QUEUE} ...");
      Task.Delay(8000).Wait();

      return await tcs.Task;
    }
    catch (IOException e)
    {
      return await Task.FromResult($"Error: {e.Message}");
    }
    catch (OperationInterruptedException e)
    {
      return await Task.FromResult($"Error: {e.Message}");
    }
    catch (Exception e)
    {
      return await Task.FromResult($"Error: {e.Message}");
    }
  }
  
  /// <summary>
  /// Message router type of communication
  /// </summary>
  /// <param name="exchange"></param>
  /// <param name="message"></param>
  /// <param name="messageKey"></param>
  public void SendEvent(string exchange, string message, string messageKey)
  {
    try
    {

      var body = Encoding.UTF8.GetBytes(message);

      _channel.ExchangeDeclare(exchange: exchange, type: "topic", durable: false, autoDelete: false, arguments: null);
      _channel.QueueDeclare(queue: SEMINAR_QUEUE, durable: false, exclusive: false, autoDelete: false, arguments: null);
      _channel.QueueDeclare(queue: HACKATON_QUEUE, durable: false, exclusive: false, autoDelete: false, arguments: null);

      _channel.QueueBind(queue: SEMINAR_QUEUE, exchange: TOPIC_EXCHANGE, routingKey: "seminar.#", arguments: null);
      _channel.QueueBind(queue: HACKATON_QUEUE, exchange: TOPIC_EXCHANGE, routingKey: "hackaton.#", arguments: null);

      _channel.BasicPublish(exchange: TOPIC_EXCHANGE, routingKey: messageKey, basicProperties: null, body: body);

    }
    catch (Exception ex)
    {
      Console.WriteLine(ex.ToString());
    }
  }

  /// <summary>
  /// Used to close the AMQP connection and must be called explicitly when needed; closing the
  /// connection closes all AMQP channels created in that connection. 
  /// </summary>
  public void Destroy()
  {
    try
    {
      if (_connection != null
        && _channel != null
        && _channel.IsOpen)
      {
        _channel.Close();
        _channel.Dispose();
        _connection.Close();
        _connection.Dispose();
        Console.WriteLine("[S-->] Closed all connections");
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine(ex.ToString());
    }
  }
}

