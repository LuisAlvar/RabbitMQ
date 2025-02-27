using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Text;

public class Sender {
  private readonly static string QUEUE_NAME = "event_queue";
  private readonly static string DEFAULT_EXCHANGE = "";

  private readonly ILogger<Sender> _logger = null!;
  private readonly IConfiguration _configuration = null!;

  private IConnection? _connection;
  private IChannel? _channel;

  public Sender(){}

  public Sender(ILogger<Sender> logger, IConfiguration configuration) {
    _logger = logger;
    _configuration = configuration;
  }

  public async void initialize()
  {
    try
    {
      ConnectionFactory factory = new ConnectionFactory() {
        HostName = "localhost",
        Port = 5672,
      };
      _connection = await factory.CreateConnectionAsync();
      _channel = await _connection.CreateChannelAsync();

      await _channel!.QueueDeclareAsync(queue: QUEUE_NAME, durable: false, exclusive: false, autoDelete: false, arguments: null);


    }
    catch (System.Exception ex)
    {
      throw ex;
    }
  }

  public async void send(string message){
    try
    {
      byte[] body = Encoding.UTF8.GetBytes(message);
      await _channel!.BasicPublishAsync(exchange: DEFAULT_EXCHANGE, routingKey: QUEUE_NAME, body: body);
    }
    catch (Exception ex)
    {
      throw ex;
    }
  }

  public async void send(string exchange, string type, string message)
  {
    try
    {
      byte[] body = Encoding.UTF8.GetBytes(message);
      await _channel!.ExchangeDeclareAsync(exchange: exchange, type: type);
      await _channel!.BasicPublishAsync(exchange: exchange, routingKey: "", body: body);
    }
    catch (Exception ex)
    {
      throw ex;
    }
  }

  public void destroy()
  {
    try
    {
      if (_channel != null 
        && _connection != null
        && _channel.IsOpen)
      {
        _channel.CloseAsync();
        _channel.Dispose();
        _connection.CloseAsync();
        _connection.Dispose();
      }
    }
    catch (Exception ex)
    {
      throw ex;
    }
  }



}