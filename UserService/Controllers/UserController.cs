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
            // check if loggedin 
            _logger.LogInformation("GetAll endpoint called by: {UserId}", Guid.NewGuid()); //# Add userId here later when log in is added

            // if(User.Role == Admin) do this 
            var allUsers = await _userService.GetAllAsync();

            if (allUsers.Count == 0)
            {
                _logger.LogWarning("No users found in the database.");
                return NotFound(new { errorMessage = "There are no users in the database." });
            }

            _logger.LogInformation("Successfully retrieved {UserCount} users.", allUsers.Count);
            return Ok(allUsers);
            // else Return Unathorized({errorMessage: "You are not authorized for this action."})
        }

        [HttpGet("{id:guid}")] // mainly for Admin -- can be abused (User can use it too)
        public async Task<IActionResult> GetUserById(Guid id)
        {
            // check if logged in
            // check role 
            // if user _logger.LogInformation("Getting user - User {id} Request", id);
            // if admin _logger.LogInformation("Getting user - Admin Request");
            _logger.LogInformation("GetUserById endpoint called by {UserId} - Admin Request", id); //temporary

            var user = await _userService.GetUserByIdAsync(id);
            if (user is null)
            {
                _logger.LogWarning("No user {UserId} found in the database.", id);
                return NotFound(new { errorMessage = $"There is no user with ID {id} in the database." });
            }

            return Ok(user);
            // else Return Unathorized({errorMessage: "You are not authorized for this action."})
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser(CreateUserDto dto)
        {
            // check if logged in
            _logger.LogInformation("CreateUser endpoint called for username: {Username}", dto.Username);
            // Check for duplicates
            try
            {
                var newUser = await _userService.CreateUserAsync(dto);
                _logger.LogInformation("User creation endpoint completed successfully for: {UserId}", newUser.UserId);

                return CreatedAtAction(nameof(GetUserById), new { id = newUser.UserId }, newUser);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("User creation endpoint failed: {ErrorMessage}", ex.Message);
                return BadRequest(new { errorMessage = ex.Message });
            }

            // else Return Unathorized({errorMessage: "You are not authorized for this action."})
        }

        [HttpPatch("{id:guid}")]
        public async Task<IActionResult> EditUserInfo(Guid id, EditUserDto dto)
        {
            // check if logged in
            _logger.LogInformation("EditUserInfo endpoint called for user with user ID: {ID}", id);

            try
            {
                var user = await _userService.EditUserInfoAsync(id, dto);
                return Ok(user);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("User update endpoint failed: {ErrorMessage}", ex.Message);
                return NotFound(new { errorMessage = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("User update endpoint failed: {ErrorMessage}", ex.Message);
                return BadRequest(new { errorMessage = ex.Message });
            }
            // else Return Unathorized({errorMessage: "You are not authorized for this action."})
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            // check if logged in
            _logger.LogInformation("Deleting endpoint called by: {UserId}", Guid.NewGuid()); //# Add userId here later when log in is added

            var userDeleted = await _userService.DeleteUserAsync(id);
            if (userDeleted)
            {
                return Ok(new { Message = $"User with ID {id} is deleted successfully." });
            }
            return NotFound(new { errorMessage = $"There are no user with ID {id} in the database." });
            // else Return Unathorized({errorMessage: "You are not authorized for this action."})
        }
    }
}
