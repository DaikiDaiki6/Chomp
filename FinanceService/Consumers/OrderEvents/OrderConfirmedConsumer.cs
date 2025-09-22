using System;
using Contracts;
using MassTransit;

namespace FinanceService.OrderEvents.Consumers;

public class OrderConfirmedConsumer : IConsumer<OrderConfirmedEvent>
{
    private readonly ILogger<OrderConfirmedConsumer> _logger;

    public OrderConfirmedConsumer(ILogger<OrderConfirmedConsumer> logger)
    {
        _logger = logger;
    }
    public Task Consume(ConsumeContext<OrderConfirmedEvent> context)
    {
        var message = context.Message;
        // Check PaymentType == Wallet
        // Deduct Wallet 
        // PaymentSucceededEvent or PaymentFailedEvent

        // Others just make them pending
        try
        {
            if (message is null)
            {
                throw new NullReferenceException("OrderConfimedEvent sent no context to the OrderConfirmedConsumer");
            }

            switch (message.PaymentType)
            {
                case PaymentType.ChompWallet:
                    break;
                case PaymentType.EWallet:
                    break;
                case PaymentType.Bank:

                    break;
                case PaymentType.COD:
                    break;
                default:
                    throw new Exception("Invalid Payment Method");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("{ex}", ex);
        }



        return Task.CompletedTask;

    }
}
