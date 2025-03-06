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

namespace RabbitMQSender.MessagePatterns;

public class Sender
{
  private const string QUEUE_NAME = "event_queue";
  private const string DEFAULT_EXCHANGE = "";

  private const string REQUEST_QUEUE = "request_queue";
  private const string RESPONSE_QUEUE = "response_queue";

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
        AutomaticRecoveryEnabled = true
      };
      _connection = Policy.Handle<BrokerUnreachableException>()
                               .Or<SocketException>()
                               .WaitAndRetry(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))
                               .Execute(() => factory.CreateConnection());
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
  /// </summary>
  /// <param name="message"></param>
  public bool Send(string message)
  {
    try
    {
      Console.WriteLine($"[S-->] Sending Message ---> {message}");
      byte[] body = Encoding.UTF8.GetBytes(message);
      _channel.QueueDeclare(queue: QUEUE_NAME, durable: false, exclusive: false, autoDelete: false, arguments: null);
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
      _channel.ExchangeDeclare(exchange: exchange, type: type);
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
  /// 
  /// </summary>
  /// <param name="correlationId"></param>
  /// <returns></returns>
  public Task<string> WaitForResponse(string correlationId)
  {
    var tcs = new TaskCompletionSource<string>();

    try
    {
      var consumer = new EventingBasicConsumer(_channel);
      _channel.BasicConsume(queue: RESPONSE_QUEUE, autoAck: false, consumer: consumer);

      Console.WriteLine($"[S-->] waiting on message from {RESPONSE_QUEUE}");

      consumer.Received += (model, ea) => {
        var body = ea.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);
        var props = ea.BasicProperties;

        Console.WriteLine($"[S-->] receiving the following messsage: {message}");

        if (props != null)
        {
          var msgCorrelationId = props.CorrelationId;

          if (!correlationId.Equals(msgCorrelationId))
          {
            Console.WriteLine("[S-->] Received response of another request.");
          }
          else
          {
            tcs.SetResult(message);
            _channel.BasicAck(ea.DeliveryTag, false);
          }
        }
      };
      return tcs.Task;
    }
    catch (IOException e)
    {
      return Task.FromResult($"Error: {e.Message}");
    }
    catch (OperationInterruptedException e)
    {
      return Task.FromResult($"Error: {e.Message}");
    }
    catch (Exception e)
    {
      return Task.FromResult($"Error: {e.Message}");
    }
  }


  /// <summary>
  /// Close the connection and all channels to the message broker
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


