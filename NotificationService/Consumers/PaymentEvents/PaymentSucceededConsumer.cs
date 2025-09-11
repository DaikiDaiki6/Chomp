using System;
using Contracts;
using MassTransit;

namespace NotificationService.Consumers.PaymentEvents;

public class PaymentSucceededConsumer : IConsumer<PaymentSucceededEvent>
{
    public async Task Consume(ConsumeContext<PaymentSucceededEvent> context)
    {
        await Task.CompletedTask;
    }

}
