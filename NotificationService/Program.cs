using MassTransit;
using Microsoft.Extensions.Hosting;
using NotificationService.Consumers;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddMassTransit(x =>
        {
            x.AddConsumer<UserCreatedConsumer>();

            x.UsingRabbitMq((context, config) =>
            {
                config.Host("rabbitmq://localhost");

                config.ReceiveEndpoint("notification-service", e =>
                {
                    e.ConfigureConsumer<UserCreatedConsumer>(context);
                });
            });
        }); 
    });

await builder.RunConsoleAsync();