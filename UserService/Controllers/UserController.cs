using Contracts;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UserService.Attributes;
using UserService.Data;
using UserService.DTO;
using UserService.Models;
using UserService.Services.Interfaces;

namespace UserService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AccountStatusFilter] // Apply account status checking to all endpoints
    public class UserController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;
        private readonly IUserService _userService;

        public UserController(ILogger<UserController> logger,
            IUserService userService)
        {
            _logger = logger;
            _userService = userService;
        }

        // Helper method to check if current user can modify target user
        private bool CanModifyUser(Guid targetUserId)
        {
            var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

            // Admin can modify any user
            if (currentUserRole == "Admin")
                return true;

            // User can only modify their own account
            if (Guid.TryParse(currentUserIdClaim, out var currentUserId))
                return currentUserId == targetUserId;

            return false;
        }

        [HttpGet] // Role Admin only
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll() //From UserInfo
        {
            _logger.LogInformation("GetAll endpoint called");

            try
            {
                var allUsers = await _userService.GetAllAsync();
                return Ok(allUsers);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("No users found: {ErrorMessage}", ex.Message);
                return NotFound(new { errorMessage = ex.Message });
            }
        }

        [HttpGet("{id:guid}")] // mainly for Admin -- can be abused (User can use it too)
        [Authorize] // Any authenticated user can access
        public async Task<IActionResult> GetUserById(Guid id)
        {
            _logger.LogInformation("GetUserById endpoint called for user: {UserId}", id);

            // Check if user can access this profile
            if (!CanModifyUser(id))
            {
                _logger.LogWarning("User attempted to access unauthorized profile: {UserId}", id);
                return Forbid("You can only access your own profile unless you are an admin.");
            }

            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                return Ok(user);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("User not found: {ErrorMessage}", ex.Message);
                return NotFound(new { errorMessage = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser(CreateUserDto dto)
        {
            _logger.LogInformation("CreateUser endpoint called for username: {Username}", dto.Username);

            try
            {
                var newUser = await _userService.CreateUserAsync(dto);
                _logger.LogInformation("User creation completed successfully for: {UserId}", newUser.UserId);

                return CreatedAtAction(nameof(GetUserById), new { id = newUser.UserId }, newUser);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("User creation failed: {ErrorMessage}", ex.Message);
                return BadRequest(new { errorMessage = ex.Message });
            }
        }

        [HttpPatch("{id:guid}")]
        [Authorize] // User can edit their own profile, Admin can edit any
        public async Task<IActionResult> EditUserInfo(Guid id, EditUserDto dto)
        {
            _logger.LogInformation("EditUserInfo endpoint called for user: {UserId}", id);

            // Check if user can modify this profile
            if (!CanModifyUser(id))
            {
                _logger.LogWarning("User attempted to modify unauthorized profile: {UserId}", id);
                return Forbid("You can only modify your own profile unless you are an admin.");
            }

            try
            {
                var user = await _userService.EditUserInfoAsync(id, dto);
                _logger.LogInformation("User update completed successfully for: {UserId}", id);
                return Ok(user);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("User not found during update: {ErrorMessage}", ex.Message);
                return NotFound(new { errorMessage = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("User update failed: {ErrorMessage}", ex.Message);
                return BadRequest(new { errorMessage = ex.Message });
            }
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")] // Only Admin can hard delete users
        public async Task<IActionResult> DeleteUser(Guid id) //# add timer for deletion cancel (7 days or something) like in normal social medias
        {
            _logger.LogInformation("DeleteUser endpoint called for user: {UserId}", id);

            try
            {
                await _userService.DeleteUserAsync(id);
                _logger.LogInformation("User deletion completed successfully for: {UserId}", id);
                
                return NoContent(); 
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("User not found during deletion: {ErrorMessage}", ex.Message);
                return NotFound(new { errorMessage = ex.Message });
            }
        }

        [HttpPost("{id:guid}/soft-delete")]
        [Authorize] // User can soft delete their own account, Admin can soft delete any
        public async Task<IActionResult> SoftDeleteUser(Guid id, [FromBody] SoftDeleteRequest? request = null)
        {
            _logger.LogInformation("SoftDeleteUser endpoint called for user: {UserId}", id);

            // Check if user can soft delete this account
            if (!CanModifyUser(id))
            {
                _logger.LogWarning("User attempted to soft delete unauthorized account: {UserId}", id);
                return Forbid("You can only delete your own account unless you are an admin.");
            }

            try
            {
                var deletedUser = await _userService.SoftDeleteUserAsync(id, request?.Reason);
                _logger.LogInformation("User soft deletion completed successfully for: {UserId}", id);
                
                return Ok(new 
                { 
                    message = "User account has been scheduled for deletion. You have 7 days to restore your account.",
                    user = deletedUser,
                    gracePeriodEnds = DateTime.UtcNow.AddDays(7)
                }); 
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("User not found during soft deletion: {ErrorMessage}", ex.Message);
                return NotFound(new { errorMessage = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Invalid operation during soft deletion: {ErrorMessage}", ex.Message);
                return BadRequest(new { errorMessage = ex.Message });
            }
        }

        [HttpPost("{id:guid}/restore")]
        [Authorize] // User can restore their own account, Admin can restore any
        public async Task<IActionResult> RestoreUser(Guid id)
        {
            _logger.LogInformation("RestoreUser endpoint called for user: {UserId}", id);

            // Check if user can restore this account
            if (!CanModifyUser(id))
            {
                _logger.LogWarning("User attempted to restore unauthorized account: {UserId}", id);
                return Forbid("You can only restore your own account unless you are an admin.");
            }

            try
            {
                var restoredUser = await _userService.RestoreUserAsync(id);
                _logger.LogInformation("User restoration completed successfully for: {UserId}", id);
                
                return Ok(new 
                { 
                    message = "User account has been successfully restored.",
                    user = restoredUser
                }); 
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("User not found during restoration: {ErrorMessage}", ex.Message);
                return NotFound(new { errorMessage = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Invalid operation during restoration: {ErrorMessage}", ex.Message);
                return BadRequest(new { errorMessage = ex.Message });
            }
        }

        [HttpGet("deleted")]
        [Authorize(Roles = "Admin")] // Only admins can view deleted users
        public async Task<IActionResult> GetDeletedUsers()
        {
            _logger.LogInformation("GetDeletedUsers endpoint called");

            try
            {
                var deletedUsers = await _userService.GetDeletedUsersAsync();
                return Ok(deletedUsers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving deleted users");
                return StatusCode(500, new { errorMessage = "An error occurred while retrieving deleted users." });
            }
        }
    }
}
