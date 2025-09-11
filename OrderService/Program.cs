using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderService.Consumers;
using OrderService.Data;
using OrderService.Middleware;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
// Logging
builder.Host.UseSerilog((context, config) => 
    config.ReadFrom.Configuration(context.Configuration));
// Services
builder.Services.AddDbContext<OrdersDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("ChompOrdersDb")));
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<PaymentFailedConsumer>();
    x.AddConsumer<PaymentSucceededConsumer>();

    x.UsingRabbitMq((context, config) =>
    {
        config.Host("rabbitmq://localhost");

        config.ReceiveEndpoint(e =>
        {
            e.ConfigureConsumer<PaymentFailedConsumer>(context);
            e.ConfigureConsumer<PaymentSucceededConsumer>(context);
        });
    }
    );
});

var app = builder.Build();
// global exception handling for (mainly) controllers but also all services
app.UseExceptionHandling();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();


