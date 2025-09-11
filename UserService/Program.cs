using MassTransit;
using Microsoft.EntityFrameworkCore;
using Serilog;
using UserService.Data;
using UserService.Middleware;

var builder = WebApplication.CreateBuilder(args);
// Logging
builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration));
// Services
builder.Services.AddDbContext<UsersDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("ChompUsersDb")));
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMassTransit( x =>
{
    x.UsingRabbitMq((context, config) =>
    {
        config.Host("rabbitmq://localhost");
    });
}
);

var app = builder.Build();
// global exception handling for (mainly) controllers but also all services
app.UseExceptionHandling(); 
app.UseHttpsRedirection();
app.UseAuthorization();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.MapControllers();

try
{
    Log.Information("Starting up {ServiceName}", builder.Environment.ApplicationName);
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application start up failed");
}
finally
{
    Log.CloseAndFlush();
}
