using System;
using NotificationService.Data;
using NotificationService.Models;
using NotificationService.Services.Interface;

namespace NotificationService.Services;

public class NotificationDispatcher : INotificationDispatcher
{
    private readonly NotifsDbContext _dbContext;
    private readonly IEmailService _emailService;

    public NotificationDispatcher(NotifsDbContext dbContext, IEmailService emailService)
    {
        _dbContext = dbContext;
        _emailService = emailService;
    }

    public async Task DispatchAsync(Guid userId, string title, string message, NotificationChannel channel, string? email)
    {
        if (channel == NotificationChannel.None) return;
        var notification = new Notification
        {
            NotificationId = Guid.NewGuid(),
            UserId = userId,
            Title = title,
            Message = message,
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        };
        
        if (channel == NotificationChannel.Bell || channel == NotificationChannel.BellAndEmail)
        {


            _dbContext.Notifications.Add(notification);
            await _dbContext.SaveChangesAsync();
        }

        if (channel == NotificationChannel.Email || channel == NotificationChannel.BellAndEmail)
        {

            if (email is null) throw new KeyNotFoundException($"User with ID {userId} does not have an email");
            await _emailService.TestConsoleWriteLineForNow(notification, email);
        }
    }
}
