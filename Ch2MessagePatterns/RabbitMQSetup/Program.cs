// See https://aka.ms/new-console-template for more information
using RabbitMQSetup;
Console.WriteLine("RabbitMQSetup");
DefaultExchangeSenderDemo.Main(args);
Console.Write("Wait.... (main app thread sleeping) ... ");
Task.Delay(2000).Wait();
Console.WriteLine("Done");
await CompetingReceiverDemo.MainFCFS(args);

