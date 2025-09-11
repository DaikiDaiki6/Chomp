using System;
using Contracts;
using MassTransit;

namespace NotificationService.Consumers.ProductEvents;

public class ProductDeletedConsumer : IConsumer<ProductDeletedEvent>
{
    public async Task Consume(ConsumeContext<ProductDeletedEvent> context)
    {
        var message = context.Message;
        Console.WriteLine($"🗑️ [PRODUCT DELETED] Product Removed!");
        Console.WriteLine($"   🆔 Product ID: {message.ProductId}");
        Console.WriteLine($"   📝 Name: {message.ProductName}");
        Console.WriteLine($"   🕒 Deleted At: {message.DeletedAt:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"   ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

        await Task.CompletedTask; 
    }
}
