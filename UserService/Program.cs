using MassTransit;
using Microsoft.EntityFrameworkCore;
using Serilog;
using UserService.Data;
using UserService.Middleware;
using UserService.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);
// Logging Service
builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration));
// Other Services
builder.Services.AddDbContext<UsersDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("ChompUsersDb")));
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, config) =>
    {
        config.Host("rabbitmq://localhost");
    });
});
// Register service layer
builder.Services.AddScoped<IUserService, UserService.Services.UserService>();

var app = builder.Build();
// Global Exception handling - used mainly for controller but can be used globally
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
    Log.Fatal(ex, "UserService host terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
