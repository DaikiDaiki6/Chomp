using System;
using Contracts;
using MassTransit;

namespace NotificationService.Consumers.OrderEvents;

public class OrderUpdatedConsumer: IConsumer<OrderUpdatedEvent>
{
  public async Task Consume(ConsumeContext<OrderUpdatedEvent> context)
  {
    await Task.CompletedTask;
  }
}
