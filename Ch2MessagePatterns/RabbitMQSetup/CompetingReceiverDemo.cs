using System;
using System.Threading.Tasks;

namespace RabbitMQSetup
{
  /// <summary>
  /// Point-to-Point type of communcation
  /// </summary>
  public class CompetingReceiverDemo
  {
    
    public static async Task RunParallelTasks()
    {
      /*
        // Under the assumptation that there is only a single message within the queue
        // where two thread are competing for this message. 
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
        Console.WriteLine(await completedTask);
      }
      catch (Exception ex)
      {
        Console.WriteLine($"[Error in RunParallelTasks]: {ex.Message}");
      }
      finally
      {
        Console.WriteLine("Destorying Receivers....");
        receiver1.Destory();
        receiver2.Destory();
      }
    }

    public static async Task MainFCFS(string[] args)
    {
      await RunParallelTasks();
      Console.WriteLine("Both competing receivers have completed.");
    }

  }
}
