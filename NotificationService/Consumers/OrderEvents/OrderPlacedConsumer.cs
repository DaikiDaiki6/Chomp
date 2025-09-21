using System;
using Contracts;
using MassTransit;

namespace NotificationService.Consumers.OrderEvents;

public class OrderPlaceConsumer: IConsumer<OrderPlacedEvent>
{
  public async Task Consume(ConsumeContext<OrderPlacedEvent> context)
  {
    await Task.CompletedTask;
  }
}