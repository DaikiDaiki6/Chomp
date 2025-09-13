using System.Text;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using UserService.Data;
using UserService.Middleware;
using UserService.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Authentication config
var jwtConfig = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtConfig["Key"] ?? string.Empty;
if (string.IsNullOrEmpty(jwtKey))
{
    Log.Warning("UserService - Jwt Key is missing from configuration");
    throw new InvalidOperationException("Jwt Key is missing from configuration");
}
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

// Register background services
builder.Services.AddHostedService<UserService.Services.UserCleanupService>();

var app = builder.Build();
app.UseExceptionHandling(); // Global Exception handling - used mainly for controller but can be used globally
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.Run(); Try-Catch-Finally
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
