using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQMessagePatterns.RequestReply;

/// <summary>
/// The request receiver is a subscriber to the request queue
/// </summary>
public class RequestReceiver
{
  private const string DEFAULT_QUEUE = "";
  private const string REQUEST_QUEUE = "request_queue";

  private const string HOST_NAME = "localhost";
  private const int HOST_PORT = 5672;

  private readonly ILogger<RequestReceiver> _logger;
  private readonly IConfiguration _configuration;

  private IConnection _connection;
  private IModel _channel;

  private readonly string _id;

  public RequestReceiver(string id)
  {
    _id = id;
  }

  public RequestReceiver(ILogger<RequestReceiver> logger, IConfiguration configuration)
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
        AutomaticRecoveryEnabled = true,
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
  /// After a request message is received, the request receiver retrieves the value of the replyTo property from 
  /// the message header, creates a response message, and sends it to the default exchange with a routing key that 
  /// matches the replyTo property.
  /// <br/>
  /// This means that the replyTo property points to a queue that handles response messages and the sender is 
  /// subscribed to that queue in order to received a response. 
  /// 
  /// </summary>
  public void Receive()
  {
    if (_channel == null) Initialize();
    string message = string.Empty;
    try
    {
      _channel!.QueueDeclare(queue: REQUEST_QUEUE, durable: false, exclusive: false, autoDelete: false, arguments: null);
      EventingBasicConsumer consumer = new EventingBasicConsumer(_channel);
      _channel.BasicConsume(queue: REQUEST_QUEUE, autoAck: true, consumer: consumer);

      Console.WriteLine($"[x] {_id} waiting on message from {REQUEST_QUEUE}");

      consumer.Received += (model, ea) =>
      {
        var body = ea.Body.ToArray();
        message = Encoding.UTF8.GetString(body);
        Console.WriteLine($"[x] {_id} received \"{message}\"");

        // do something with the request message ...

        var props = ea.BasicProperties;
        if (props != null)
        {
          var amqpProps = _channel.CreateBasicProperties();
          amqpProps.CorrelationId = props.CorrelationId;

          var responseMessage = Encoding.UTF8.GetBytes("Response message.");
          _channel.BasicPublish(exchange: DEFAULT_QUEUE, routingKey: props.ReplyTo, basicProperties: amqpProps, body: responseMessage);
          Console.WriteLine($"[x] {_id} sending message (Response message.) to default queue and {props.ReplyTo}");
        }
        else
        {
          Console.WriteLine("Cannot determine response destination for message.");
        }
      };
    }
    catch (IOException ex)
    {
      Console.WriteLine ($"[Error in {_id}]: {ex.Message}");
    }
    catch (OperationInterruptedException ex)
    {
      Console.WriteLine($"[Error in {_id}]: {ex.Message}");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[Error in {_id}]: {ex.Message}");
    }
  }

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
        Console.WriteLine($"[x] {_id} closed the connection.");
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine(ex.ToString());
    }
  }

}
