// See https://aka.ms/new-console-template for more information
using RabbitMQSetup.Demo;
Console.WriteLine("RabbitMQSetup");

#region Point-to-Point 
//DefaultExchangeSenderDemo.Main(args);
//Console.Write("Wait.... (main app thread sleeping) ... ");
//Task.Delay(2000).Wait();
//Console.WriteLine("Done");
//await CompetingReceiverDemo.MainFCFS(args);
#endregion

#region pub-sub communication
//FanoutExchangeSenderDemo.Main(args);
//Console.Write("Wait.... (main app thread sleeping) ... ");
//Task.Delay(2000).Wait();
//Console.WriteLine("Done");
//PublishSubscribeReceiverDemo.Main(args);
#endregion

RequestReceiverDemo.Main(args);


