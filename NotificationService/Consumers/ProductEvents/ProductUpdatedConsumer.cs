using System;
using Contracts;
using MassTransit;
using NotificationService.Models;
using NotificationService.Services.Interface;

namespace NotificationService.Consumers.ProductEvents;

public class ProductUpdatedConsumer: IConsumer<ProductUpdatedEvent>
{
  private readonly INotificationDispatcher _dispatcher;
  private readonly ILogger<ProductUpdatedConsumer> _logger;
  public ProductUpdatedConsumer(INotificationDispatcher dispatcher, ILogger<ProductUpdatedConsumer> logger)
  {
    _dispatcher = dispatcher;
    _logger = logger;
  }

  public async Task Consume(ConsumeContext<ProductUpdatedEvent> context)
  {
    var message = context.Message;

    await _dispatcher.DispatchAsync(
      message.AddedBy,
      "Product Updated",
      $"Product {message.ProductName}(ID: {message.ProductId}) has been updated!",
      NotificationChannel.Bell,
      null
    );
  }
}
