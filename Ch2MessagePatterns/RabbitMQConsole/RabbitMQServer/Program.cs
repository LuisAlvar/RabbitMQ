using RabbitMQMessagePatterns.MessageRouter;
using RabbitMQMessagePatterns.PointToPoint;
using RabbitMQMessagePatterns.PublishSubscribe;
using RabbitMQMessagePatterns.RequestReply;

Console.WriteLine("RabbitMQServer (i.e., Sender) .... ");

#region PointToPoint Communication 
//DefaultExchangeSenderDemo.Main(args);
#endregion 

#region PublishSubscribe Communication
//FanoutExchangeSenderDemo.Main(args);
#endregion

#region RequestReply Communication
//RequestSenderDemo.Main(args);
#endregion

#region MessageRouter Communication
TopicSenderDemo.Main(args);
#endregion

Console.Write("Enter any key to exist. ");
Console.ReadKey();