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
  /// <summary>
  /// Example of Point-to-Point Communcation 
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

    public CompetingReceiver(string id) {
      _id = id;
    }

    public CompetingReceiver(ILogger<CompetingReceiver> logger, IConfiguration configuration)
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

    /// <summary>
    /// This function is only meant to working with await CompetingReceiverDemo.MainFCFS(args);
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<string> Receive(CancellationToken cancellationToken)
    {
      if (_channel == null) Initialize();

      try
      {
        _channel!.QueueDeclare(queue: QUEUE_NAME, durable: false, exclusive: false, autoDelete: false, arguments: null);
        Console.WriteLine($"[*] {_id} waiting for messages ...");

        var consumer = new EventingBasicConsumer(_channel);
        var tcs = new TaskCompletionSource<string>();

        consumer.Received += (model, ea) =>
        {
          var body = ea.Body.ToArray();
          var message = Encoding.UTF8.GetString(body);
          Console.WriteLine($"[x] {_id} received {message}");
          tcs.SetResult($"[{_id}]: {message}");
        };

        _channel.BasicConsume(queue: QUEUE_NAME, autoAck: true, consumer: consumer);

        using (cancellationToken.Register(() => tcs.SetCanceled()))
        {
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
      catch (RabbitMQ.Client.Exceptions.OperationInterruptedException ex)
      {
        return $"[Error in {_id}]: {ex.Message}";
      }
      catch (Exception ex)
      {
        return $"[Error in {_id}]: {ex.Message}";
      }
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
}
