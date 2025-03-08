

namespace RabbitMQMessagePatterns.RequestReply;
public class RequestReceiverDemo
{
  public static void Main(string[] args)
  {
    RequestReceiver receiver = new RequestReceiver("RequestReceiver");
    receiver.Initialize();
    receiver.Receive();
    Console.WriteLine("Enter any key to quit");
    Console.ReadKey();
    receiver.Destroy();
  }
}
