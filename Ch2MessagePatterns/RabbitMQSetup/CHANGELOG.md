# RabbitMQSetup


# Dependency of RabbitMQ Docker Container Running 
- Run command `docker network create mynetwork`
- Run command `docker run -d --hostname my-rabbit --name rabbitmq-container --network mynetwork -p 5672:5672 -p 15672:15672 rabbitmq:3-management`

# Project 
- Create of project `dotnet new console -o RabbitMQSetup -f net6.0`
- Add Nuget packages `Microsoft.Extensions.Configuration.Abstractions`
- Add Nuget packages `Microsoft.Extensions.Configuration.Json`
- Add Nuget packages `Microsoft.Extensions.Logging.Abstractions`
- Add Nuget package `RabbitMQ.Client --version 6.2.2`
- Add Nuget package `Polly`

- Create `appsettings.json`
- Create `Sender.cs` 
- Create `CompetingReceiver.cs`
- Create `DefaultExchangeSenderDemo.cs`

