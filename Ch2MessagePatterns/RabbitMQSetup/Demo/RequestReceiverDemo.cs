using RabbitMQSetup.MessagePatterns;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQSetup.Demo;
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
