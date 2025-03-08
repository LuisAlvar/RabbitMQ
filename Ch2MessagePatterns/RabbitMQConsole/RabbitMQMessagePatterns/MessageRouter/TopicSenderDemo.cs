using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQMessagePatterns.MessageRouter;

public class TopicSenderDemo
{
  private const string TOPIC_EXCHANGE = "topic_exchange";

  private static void SendToTopicExchange()
  {
    Sender sender = new Sender();
    sender.Initialize();
    sender.SendEvent(TOPIC_EXCHANGE, "Test message 1.", "seminar.java");
    sender.SendEvent(TOPIC_EXCHANGE, "Test message 2.", "seminar.rabbitmq");
    sender.SendEvent(TOPIC_EXCHANGE, "Test message 3.", "hackaton.rabbitmq");
    sender.Destroy();
  }

  public static void Main(string[] args)
  {
    SendToTopicExchange();
  }

}

