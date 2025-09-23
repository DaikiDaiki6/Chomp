using System;
using Contracts;
using MassTransit;
using NotificationService.Data;
using NotificationService.Models;
using NotificationService.Services.Interface;

namespace NotificationService.Consumers.UserEvents;

public class UserCreatedConsumer: IConsumer<UserCreatedEvent>
{
  private readonly INotificationDispatcher _dispatcher;
  private readonly ILogger<UserCreatedConsumer> _logger;
  public UserCreatedConsumer(INotificationDispatcher dispatcher, ILogger<UserCreatedConsumer> logger)
  {
    _dispatcher = dispatcher;
    _logger = logger;
  }

  public async Task Consume(ConsumeContext<UserCreatedEvent> context)
  {
    var message = context.Message;

    await _dispatcher.DispatchAsync(
      message.UserId,
      "User Created",
      $"User {message.Username}(ID: {message.UserId}) has been created!",
      NotificationChannel.BellAndEmail,
      message.Email
    );
  }
}
