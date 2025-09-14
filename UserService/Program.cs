using System.Text;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Tokens.Experimental;
using Serilog;
using UserService.Data;
using UserService.Middleware;
using UserService.Services;
using UserService.Services.Helpers;
using UserService.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Appsettings config retrieval
var connectionString = builder.Configuration.GetConnectionString("ChompUsersDb") ?? throw new InvalidOperationException("Database connection string is missing");
var rabbitMqHost = builder.Configuration["RabbitMq:Host"] ?? throw new InvalidOperationException("RabbitMq host is missing");

// JWT Config of Service for Program.cs
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
builder.Services.AddDbContext<UsersDbContext>(opt =>
    opt.UseNpgsql(connectionString));
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, config) =>
    {
        config.Host(rabbitMqHost);
    });
});

// Register service layer
builder.Services.AddScoped<IUserService, UserService.Services.UserService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<GetPrincipalFromExpiredToken>();
builder.Services.AddHostedService<UserCleanupService>();

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
