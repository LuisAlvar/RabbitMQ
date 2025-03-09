
namespace RabbitMQMessagePatterns.PublishSubscribe;

/// <summary>
/// Use PublishSubscribeReceiver class for the establishment of a publish-subscribe channel
/// </summary>
public class PublishSubscribeReceiverDemo
{

  /// <summary>
  /// Creates two receivers that bind to two different queus: pubsub_queue1 and pubsub_queue2. 
  /// Sender will sent a message to the pubsub_exchange, it will be delievered to both consumers.
  /// </summary>
  public static void RunParallelTasks()
  {
    PublishSubscribeReceiver receiver1 = new PublishSubscribeReceiver("Receiver1");
    PublishSubscribeReceiver receiver2 = new PublishSubscribeReceiver("Receiver2");

    try
    {
      receiver1.Initialize();
      receiver2.Initialize();

      var task1 = Task.Run(() => receiver1.Receive("pubsub_queue1"));
      var task2 = Task.Run(() => receiver2.Receive("pubsub_queue2"));

      Task.WaitAll(task1, task2);
    }
    catch (AggregateException ex)
    {
      foreach (var item in ex.InnerExceptions)
      {
        Console.WriteLine($"[Error in RunParallelTasks]: {item.Message}");
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[Error in RunParallelTasks]: {ex.Message}");
    }
    finally
    {
      receiver1.Destroy();
      receiver2.Destroy();
    }
  }


  public static void Main(string[] args)
  {
    RunParallelTasks();
    Console.WriteLine("Both pub-sub receivers have completed.");
  }
}

