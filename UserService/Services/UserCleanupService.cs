using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using UserService.Data;

namespace UserService.Services
{
    public class UserCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<UserCleanupService> _logger;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(24); // Run daily

        public UserCleanupService(IServiceProvider serviceProvider, ILogger<UserCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PerformCleanup();
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
                    // Continue running even if there's an error
                    await Task.Delay(_cleanupInterval, stoppingToken);
                }
            }
        }

        private async Task PerformCleanup()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<UsersDbContext>();

            try
            {
                var now = DateTime.UtcNow;
                var usersToDelete = await context.Users
                    .Where(u => u.IsDeleted && 
                               u.PermanentDeletionAt.HasValue && 
                               u.PermanentDeletionAt <= now)
                    .ToListAsync();

                if (usersToDelete.Any())
                {
                    _logger.LogInformation("Found {Count} users ready for permanent deletion", usersToDelete.Count);

                    foreach (var user in usersToDelete)
                    {
                        _logger.LogInformation("Permanently deleting user {UserId} ({Username})", 
                            user.UserId, user.Username);

                        // Remove refresh tokens first
                        var refreshTokens = await context.RefreshTokens
                            .Where(rt => rt.UserId == user.UserId)
                            .ToListAsync();

                        if (refreshTokens.Any())
                        {
                            context.RefreshTokens.RemoveRange(refreshTokens);
                        }

                        // Remove the user
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
                _logger.LogError(ex, "Error during user cleanup operation");
                throw;
            }
        }
    }
}