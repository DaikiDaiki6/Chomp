using System.Text;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NotificationService.Consumers.OrderEvents;
using NotificationService.Consumers.PaymentEvents;
using NotificationService.Consumers.ProductEvents;
using NotificationService.Consumers.UserEvents;
using NotificationService.Data;
using NotificationService.Middleware;
using NotificationService.Services;
using NotificationService.Services.Interface;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Appsettings config retrieval
var connectionString = builder.Configuration.GetConnectionString("ChompNotifDb") ?? throw new InvalidOperationException("Database connection string is missing");
var rabbitmq = builder.Configuration["RabbitMq:Host"] ?? throw new InvalidOperationException("RabbitMq host is missing");

// Jwt Config of Service for Program.cs
var jwtConfig = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtConfig["Key"] ?? throw new InvalidOperationException("Jwt key is missing from configuration");
var key = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtConfig["Issuer"],
            ValidAudience = jwtConfig["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });
builder.Services.AddAuthorization();

// Logging Service
builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration));

// Other Services
builder.Services.AddDbContext<NotifsDbContext>(opt =>
    opt.UseNpgsql(connectionString));
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderCancelledConsumer>();
    x.AddConsumer<OrderConfirmedConsumer>();
    x.AddConsumer<OrderPlacedConsumer>();
    x.AddConsumer<OrderUpdatedConsumer>();
    x.AddConsumer<PaymentFailedConsumer>();
    x.AddConsumer<PaymentSucceededConsumer>();
    x.AddConsumer<ProductCreatedConsumer>();
    x.AddConsumer<ProductDeletedConsumer>();
    x.AddConsumer<ProductUpdatedConsumer>();
    x.AddConsumer<UserCreatedConsumer>();
    x.AddConsumer<UserDeletedConsumer>();
    x.AddConsumer<UserUpdatedConsumer>();


    x.UsingRabbitMq((context, config) =>
    {
        config.Host(rabbitmq);

        config.ReceiveEndpoint(e =>
        {
            e.ConfigureConsumer<OrderCancelledConsumer>(context);
            e.ConfigureConsumer<OrderConfirmedConsumer>(context);
            e.ConfigureConsumer<OrderPlacedConsumer>(context);
            e.ConfigureConsumer<OrderUpdatedConsumer>(context);
            e.ConfigureConsumer<PaymentFailedConsumer>(context);
            e.ConfigureConsumer<PaymentSucceededConsumer>(context);
            e.ConfigureConsumer<ProductCreatedConsumer>(context);
            e.ConfigureConsumer<ProductDeletedConsumer>(context);
            e.ConfigureConsumer<ProductUpdatedConsumer>(context);
            e.ConfigureConsumer<UserCreatedConsumer>(context);
            e.ConfigureConsumer<UserDeletedConsumer>(context);
            e.ConfigureConsumer<UserUpdatedConsumer>(context);
        });
    }
    );
});

// Register service layer
builder.Services.AddScoped<INotificationDispatcher, NotificationDispatcher>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<NotificationService.Services.NotificationService>();


var app = builder.Build();
app.UseExceptionHandling(); // global exception handling for (mainly) controllers but also all services
app.UseAuthentication();
app.UseAuthorization();
app.UseHttpsRedirection();
app.MapControllers();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

try
{
    Log.Information("Starting up {ServiceName}", builder.Environment.ApplicationName);
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "NotificationService host terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}