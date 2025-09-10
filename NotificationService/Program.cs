using MassTransit;
using Microsoft.Extensions.Hosting;
using NotificationService.Consumers;
using NotificationService.Consumers.UserEvents;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddMassTransit(x =>
        {
            x.AddConsumer<UserCreatedConsumer>();
            x.AddConsumer<UserUpdatedConsumer>();
            x.AddConsumer<UserDeletedConsumer>();
            x.UsingRabbitMq((context, config) =>
            {
                config.Host("rabbitmq://localhost");

                config.ReceiveEndpoint("notification-service", e =>
                {
                    e.ConfigureConsumer<UserCreatedConsumer>(context);
                    e.ConfigureConsumer<UserUpdatedConsumer>(context);
                    e.ConfigureConsumer<UserDeletedConsumer>(context);
                });
            });
        }); 
    });

await builder.RunConsoleAsync();