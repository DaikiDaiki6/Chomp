using Contracts;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserService.Data;
using UserService.DTO;
using UserService.Models;
using UserService.Services.Interfaces;

namespace UserService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        // Check if account is active or banned if banned unauthorized
        private readonly ILogger<UserController> _logger;
        private readonly IUserService _userService;

        public UserController(ILogger<UserController> logger,
            IUserService userService)
        {
            _logger = logger;
            _userService = userService;
        }

        [HttpGet] // Role Admin only
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
            // else Return Unathorized({errorMessage: "You are not authorized for this action."})
        }

        [HttpGet("{id:guid}")] // mainly for Admin -- can be abused (User can use it too)
        public async Task<IActionResult> GetUserById(Guid id)
        {
            _logger.LogInformation("GetUserById endpoint called for user: {UserId}", id);

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
            // else Return Unathorized({errorMessage: "You are not authorized for this action."})
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

            // else Return Unathorized({errorMessage: "You are not authorized for this action."})
        }

        [HttpPatch("{id:guid}")]
        public async Task<IActionResult> EditUserInfo(Guid id, EditUserDto dto)
        {
            _logger.LogInformation("EditUserInfo endpoint called for user: {UserId}", id);

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
            // else Return Unathorized({errorMessage: "You are not authorized for this action."})
        }

        [HttpDelete("{id:guid}")]
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
            // else Return Unathorized({errorMessage: "You are not authorized for this action."})
        }
    }
}
