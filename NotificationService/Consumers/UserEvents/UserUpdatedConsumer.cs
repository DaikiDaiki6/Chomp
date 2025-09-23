using System;
using Contracts;
using MassTransit;
using NotificationService.Models;
using NotificationService.Services.Interface;

namespace NotificationService.Consumers.UserEvents;

public class UserUpdatedConsumer: IConsumer<UserUpdatedEvent>
{
  private readonly INotificationDispatcher _dispatcher;
  private readonly ILogger<UserUpdatedConsumer> _logger;
  public UserUpdatedConsumer(INotificationDispatcher dispatcher, ILogger<UserUpdatedConsumer> logger)
  {
    _dispatcher = dispatcher;
    _logger = logger;
  }

  public async Task Consume(ConsumeContext<UserUpdatedEvent> context)
  {
    var message = context.Message;

    await _dispatcher.DispatchAsync(
      message.UserId,
      "User Updated",
      $"User {message.Username}(ID: {message.UserId}) has been updated!",
      NotificationChannel.BellAndEmail,
      message.Email
    );
  }
}
