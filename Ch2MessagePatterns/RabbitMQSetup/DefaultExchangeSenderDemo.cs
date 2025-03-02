using RabbitMQ;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQSetup
{
  public class DefaultExchangeSenderDemo
  {
    public static void sendToDefaultExchange()
    {
      try
      {
        Sender sender = new Sender();
        sender.Initialize();
        sender.Send("Test message");
        sender.Destroy();
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.ToString());
      }

    }

    public static void Main(string[] args)
    {
      sendToDefaultExchange();
    }

  }
}
