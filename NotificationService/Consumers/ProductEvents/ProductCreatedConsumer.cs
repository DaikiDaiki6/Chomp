using System;
using Contracts;
using MassTransit;

namespace NotificationService.Consumers.ProductEvents;

public class ProductCreatedConsumer: IConsumer<ProductCreatedEvent>
{
  public async Task Consume(ConsumeContext<ProductCreatedEvent> context)
  {
    await Task.CompletedTask;
  }
}
