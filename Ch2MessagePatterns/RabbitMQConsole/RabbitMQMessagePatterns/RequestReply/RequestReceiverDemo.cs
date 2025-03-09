

namespace RabbitMQMessagePatterns.RequestReply;
public class RequestReceiverDemo
{
  public async static void Main(string[] args)
  {
    RequestReceiver receiver = new RequestReceiver("RequestReceiver");
    receiver.Initialize();
    await receiver.Receive();
    receiver.Destroy();
  }
}
