using System;
using Contracts;
using MassTransit;

namespace NotificationService.Consumers.ProductEvents;

public class ProductUpdatedConsumer: IConsumer<ProductUpdatedEvent>
{
  public async Task Consume(ConsumeContext<ProductUpdatedEvent> context)
  {
    await Task.CompletedTask;
  }
}
