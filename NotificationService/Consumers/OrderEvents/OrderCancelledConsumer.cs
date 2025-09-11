using System;
using Contracts;
using MassTransit;

namespace NotificationService.Consumers.OrderEvents;

public class OrderCancelledConsumer : IConsumer<OrderCancelledEvent>
{
    public async Task Consume(ConsumeContext<OrderCancelledEvent> context)
    {
        var message = context.Message;

        Console.WriteLine($"❌ [ORDER CANCELLED] Order Cancelled!");
        Console.WriteLine($"   🆔 Order ID: {message.OrderId}");
        Console.WriteLine($"   👤 Customer ID: {message.CustomerId}");
        Console.WriteLine($"   📝 Reason: {message.Reason}");
        Console.WriteLine($"   🕒 Cancelled At: {message.CancelledAt:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"   ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

        await Task.CompletedTask; 
    }
}
