using System;
using Contracts;
using MassTransit;

namespace FinanceService.Consumers;

public class OrderConfirmedConsumer : IConsumer<OrderConfirmedEvent>
{
    public Task Consume(ConsumeContext<OrderConfirmedEvent> context)
    {
        throw new NotImplementedException();
        // Check PaymentType == Wallet
        // Deduct Wallet 
        // PaymentSucceededEvent or PaymentFailedEvent

        // Others just make them pending
    }
}
