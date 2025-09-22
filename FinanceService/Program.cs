using System.Text;
using Contracts;
using FinanceService.Consumers;
using FinanceService.Data;
using FinanceService.Middleware;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Appsettings config retrieval
var connectionString = builder.Configuration.GetConnectionString("ChompFinanceDb") ?? throw new InvalidOperationException("Database connection string is missing");
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
builder.Services.AddDbContext<FinanceDbContext>(opt =>
    opt.UseNpgsql(connectionString));
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderConfirmedConsumer>();

    x.UsingRabbitMq((context, config) =>
    {
        config.Host(rabbitmq);

        config.ReceiveEndpoint(e =>
        {
            e.ConfigureConsumer<OrderConfirmedConsumer>(context);
        });
    }
    );
});

// Register service layer

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
    Log.Fatal(ex, "FinanceService host terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
