using System;
using Contracts;
using MassTransit;
using NotificationService.Models;
using NotificationService.Services.Interface;

namespace NotificationService.Consumers.OrderEvents;

public class OrderCancelledConsumer : IConsumer<OrderCancelledEvent>
{
  private readonly INotificationDispatcher _dispatcher;
  private readonly ILogger<OrderCancelledConsumer> _logger;
  public OrderCancelledConsumer(INotificationDispatcher dispatcher, ILogger<OrderCancelledConsumer> logger)
  {
    _dispatcher = dispatcher;
    _logger = logger;
  }

  public async Task Consume(ConsumeContext<OrderCancelledEvent> context)
  {
    var message = context.Message;

    await _dispatcher.DispatchAsync(
      message.CustomerId,
      "Order Cancelled",
      $"Your order {message.OrderId} has been cancelled!",
      NotificationChannel.BellAndEmail,
      message.Email
    );
  }
}
