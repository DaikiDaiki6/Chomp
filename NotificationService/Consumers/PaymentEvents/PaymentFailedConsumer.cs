using System;
using Contracts;
using MassTransit;
using NotificationService.Models;
using NotificationService.Services.Interface;

namespace NotificationService.Consumers.PaymentEvents;

public class PaymentFailedConsumer: IConsumer<PaymentFailedEvent>
{
  private readonly INotificationDispatcher _dispatcher;
  private readonly ILogger<PaymentFailedConsumer> _logger;
  public PaymentFailedConsumer(INotificationDispatcher dispatcher, ILogger<PaymentFailedConsumer> logger)
  {
    _dispatcher = dispatcher;
    _logger = logger;
  }

  public async Task Consume(ConsumeContext<PaymentFailedEvent> context)
  {
    var message = context.Message;

    await _dispatcher.DispatchAsync(
      message.CustomerId,
      "Payment Failed",
      $"Your payment {message.PaymentId} for order {message.OrderId} has failed due to {message.Reason}",
      NotificationChannel.BellAndEmail,
      message.Email
    );
  }
}

