using System;
using Contracts;
using MassTransit;

namespace NotificationService.Consumers.OrderEvents;

public class OrderConfirmedConsumer: IConsumer<OrderConfirmedEvent>
{
  public async Task Consume(ConsumeContext<OrderConfirmedEvent> context)
  {
    await Task.CompletedTask;
  }
}
