using RabbitMQMessagePatterns.MessageRouter;
using RabbitMQMessagePatterns.PointToPoint;
using RabbitMQMessagePatterns.PublishSubscribe;
using RabbitMQMessagePatterns.RequestReply;

Console.WriteLine("RabbitMQConsole .... ");

#region PointToPoint Communication 
//Console.Write("Waiting for sender.... (main app thread sleeping) ... ");
//Task.Delay(2000).Wait();
//Console.WriteLine("Done");
//await CompetingReceiverDemo.MainFCFS(args);
#endregion

#region PublishSubscribe Communication
//PublishSubscribeReceiverDemo.Main(args);
#endregion


#region RequestReply Communication
//RequestReceiverDemo.Main(args);
#endregion

#region MessageRouter Communication 
TopicReceiverDemo.Main(args);
#endregion

Console.Write("Enter any key to exist. ");
Console.ReadKey();