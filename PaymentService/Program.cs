using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PaymentService.Consumers;
using PaymentService.Data;
using Serilog;

// Configure Serilog from appsettings.json
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", true) // for Production
    .Build();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .CreateLogger();
try
{
    Log.Information("Starting PaymentService host");

    var builder = Host.CreateDefaultBuilder(args)
        .UseSerilog() // Add Serilog
    .ConfigureServices((context, services) =>
    {
        services.AddDbContext<PaymentDbContext>(opt =>
            opt.UseNpgsql(context.Configuration.GetConnectionString("ChompPaymentDb")));

        // Configure MassTransit properly
        services.AddMassTransit(x =>
        {
            x.AddConsumer<OrderConfirmedConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host("rabbitmq://localhost");

                cfg.ReceiveEndpoint("payment-service", e =>
                {
                    e.ConfigureConsumer<OrderConfirmedConsumer>(context);
                });
            });
        });
    });

    await builder.Build().RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "PaymentService host terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}