using RabbitMQMessagePatterns.PointToPoint;
using RabbitMQMessagePatterns.PublishSubscribe;
using RabbitMQMessagePatterns.RequestReply;

Console.WriteLine("RabbitMQConsole .... ");

#region Point-to-Point 
Console.Write("Waiting for sender.... (main app thread sleeping) ... ");
Task.Delay(2000).Wait();
Console.WriteLine("Done");
await CompetingReceiverDemo.MainFCFS(args);
#endregion

#region pub-sub communication
//FanoutExchangeSenderDemo.Main(args);
//Console.Write("Wait.... (main app thread sleeping) ... ");
//Task.Delay(2000).Wait();
//Console.WriteLine("Done");
//PublishSubscribeReceiverDemo.Main(args);
#endregion


#region RequestReply
//RequestReceiverDemo.Main(args);
#endregion

Console.Write("Enter any key to exist. ");
Console.ReadKey();