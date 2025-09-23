using System;
using Microsoft.EntityFrameworkCore;
using NotificationService.Data;
using static NotificationService.DTO.NotificationDto;

namespace NotificationService.Services;

public class NotificationService
{
    private readonly ILogger<NotificationService> _logger;
    private readonly NotifsDbContext _dbContext;

    public NotificationService(ILogger<NotificationService> logger,
        NotifsDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<List<NotificationAdminDto>> GetAllNotifications(int pageNumber, int pageSize)
    {
        if (pageNumber < 1 || pageSize < 1 || pageSize > 100)
        {
            _logger.LogWarning("Invalid pagination parameters: pageNumber={PageNumber}, pageSize={PageSize}", pageNumber, pageSize);
            throw new ArgumentException("Invalid pagination parameters.");
        }

        _logger.LogInformation("Fetching all notifications: page {PageNumber}, size {PageSize}", pageNumber, pageSize);

        var notifications = await _dbContext.Notifications
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        _logger.LogInformation("Retrieved {Count} notifications for admin", notifications.Count);

        return notifications.Select(p => new NotificationAdminDto
        {
            NotificationId = p.NotificationId,
            UserId = p.UserId,
            Title = p.Title,
            Message = p.Message,
            IsRead = p.IsRead,
            CreatedAt = p.CreatedAt
        }).ToList();
    }

    public async Task<List<NotificationUserDto>> GetMyNotifications(Guid userId, int pageNumber, int pageSize)
    {
        if (pageNumber < 1 || pageSize < 1 || pageSize > 100)
        {
            _logger.LogWarning("Invalid pagination for user {UserId}: pageNumber={PageNumber}, pageSize={PageSize}", userId, pageNumber, pageSize);
            throw new ArgumentException("Invalid pagination parameters.");
        }

        _logger.LogInformation("Fetching notifications for user {UserId}: page {PageNumber}, size {PageSize}", userId, pageNumber, pageSize);

        var notifications = await _dbContext.Notifications
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        _logger.LogInformation("Retrieved {Count} notifications for user {UserId}", notifications.Count, userId);

        return notifications.Select(p => new NotificationUserDto
        {
            NotificationId = p.NotificationId,
            Title = p.Title,
            Message = p.Message,
            IsRead = p.IsRead,
            CreatedAt = p.CreatedAt
        }).ToList();
    }

    public async Task NotifRead(Guid userId, Guid notifId)
    {
        var notification = await _dbContext.Notifications
            .FirstOrDefaultAsync(u => u.NotificationId == notifId && u.UserId == userId) ?? throw new KeyNotFoundException($"Notification {notifId} is not available for user {userId}");

        notification.IsRead = true;

        await _dbContext.SaveChangesAsync();
    }
}
