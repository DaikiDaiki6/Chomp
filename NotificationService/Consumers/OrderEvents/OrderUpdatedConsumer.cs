using System;
using Contracts;
using MassTransit;

namespace NotificationService.Consumers.OrderEvents;

public class OrderUpdatedConsumer : IConsumer<OrderUpdatedEvent>
{
    public async Task Consume(ConsumeContext<OrderUpdatedEvent> context)
    {
        var message = context.Message;

        Console.WriteLine($"🔄 [ORDER UPDATED] Order Modified!");
        Console.WriteLine($"   🆔 Order ID: {message.OrderId}");
        Console.WriteLine($"   👤 Customer ID: {message.CustomerId}");
        Console.WriteLine($"   💰 Total Price: ${message.TotalPrice:F2}");
        Console.WriteLine($"   🕒 Updated At: {message.UpdatedAt:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"   📦 Current Items ({message.Items.Count}):");
        
        foreach (var item in message.Items)
        {
            Console.WriteLine($"      • {item.ProductName} x{item.Quantity} @ ${item.UnitPrice:F2}");
        }
        
        Console.WriteLine($"   ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");


        await Task.CompletedTask; 
    }
}
