using System;
using Contracts;
using MassTransit;

namespace NotificationService.Consumers.UserEvents;

public class UserDeletedConsumer : IConsumer<UserDeletedEvent>
{
    public async Task Consume(ConsumeContext<UserDeletedEvent> context)
    {
        var message = context.Message;
        
        Console.WriteLine($"🗑️ [USER DELETED] User Account Removed!");
        Console.WriteLine($"   🆔 User ID: {message.UserId}");
        Console.WriteLine($"   👨‍💼 Username: {message.Username}");
        Console.WriteLine($"   📧 Email: {message.Email}");
        Console.WriteLine($"   📱 Contact: {message.ContactNo}");
        Console.WriteLine($"   🕒 Deleted At: {message.DeletedAt:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"   ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

        await Task.CompletedTask;
    }
}
