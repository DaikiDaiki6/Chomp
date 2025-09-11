using System;
using Contracts;
using MassTransit;

namespace NotificationService.Consumers;

public class UserUpdatedConsumer : IConsumer<UserUpdatedEvent>
{
    public async Task Consume(ConsumeContext<UserUpdatedEvent> context)
    {
        var message = context.Message;

        Console.WriteLine($"🔄 [USER UPDATED] User Profile Modified!");
        Console.WriteLine($"   🆔 User ID: {message.UserId}");
        Console.WriteLine($"   👨‍💼 Username: {message.Username}");
        Console.WriteLine($"   📧 Email: {message.Email}");
        Console.WriteLine($"   📱 Contact: {message.ContactNo}");
        Console.WriteLine($"   🕒 Updated At: {message.UpdatedAt:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"   ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

        await Task.CompletedTask;
    }
}
