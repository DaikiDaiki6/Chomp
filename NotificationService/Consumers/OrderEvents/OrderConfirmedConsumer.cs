using System;
using Contracts;
using MassTransit;
using NotificationService.Models;
using NotificationService.Services.Interface;

namespace NotificationService.Consumers.OrderEvents;

public class OrderConfirmedConsumer: IConsumer<OrderConfirmedEvent>
{
  private readonly INotificationDispatcher _dispatcher;
  private readonly ILogger<OrderConfirmedConsumer> _logger;
  public OrderConfirmedConsumer(INotificationDispatcher dispatcher, ILogger<OrderConfirmedConsumer> logger)
  {
    _dispatcher = dispatcher;
    _logger = logger;
  }

  public async Task Consume(ConsumeContext<OrderConfirmedEvent> context)
  {
    var message = context.Message;

    await _dispatcher.DispatchAsync(
      message.CustomerId,
      "Order Confirmed",
      $"Your order {message.OrderId} has been confirmed!",
      NotificationChannel.BellAndEmail,
      message.Email
    );
  }
}
