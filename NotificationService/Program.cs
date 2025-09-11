using MassTransit;
using Microsoft.Extensions.Hosting;
using NotificationService.Consumers;
using NotificationService.Consumers.OrderEvents;
using NotificationService.Consumers.ProductEvents;
using NotificationService.Consumers.UserEvents;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddMassTransit(x =>
        {
            // user event consumers
            x.AddConsumer<UserCreatedConsumer>();
            x.AddConsumer<UserUpdatedConsumer>();
            x.AddConsumer<UserDeletedConsumer>();

            // product event consumers
            x.AddConsumer<ProductCreatedConsumer>();
            x.AddConsumer<ProductUpdatedConsumer>();
            x.AddConsumer<ProductDeletedConsumer>();

            // order event consumers
            x.AddConsumer<OrderPlacedConsumer>();
            x.AddConsumer<OrderUpdatedConsumer>();
            x.AddConsumer<OrderCancelledConsumer>();

            x.UsingRabbitMq((context, config) =>
            {
                config.Host("rabbitmq://localhost");

                config.ReceiveEndpoint("notification-service", e =>
                {
                    // user event consumers
                    e.ConfigureConsumer<UserCreatedConsumer>(context);
                    e.ConfigureConsumer<UserUpdatedConsumer>(context);
                    e.ConfigureConsumer<UserDeletedConsumer>(context);

                    // product event consumers
                    e.ConfigureConsumer<ProductCreatedConsumer>(context);
                    e.ConfigureConsumer<ProductUpdatedConsumer>(context);
                    e.ConfigureConsumer<ProductDeletedConsumer>(context);

                    // order event consumers
                    e.ConfigureConsumer<OrderPlacedConsumer>(context);
                    e.ConfigureConsumer<OrderUpdatedConsumer>(context);
                    e.ConfigureConsumer<OrderCancelledConsumer>(context);
                });
            });
        }); 
    });

await builder.RunConsoleAsync();