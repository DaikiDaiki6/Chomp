using System;
using Contracts;
using MassTransit;

namespace NotificationService.Consumers.PaymentEvents;

public class PaymentFailedConsumer : IConsumer<PaymentFailedEvent>
{
    public async Task Consume(ConsumeContext<PaymentFailedEvent> context)
    {
        await Task.CompletedTask;
    }

}
