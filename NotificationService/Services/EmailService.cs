using System;
using NotificationService.Models;
using NotificationService.Services.Interface;

namespace NotificationService.Services;

public class EmailService : IEmailService
{
    public Task TestConsoleWriteLineForNow(Notification notification, string email)
    {
        Console.WriteLine($"Notification ID: {notification.NotificationId}\nUser ID: {notification.UserId}\nTitle: {notification.Title}\nMessage: {notification.Message}\nIs Read: {notification.IsRead}\nCreated At: {notification.CreatedAt}\nTo {email}");
        return Task.CompletedTask;
    }
}
