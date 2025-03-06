using RabbitMQSender.MessagePatterns;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQSender.Demo;

public class RequestSenderDemo
{
  private const string REQUEST_QUEUE = "request_queue";

  public static string SendToRequestReplyQueue()
  {
    Sender sender = new Sender();
    sender.Initialize();
    sender.SendRequest(REQUEST_QUEUE, "Test Message.", "MSG1");
    var result = sender.WaitForResponse("MSG1").Result.ToString();
    sender.Destroy();
    return result;
  }

  public static void Main(string[] args)
  {
    string response = SendToRequestReplyQueue();
    Console.WriteLine($"[S-->] received response: {response}");
  }
}
