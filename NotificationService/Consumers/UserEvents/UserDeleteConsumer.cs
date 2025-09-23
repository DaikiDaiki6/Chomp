using System;
using Contracts;
using MassTransit;
using NotificationService.Models;
using NotificationService.Services.Interface;

namespace NotificationService.Consumers.UserEvents;

public class UserDeletedConsumer: IConsumer<UserDeletedEvent>
{
  private readonly INotificationDispatcher _dispatcher;
  private readonly ILogger<UserDeletedConsumer> _logger;
  public UserDeletedConsumer(INotificationDispatcher dispatcher, ILogger<UserDeletedConsumer> logger)
  {
    _dispatcher = dispatcher;
    _logger = logger;
  }

  public async Task Consume(ConsumeContext<UserDeletedEvent> context)
  {
    var message = context.Message;

    await _dispatcher.DispatchAsync(
      message.UserId,
      "User Deleted",
      $"User {message.Username}(ID: {message.UserId}) has been deleted!",
      NotificationChannel.BellAndEmail,
      message.Email
    );
  }
}
