using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using RabbitMQMessagePatterns.RequestReply;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQMessagePatterns.MessageRouter
{
  public class TopicReceiver
  {
    private const string TOPIC_EXCHANGE = "topic_exchange";

    private const string HOST_NAME = "localhost";
    private const int HOST_PORT = 5672;

    private readonly ILogger<TopicReceiver> _logger;
    private readonly IConfiguration _configuration;

    private IConnection _connection;
    private IModel _channel;

    private readonly string _id;

    public TopicReceiver(string id)
    {
      _id = id;
    }

    public TopicReceiver(ILogger<TopicReceiver> logger, IConfiguration configuration)
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
    /// Use to consume the message of the corresponding queue
    /// </summary>
    /// <param name="queue"></param>
    /// <returns></returns>
    public string Receive(string queue)
    {
      if (_channel == null) Initialize();
      string message = string.Empty;

      try
      {
        EventingBasicConsumer consumer = new EventingBasicConsumer(_channel);

        _channel.BasicConsume(queue: queue, autoAck: true, consumer: consumer);

        consumer.Received += (model, ea) =>
        {
          var body = ea.Body.ToArray();
          message = Encoding.UTF8.GetString(body);
          Console.WriteLine($"[R<--] {_id} received \"{message}\" from queue [{queue}]");
        };

      }
      catch (Exception ex)
      {
        Console.WriteLine($"[Error in ]: {ex.Message}");
      }

      Console.WriteLine($"[R<--] {_id} waiting on message from {queue}");
      Task.Delay(4000).Wait();

      return message;
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
}
