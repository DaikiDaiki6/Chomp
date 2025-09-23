using System;
using Contracts;
using MassTransit;
using NotificationService.Models;
using NotificationService.Services.Interface;

namespace NotificationService.Consumers.PaymentEvents;

public class PaymentSucceededConsumer: IConsumer<PaymentSucceededEvent>
{
  private readonly INotificationDispatcher _dispatcher;
  private readonly ILogger<PaymentSucceededConsumer> _logger;
  public PaymentSucceededConsumer(INotificationDispatcher dispatcher, ILogger<PaymentSucceededConsumer> logger)
  {
    _dispatcher = dispatcher;
    _logger = logger;
  }

  public async Task Consume(ConsumeContext<PaymentSucceededEvent> context)
  {
    var message = context.Message;

    await _dispatcher.DispatchAsync(
      message.CustomerId,
      "Payment Success",
      $"Your payment {message.PaymentId} for order {message.OrderId} has successfully been paid {message.Amount}!",
      NotificationChannel.BellAndEmail,
      message.Email
    );
  }
}
