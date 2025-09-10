using System;
using Contracts;
using MassTransit;

namespace NotificationService.Consumers;

public class UserUpdatedConsumer : IConsumer<UserUpdatedEvent>
{
    public async Task Consume(ConsumeContext<UserUpdatedEvent> context)
    {
        var message = context.Message;

        Console.WriteLine($"📧 User {message.Username} with ID {message.UserId} is updated at {message.UpdatedAt}");
        Console.WriteLine($"📧 Other Information: {message.Email} {message.ContactNo}");

        await Task.CompletedTask;
    }
}
