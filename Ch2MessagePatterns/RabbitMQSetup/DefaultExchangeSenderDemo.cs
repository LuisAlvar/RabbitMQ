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
    public static async void sendToDefaultExchange()
    {
      Sender sender = new Sender();
      await sender.initialize();
      sender.send("Test message");
      sender.destroy();
    }

    public static void main(string[] args)
    {
      sendToDefaultExchange();
    }

  }
}
