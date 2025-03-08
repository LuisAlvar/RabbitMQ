
namespace RabbitMQMessagePatterns.PublishSubscribe;

public class FanoutExchangeSenderDemo
{
  private const string FANOUT_EXCHANGE_TYPE = "fanout";

  public static void sendToFanoutExchange(String exchange)
  {
    Sender sender = new Sender();
    sender.Initialize();
    sender.Send(exchange: exchange, type: FANOUT_EXCHANGE_TYPE, message: "Test message from Fanout");
    sender.Destroy();
  }

  public static void Main(string[] args)
  {
    sendToFanoutExchange("pubsub_exchange");
  }
}

