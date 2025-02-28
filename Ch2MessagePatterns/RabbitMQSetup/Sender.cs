using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Text;

namespace RabbitMQ;

public class Sender {
  private string QUEUE_NAME = "event_queue";
  private string DEFAULT_EXCHANGE = "";
  private string HOST_NAME = "";
  private int HOST_PORT = 0;

  private readonly ILogger<Sender> _logger = null!;
  private readonly IConfiguration _configuration = null!;

  private IConnection _connection;
  private IChannel _channel;

  public Sender(){}

  public Sender(ILogger<Sender> logger, IConfiguration configuration) {
    _logger = logger;
    _configuration = configuration;
  }

  public async Task<bool> initialize()
  {
    try
    {
      ConnectionFactory factory = new ConnectionFactory() {
        HostName = "localhost",
        Port = 5672,
      };
      _connection = await factory.CreateConnectionAsync();
      _channel = await _connection.CreateChannelAsync();
      Console.WriteLine("[x] Initalized connection to RabbitMQ");
      return true;
    }
    catch (System.Exception ex)
    {
      Console.WriteLine(ex.ToString());
      return false;
    }
  }

  /// <summary>
  /// Appropriate for Point-to-Point type of communication
  /// </summary>
  /// <param name="message"></param>
  public async void send(string message){
    try
    {
      byte[] body = Encoding.UTF8.GetBytes(message);
      if (_channel == null) Console.WriteLine("[!] Channel connection is not setup");
      await _channel!.QueueDeclareAsync(queue: QUEUE_NAME, durable: false, exclusive: false, autoDelete: false, arguments: null);
      await _channel!.BasicPublishAsync(exchange: DEFAULT_EXCHANGE, routingKey: QUEUE_NAME, body: body);
    }
    catch (Exception ex)
    {
      Console.WriteLine(ex.ToString());
    }
  }


  /// <summary>
  /// Appropriate for Publish-Subscriber type of communcation 
  /// </summary>
  /// <param name="exchange"></param>
  /// <param name="type"></param>
  /// <param name="message"></param>
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
      Console.WriteLine(ex.ToString());
    }
  }

  /// <summary>
  /// Close the connection and all channels to the message broker
  /// </summary>
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
      Console.WriteLine(ex.ToString());
    }
  }



}