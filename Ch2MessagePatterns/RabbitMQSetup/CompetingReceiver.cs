using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ;
using RabbitMQ.Client.Events;
using System.Text;
using Polly.Retry;
using RabbitMQ.Client.Exceptions;
using System.Net.Sockets;
using Polly;

namespace RabbitMQSetup
{
  public class CompetingReceiver
  {
    private const string QUEUE_NAME = "event_queue";
    private const string HOST_NAME = "localhost";
    private const int HOST_PORT = 5672;

    private readonly ILogger<CompetingReceiver> _logger;
    private readonly IConfiguration _configuration;

    private IConnection _connection;
    private IModel? _channel;

    public CompetingReceiver() { }

    public CompetingReceiver(ILogger<CompetingReceiver> logger, IConfiguration configuration)
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
        _connection = RetryPolicy.Handle<BrokerUnreachableException>()
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
      catch (System.Exception ex)
      {
        Console.WriteLine("[!] While initialization error out: " + ex.ToString());
        return false;
      }
    }

    public string Receive()
    {
      if (_channel == null) Initialize();

      string message = string.Empty;

      try
      {
        _channel!.QueueDeclare(queue: QUEUE_NAME, durable: false, exclusive: false, autoDelete: false, arguments: null);

        Console.WriteLine("[*] Waiting for messages: ");

        var consumer = new EventingBasicConsumer(_channel);

        if (consumer != null)
        {
          consumer.Received += (model, ea) =>
          {
            var body = ea.Body.ToArray();
            message = Encoding.UTF8.GetString(body);
            Console.WriteLine($"[x] Received {message}");
          };
        }
      }
      catch (IOException ex)
      {
        Console.WriteLine(ex.Message);
      }
      catch (RabbitMQ.Client.Exceptions.OperationInterruptedException ex)
      {
        Console.WriteLine(ex.Message);
      }

      return message;
    }

    public void Destory()
    {
      try
      {
        if (_connection != null)
        {
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
}
