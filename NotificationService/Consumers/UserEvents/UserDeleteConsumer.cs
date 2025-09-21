using System;
using Contracts;
using MassTransit;

namespace NotificationService.Consumers.UserEvents;

public class UserDeletedConsumer: IConsumer<UserDeletedEvent>
{
  public async Task Consume(ConsumeContext<UserDeletedEvent> context)
  {
    await Task.CompletedTask;
  }
}
