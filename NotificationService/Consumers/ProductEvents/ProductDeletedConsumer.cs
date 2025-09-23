using System;
using Contracts;
using MassTransit;
using NotificationService.Models;
using NotificationService.Services.Interface;

namespace NotificationService.Consumers.ProductEvents;

public class ProductDeletedConsumer: IConsumer<ProductDeletedEvent>
{
  private readonly INotificationDispatcher _dispatcher;
  private readonly ILogger<ProductDeletedConsumer> _logger;
  public ProductDeletedConsumer(INotificationDispatcher dispatcher, ILogger<ProductDeletedConsumer> logger)
  {
    _dispatcher = dispatcher;
    _logger = logger;
  }

  public async Task Consume(ConsumeContext<ProductDeletedEvent> context)
  {
    var message = context.Message;

    await _dispatcher.DispatchAsync(
      message.AddedBy,
      "Product Deleted",
      $"Product {message.ProductName}(ID: {message.ProductId}) has been deleted!",
      NotificationChannel.Bell,
      null
    );
  }
}
