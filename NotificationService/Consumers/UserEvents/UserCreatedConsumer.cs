using System;
using Contracts;
using MassTransit;

namespace NotificationService.Consumers;

public class UserCreatedConsumer : IConsumer<UserCreatedEvent>
{
    public async Task Consume(ConsumeContext<UserCreatedEvent> context)
    {
        var message = context.Message;

        Console.WriteLine($"📧 User {message.Username} with ID {message.UserId} is created at {message.CreatedAt}");
        Console.WriteLine($"📧 Other Information: {message.Email} {message.ContactNo}");

        await Task.CompletedTask;
    }
}
