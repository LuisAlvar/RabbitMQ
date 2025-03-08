using RabbitMQMessagePatterns.PointToPoint;
using RabbitMQMessagePatterns.RequestReply;

Console.WriteLine("RabbitMQServer .... ");

#region PointToPoint Communication 
DefaultExchangeSenderDemo.Main(args);
#endregion 


#region RequestReply Communication
//Task.Delay(4000).Wait(); 
//RequestSenderDemo.Main(args);
#endregion

Console.Write("Enter any key to exist. ");
Console.ReadKey();