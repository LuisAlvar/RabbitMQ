using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQMessagePatterns.MessageRouter
{
  public class TopicReceiverDemo
  {
    public static void Main(string[] args)
    {
      TopicReceiver SeminarReceiver = new TopicReceiver("SeminarReceiver");
      TopicReceiver HackatonReceiver = new TopicReceiver("HackatonReceiver");

      SeminarReceiver.Initialize();
      HackatonReceiver.Initialize();

      SeminarReceiver.Receive("seminar_queue");
      HackatonReceiver.Receive("hackaton_queue");

      SeminarReceiver.Destroy();
      HackatonReceiver.Destroy();
    }
  }
}
