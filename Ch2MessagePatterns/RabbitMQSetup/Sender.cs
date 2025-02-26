using RabbitMQ.Client;
using System.Text;

public class Sender {
  private readonly static string QUEUE_NAME = "event_queue";
  private readonly static string DEFAULT_EXCHANGE = "";

  private IConnection? _connection;
  private IChannel? _channel;

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
    }
    catch (System.Exception ex)
    {
      throw ex;
    }
  }

}