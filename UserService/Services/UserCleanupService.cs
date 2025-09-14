using System;
using Microsoft.EntityFrameworkCore;
using UserService.Data;

namespace UserService.Services;

public class UserCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<UserCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(24);

    public UserCleanupService(IServiceProvider serviceProvider,
        ILogger<UserCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;        
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("User cleanup service started");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformCleanUp();
                await Task.Delay(_cleanupInterval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("User cleanup service was cancelled");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during user cleanup");
                await Task.Delay(_cleanupInterval, stoppingToken);
            }
        }
        _logger.LogInformation("User cleanup service stopped");
    }

    private async Task PerformCleanUp()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<UsersDbContext>(); // get DbContext

        try
        {
            var now = DateTime.UtcNow;
            var usersToDelete = await context.Users
                .Where(u => u.IsDeleted &&
                    u.PermanentDeletionAt.HasValue &&
                    u.PermanentDeletionAt <= now)
                .ToListAsync();

            if (usersToDelete.Count != 0)
            {
                _logger.LogInformation("Found {Count} users ready for permanent deletion", usersToDelete.Count);

                foreach (var user in usersToDelete)
                {
                    _logger.LogInformation("Permanently deleting user {UserId} ({Username})",
                        user.UserId, user.Username);

                    var refreshTokens = await context.RefreshTokens
                        .Where(rt => rt.UserId == user.UserId)
                        .ToListAsync();

                    if (refreshTokens.Count != 0)
                    {
                        context.RefreshTokens.RemoveRange(refreshTokens);
                    }
                    context.Users.Remove(user);
                }

                await context.SaveChangesAsync();
                _logger.LogInformation("Successfully deleted {Count} users permanently", usersToDelete.Count);
            }
            else
            {
                _logger.LogDebug("No users found for permanent deletion");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user cleanup operation (PerformCleanUp methiod)");
            throw;
        }
    }
}
