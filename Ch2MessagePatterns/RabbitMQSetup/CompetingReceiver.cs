using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ;
using RabbitMQ.Client.Events;
using System.Text;

namespace RabbitMQSetup
{
  public class CompetingReceiver
  {
    private string QUEUE_NAME = "event_queue";
    private string HOST_NAME = "";
    private int HOST_PORT = 0;

    private readonly ILogger<CompetingReceiver> _logger = null!;
    private readonly IConfiguration _configuration = null!;

    private IConnection? _connection;
    private IChannel? _channel;

    public CompetingReceiver() { }

    public CompetingReceiver(ILogger<CompetingReceiver> logger, IConfiguration configuration)
    {
      _logger = logger;
      _configuration = configuration;
    }

    public async void initialize()
    {
      try
      {
        ConnectionFactory factory = new ConnectionFactory()
        {
          HostName = "localhost",
          Port = 5672,
        };
        _connection = await factory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();
      }
      catch (System.Exception ex)
      {
        Console.WriteLine(ex.ToString());
      }
    }

    public async Task<string> receive()
    {
      if (_channel == null) initialize();

      string message = string.Empty;

      try
      {
        await _channel!.QueueDeclareAsync(QUEUE_NAME, false, false, false, null);

        Console.WriteLine("[*] Waiting for messages: ");

        AsyncEventingBasicConsumer? consumer = new AsyncEventingBasicConsumer(_channel);
       
        if (consumer != null)
        {
          consumer.ReceivedAsync += (model, ea) =>
          {
            var body = ea.Body.ToArray();
            message = Encoding.UTF8.GetString(body);
            Console.WriteLine($"[x] Received {message}");
            return Task.CompletedTask;
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

    public void destroy()
    {
      try
      {
        if ( _connection != null)
        {
          _connection.CloseAsync();
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
