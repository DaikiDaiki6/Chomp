using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PaymentService.Consumers;
using PaymentService.Data;

var builder = Host.CreateDefaultBuilder(args)
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