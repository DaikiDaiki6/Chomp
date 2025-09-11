using System;
using Contracts;
using MassTransit;

namespace NotificationService.Consumers.ProductEvents;

public class ProductCreatedConsumer : IConsumer<ProductCreatedEvent>
{
    public async Task Consume(ConsumeContext<ProductCreatedEvent> context)
    {
        var message = context.Message;
        Console.WriteLine($"✨ [PRODUCT CREATED] New Product Added!");
        Console.WriteLine($"   🆔 Product ID: {message.ProductId}");
        Console.WriteLine($"   📝 Name: {message.ProductName}");
        Console.WriteLine($"   💰 Price: ${message.Price:F2}");
        Console.WriteLine($"   📦 Stock: {message.Stock}");
        Console.WriteLine($"   🕒 Created At: {message.CreatedAt:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"   ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

        await Task.CompletedTask; 
    }
}
