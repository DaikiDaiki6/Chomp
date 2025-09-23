using System;
using Contracts;
using MassTransit;
using NotificationService.Models;
using NotificationService.Services.Interface;

namespace NotificationService.Consumers.OrderEvents;

public class OrderUpdatedConsumer: IConsumer<OrderUpdatedEvent>
{
  private readonly INotificationDispatcher _dispatcher;
  private readonly ILogger<OrderUpdatedConsumer> _logger;
  public OrderUpdatedConsumer(INotificationDispatcher dispatcher, ILogger<OrderUpdatedConsumer> logger)
  {
    _dispatcher = dispatcher;
    _logger = logger;
  }

  public async Task Consume(ConsumeContext<OrderUpdatedEvent> context)
  {
    var message = context.Message;

    await _dispatcher.DispatchAsync(
      message.CustomerId,
      "Order Updated",
      $"Your order {message.OrderId} has been updated!",
      NotificationChannel.Bell,
      null
    );
  }
}
