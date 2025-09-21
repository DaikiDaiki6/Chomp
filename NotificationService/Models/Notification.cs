using System;

namespace NotificationService.Models;

public class Notification
{
    public Guid NotificationId;
    public Guid UserId;
    public string Title = string.Empty;
    public string Message = string.Empty;
    public bool IsRead;
    public DateTime CreatedAt = DateTime.UtcNow;
}
