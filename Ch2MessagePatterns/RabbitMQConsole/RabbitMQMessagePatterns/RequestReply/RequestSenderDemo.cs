

namespace RabbitMQMessagePatterns.RequestReply;

public class RequestSenderDemo
{
  private const string REQUEST_QUEUE = "request_queue";

  public static async Task<string> SendToRequestReplyQueueAsync()
  {
    Sender sender = new Sender();
    sender.Initialize();
    sender.SendRequest(REQUEST_QUEUE, "Test Message.", "MSG1");
    var result = await sender.WaitForResponse("MSG1");
    sender.Destroy();
    return result;
  }

  public static async void Main(string[] args)
  {
    string response = await SendToRequestReplyQueueAsync();
    Console.WriteLine($"[S<--] received response: {response}");
  }
}
