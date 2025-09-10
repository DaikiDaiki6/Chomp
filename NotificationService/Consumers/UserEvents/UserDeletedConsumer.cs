using System;
using Contracts;
using MassTransit;

namespace NotificationService.Consumers.UserEvents;

public class UserDeletedConsumer : IConsumer<UserDeletedEvent>
{
    public async Task Consume(ConsumeContext<UserDeletedEvent> context)
    {
        var message = context.Message;
        
        Console.WriteLine($"📧 User {message.Username} with ID {message.UserId} is deleted at {message.DeletedAt}");
        Console.WriteLine($"📧 Other Information: {message.Email} {message.ContactNo}");

        await Task.CompletedTask;
    }
}
