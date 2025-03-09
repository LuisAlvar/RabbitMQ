
namespace RabbitMQMessagePatterns.PublishSubscribe;

/// <summary>
/// Used to send a message to the pubsub_exchange fanout exchange
/// </summary>
public class FanoutExchangeSenderDemo
{
  private const string FANOUT_EXCHANGE_TYPE = "fanout";

  public static void SendToFanoutExchange(String exchange)
  {
    Sender sender = new Sender();
    sender.Initialize();
    sender.Send(exchange: exchange, type: FANOUT_EXCHANGE_TYPE, message: "Test message from fanout exchange");
    sender.Destroy();
  }

  public static void Main(string[] args)
  {
    SendToFanoutExchange("pubsub_exchange");
  }
}

