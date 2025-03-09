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
  private const string DEFAULT_EXCHANGE = "";
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
  /// After a request message is received, the request receiver retrieves the value of the replyTo property from 
  /// the message header, creates a response message, and sends it to the default exchange with a routing key that 
  /// matches the replyTo property.
  /// <br/>
  /// This means that the replyTo property points to a queue that handles response messages and the sender is 
  /// subscribed to that queue in order to received a response. 
  /// 
  /// </summary>
  public async Task<bool> Receive()
  {
    if (_channel == null) Initialize();
    string message = string.Empty;
    TaskCompletionSource<bool> taskCompletion = new TaskCompletionSource<bool>();

    try
    {
      //_channel!.QueueDeclare(queue: REQUEST_QUEUE, durable: false, exclusive: false, autoDelete: false, arguments: null);
      
      EventingBasicConsumer consumer = new EventingBasicConsumer(_channel);

      // Sender may be setting up so we wait 
      Console.WriteLine($"[R<--] {_id} waiting on message from {REQUEST_QUEUE}");
      Task.Delay(4000).Wait();

      _channel.BasicConsume(queue: REQUEST_QUEUE, autoAck: true, consumer: consumer);

      consumer.Received += (model, ea) =>
      {
        var body = ea.Body.ToArray();
        message = Encoding.UTF8.GetString(body);
        Console.WriteLine($"[R<--] {_id} received \"{message}\" from queue [{REQUEST_QUEUE}]");

        // do something with the request message ...

        var props = ea.BasicProperties;
        if (props != null)
        {
          var amqpProps = _channel!.CreateBasicProperties();
          amqpProps.CorrelationId = props.CorrelationId;

          var responseMessage = Encoding.UTF8.GetBytes("Response message from RequestReceiver");
          _channel.BasicPublish(exchange: DEFAULT_EXCHANGE, routingKey: props.ReplyTo, basicProperties: amqpProps, body: responseMessage);
          Console.WriteLine($"[R<--] {_id} sending message to queue {props.ReplyTo}");
          taskCompletion.SetResult(true);
        }
        else
        {
          Console.WriteLine("Cannot determine response destination for message.");
        }
      };

      // Sender may be setting up so we wait 
      Console.WriteLine($"[R<--] {_id} waiting on sender to receive response message");
      Task.Delay(4000).Wait();

      return await taskCompletion.Task;
    }
    catch (IOException ex)
    {
      Console.WriteLine ($"[Error in {_id}]: {ex.Message}");
      return await Task.FromResult(false);
    }
    catch (OperationInterruptedException ex)
    {
      Console.WriteLine($"[Error in {_id}]: {ex.Message}");
      return await Task.FromResult(false);
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[Error in {_id}]: {ex.Message}");
      return await Task.FromResult(false);
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
