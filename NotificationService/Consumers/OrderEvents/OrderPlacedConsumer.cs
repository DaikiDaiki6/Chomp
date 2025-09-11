using System;
using Contracts;
using MassTransit;

namespace NotificationService.Consumers.OrderEvents;

public class OrderPlacedConsumer : IConsumer<OrderPlacedEvent>
{
    public async Task Consume(ConsumeContext<OrderPlacedEvent> context)
    {
        var message = context.Message;

        Console.WriteLine($"   🆔 Order ID: {message.OrderId}");
        Console.WriteLine($"   👤 Customer ID: {message.CustomerId}");
        Console.WriteLine($"   💰 Total Price: ${message.TotalPrice:F2}");
        Console.WriteLine($"   🕒 Created At: {message.CreatedAt:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"   📦 Items ({message.Items.Count}):");
        
        foreach (var item in message.Items)
        {
            Console.WriteLine($"      • {item.ProductName} x{item.Quantity} @ ${item.UnitPrice:F2}");
        }
        
        Console.WriteLine($"   ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");


        await Task.CompletedTask; 
    }
}
