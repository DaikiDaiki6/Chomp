using System;
using Contracts;
using MassTransit;

namespace NotificationService.Consumers;

public class UserCreatedConsumer : IConsumer<UserCreatedEvent>
{
    public async Task Consume(ConsumeContext<UserCreatedEvent> context)
    {
        var message = context.Message;

        Console.WriteLine($"👤 [USER CREATED] New User Registered!");
        Console.WriteLine($"   🆔 User ID: {message.UserId}");
        Console.WriteLine($"   👨‍💼 Username: {message.Username}");
        Console.WriteLine($"   📧 Email: {message.Email}");
        Console.WriteLine($"   📱 Contact: {message.ContactNo}");
        Console.WriteLine($"   🕒 Created At: {message.CreatedAt:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"   ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

        await Task.CompletedTask;
    }
}
