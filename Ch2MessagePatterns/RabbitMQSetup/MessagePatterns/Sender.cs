using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System.Data.Common;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace RabbitMQSetup.MessagePatterns;

public class Sender
{
  private const string QUEUE_NAME = "event_queue";
  private const string DEFAULT_EXCHANGE = "";
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

  /// <summary>
  /// Appropriate for Point-to-Point type of communication
  /// </summary>
  /// <param name="message"></param>
  public bool Send(string message)
  {
    try
    {
      Console.WriteLine($"Sending Message ---> {message}");
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
      Console.WriteLine($"Sending Message ---> {message}");
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
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine(ex.ToString());
    }
  }
}
