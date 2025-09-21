using System;
using Contracts;
using MassTransit;

namespace NotificationService.Consumers.ProductEvents;

public class ProductDeletedConsumer: IConsumer<ProductDeletedEvent>
{
  public async Task Consume(ConsumeContext<ProductDeletedEvent> context)
  {
    await Task.CompletedTask;
  }
}
