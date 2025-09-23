using System;
using Contracts;
using MassTransit;
using NotificationService.Models;
using NotificationService.Services.Interface;

namespace NotificationService.Consumers.OrderEvents;

public class OrderPlacedConsumer: IConsumer<OrderPlacedEvent>
{
  private readonly INotificationDispatcher _dispatcher;
  private readonly ILogger<OrderPlacedConsumer> _logger;
  public OrderPlacedConsumer(INotificationDispatcher dispatcher, ILogger<OrderPlacedConsumer> logger)
  {
    _dispatcher = dispatcher;
    _logger = logger;
  }

  public async Task Consume(ConsumeContext<OrderPlacedEvent> context)
  {
    var message = context.Message;

    await _dispatcher.DispatchAsync(
      message.CustomerId,
      "Order Placed",
      $"Your order {message.OrderId} has been placed!",
      NotificationChannel.Bell,
      null
    );
  }
}