
namespace RabbitMQMessagePatterns.PointToPoint;

 /// <summary>
 /// Demonstreate the usage of the CompetingConsumer class in a point-to-point channel
 /// <br/>
 /// Use this class to send a message to the default exchange
 /// </summary>
public class DefaultExchangeSenderDemo
{
  private static void SendToDefaultExchange()
  {
    try
    {
      Sender sender = new Sender();
      sender.Initialize();
      sender.Send("Test message: Default Exchange example to Event_Queue");
      sender.Destroy();
    }
    catch (Exception ex)
    {
      Console.WriteLine(ex.ToString());
    }

  }

  /// <summary>
  /// A demonstation of sending data to a queue and viewing it within RabbitMQ UI
  /// </summary>
  /// <param name="args"></param>
  public static void Main(string[] args)
  {
    SendToDefaultExchange();
  }

}
