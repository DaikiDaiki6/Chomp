using System;
using Contracts;
using MassTransit;

namespace NotificationService.Consumers.ProductEvents;

public class ProductUpdatedConsumer : IConsumer<ProductUpdatedEvent>
{
    public async Task Consume(ConsumeContext<ProductUpdatedEvent> context)
    {
        var message = context.Message;
        Console.WriteLine($"🔄 [PRODUCT UPDATED] Product ID: {message.ProductId}");
        Console.WriteLine($"   📝 Name: {message.ProductName}");
        Console.WriteLine($"   💰 Price: ${message.Price:F2}");
        Console.WriteLine($"   📦 Stock: {message.Stock}");
        Console.WriteLine($"   🕒 Updated At: {message.UpdatedAt:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"   ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

        await Task.CompletedTask; 
    }
}