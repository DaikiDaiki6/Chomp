using System;
using Contracts;
using MassTransit;

namespace PaymentService.Consumers;

public class OrderConfirmedConsumer : IConsumer<OrderConfirmedEvent>
{
    private readonly IPublishEndpoint _publishEndpoint;

    public OrderConfirmedConsumer(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }
    public async Task Consume(ConsumeContext<OrderConfirmedEvent> context)
    {
        var message = context.Message;
        Console.WriteLine($"[PaymentService] Received OrderConfirmedEvent for Order {message.OrderId}");
        Console.WriteLine($"[PaymentService] Customer: {message.CustomerId}, Total: ${message.TotalPrice}");

        // basic logic just for now
        var wallet = 500;
        Console.WriteLine($"[PaymentService] Checking wallet balance: ${wallet}");

        if (wallet >= message.TotalPrice)
        {
            Console.WriteLine($"[PaymentService] Payment SUCCESSFUL - Sufficient balance");
            await _publishEndpoint.Publish(new PaymentSucceededEvent(
                Guid.NewGuid(),
                message.OrderId,
                message.CustomerId,
                message.TotalPrice,
                DateTime.UtcNow
            ));
            Console.WriteLine($"[PaymentService] Payment SUCCESSFUL - Sufficient balance");
        }
        else
        {
            Console.WriteLine($"[PaymentService] Payment FAILED - Insufficient balance (Need: ${message.TotalPrice}, Have: ${wallet})");
            await _publishEndpoint.Publish(new PaymentFailedEvent(
                Guid.NewGuid(),
                message.OrderId,
                message.CustomerId,
                "Insufficient balance.",
                DateTime.UtcNow
            ));
            Console.WriteLine($"[PaymentService] Published PaymentFailedEvent for Order {message.OrderId}");
        }
        Console.WriteLine($"[PaymentService] Finished processing Order {message.OrderId}");

    }
}
