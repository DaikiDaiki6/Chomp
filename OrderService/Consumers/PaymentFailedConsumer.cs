using System;
using Contracts;
using MassTransit;

namespace OrderService.Consumers;

public class PaymentFailedConsumer: IConsumer<PaymentFailedEvent>
{
    public async Task Consume(ConsumeContext<PaymentFailedEvent> context)
    {
        var message = context.Message;
        Console.WriteLine($"Failed payment id: {message.PaymentId}");
        await Task.CompletedTask;
    }
}
