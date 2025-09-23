using System;
using Contracts;
using MassTransit;
using NotificationService.Models;
using NotificationService.Services.Interface;

namespace NotificationService.Consumers.ProductEvents;

public class ProductCreatedConsumer: IConsumer<ProductCreatedEvent>
{
  private readonly INotificationDispatcher _dispatcher;
  private readonly ILogger<ProductCreatedConsumer> _logger;
  public ProductCreatedConsumer(INotificationDispatcher dispatcher, ILogger<ProductCreatedConsumer> logger)
  {
    _dispatcher = dispatcher;
    _logger = logger;
  }

  public async Task Consume(ConsumeContext<ProductCreatedEvent> context)
  {
    var message = context.Message;

    await _dispatcher.DispatchAsync(
      message.AddedBy,
      "Product Created",
      $"Product {message.ProductName}(ID: {message.ProductId} Price: {message.Price} Stock: {message.Stock}) has been created!",
      NotificationChannel.Bell,
      null
    );
  }
}
