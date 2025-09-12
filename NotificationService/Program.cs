using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NotificationService.Consumers;
using NotificationService.Consumers.OrderEvents;
using NotificationService.Consumers.PaymentEvents;
using NotificationService.Consumers.ProductEvents;
using NotificationService.Consumers.UserEvents;
using Serilog;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", true)
    .Build();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .CreateLogger();
try
{
    var builder = Host.CreateDefaultBuilder(args)
        .UseSerilog()
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

                // payment event consumers
                x.AddConsumer<PaymentFailedConsumer>();
                x.AddConsumer<PaymentSucceededConsumer>();

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

                        // payment event consumers
                        e.ConfigureConsumer<PaymentFailedConsumer>(context);
                        e.ConfigureConsumer<PaymentSucceededConsumer>(context);
                    });
                });
            });
        });
    await builder.Build().RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "NotificationService host terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
