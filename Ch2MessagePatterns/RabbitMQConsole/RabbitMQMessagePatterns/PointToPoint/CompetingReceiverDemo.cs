
namespace RabbitMQMessagePatterns.PointToPoint;

/// <summary>
/// Used to consume a single unprocessed message on a particular queue in a Point-to-Point type channel.
/// </summary>
public class CompetingReceiverDemo
{

  /// <summary>
  /// We create two instances and invoke the receive of the two instance in separate threads
  /// so that we have two subscribers for the same queue waiting for a message.
  /// </summary>
  /// <returns></returns>
  public static async Task RunParallelTasks()
  {
    /*
      DefaultExchangeSenderDemo.Main(args);
      Console.Write("Wait.... (main app thread sleeping) ... ");
      Task.Delay(2000).Wait();
      Console.WriteLine("Done");
      await CompetingReceiverDemo.MainFCFS(args);
     */
    CompetingReceiver receiver1 = new CompetingReceiver("Receiver1");
    CompetingReceiver receiver2 = new CompetingReceiver("Receiver2");

    try
    {
      receiver1.Initialize();
      receiver2.Initialize();

      var cts = new CancellationTokenSource();

      var task1 = Task.Run(() => receiver1.Receive(cts.Token));
      var task2 = Task.Run(() => receiver2.Receive(cts.Token));

      var completedTask = await Task.WhenAny(task1, task2);
      cts.Cancel();
      var response = await completedTask;
      Console.WriteLine($"[R<--] response from queue to process: {response}");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[Error in RunParallelTasks]: {ex.Message}");
    }
    finally
    {
      Console.WriteLine("[R<--] Destorying Receivers....");
      receiver1.Destory();
      receiver2.Destory();
    }
  }

  public static async Task MainFCFS(string[] args)
  {
    await RunParallelTasks();
    Console.WriteLine("[R<--] Both competing receivers have completed.");
  }

}
