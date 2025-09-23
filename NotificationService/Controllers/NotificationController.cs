using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Attributes;
using NotificationService.Services.Helper;

namespace NotificationService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AccountStatusFilter]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly ILogger<NotificationController> _logger;
        private readonly Services.NotificationService _notificationService;

        public NotificationController(
            ILogger<NotificationController> logger,
            Services.NotificationService notificationService
           )
        {
            _logger = logger;
            _notificationService = notificationService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll(int pageNumber, int pageSize)
        {
             _logger.LogInformation("Admin requested all notifications. Page: {PageNumber}, Size: {PageSize}", pageNumber, pageSize);

            var allNotifs = await _notificationService.GetAllNotifications(pageNumber, pageSize);

            _logger.LogInformation("Retrieved {Count} notifications for admin", allNotifs.Count);
            return Ok(allNotifs);
        }

        [HttpGet("my-notifications")]
        public async Task<IActionResult> GetMyNotifications(int pageNumber, int pageSize)
        {
            var (userId, userRole, _) = GetCurrentUserInfo.GetUserInfo(User);

            if (!Guid.TryParse(userId, out var userGuid))
            {
                _logger.LogWarning("Invalid user token when requesting my-notifications");
                return Unauthorized(new { errorMessage = "Invalid user token" });
            }

            _logger.LogInformation("User {UserId} requested their notifications. Page: {PageNumber}, Size: {PageSize}", userGuid, pageNumber, pageSize);

            var allNotifs = await _notificationService.GetMyNotifications(userGuid, pageNumber, pageSize);

            _logger.LogInformation("Retrieved {Count} notifications for user {UserId}", allNotifs.Count, userGuid);
            return Ok(allNotifs);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> NotifRead(Guid id)
        {
            var (userId, _, _) = GetCurrentUserInfo.GetUserInfo(User);

            if (!Guid.TryParse(userId, out var userGuid))
            {
                _logger.LogWarning("Invalid user token when requesting Notification Read endpoint");
                return Unauthorized(new { errorMessage = "Invalid user token" });
            }

            _logger.LogInformation("User {UserId} requested Notification read endpoint.", userGuid);

            await _notificationService.NotifRead(userGuid, id);
            
            return Ok(new { message = $"Successfully set the notification {id} to read.", id });
        }
    }
}
