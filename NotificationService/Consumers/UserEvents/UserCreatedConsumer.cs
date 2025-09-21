using System;
using Contracts;
using MassTransit;

namespace NotificationService.Consumers.UserEvents;

public class UserCreatedConsumer: IConsumer<UserCreatedEvent>
{
  public async Task Consume(ConsumeContext<UserCreatedEvent> context)
  {
    await Task.CompletedTask;
  }
}
