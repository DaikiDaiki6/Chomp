using System;
using NotificationService.Models;

namespace NotificationService.Services.Interface;

public interface INotificationDispatcher
{
    Task DispatchAsync(Guid userId, string title, string message, NotificationChannel channel, string? email);
}
