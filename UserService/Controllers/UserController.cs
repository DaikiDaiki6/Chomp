using Contracts;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserService.Data;
using UserService.DTO;
using UserService.Models;

namespace UserService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        // Check if account is active or banned if banned unauthorized
        private readonly UsersDbContext _dbContext;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<UserController> _logger;

        public UserController(UsersDbContext dbContext, IPublishEndpoint publishEndpoint, ILogger<UserController> logger)
        {
            _dbContext = dbContext;
            _publishEndpoint = publishEndpoint;
            _logger = logger;
        }

        [HttpGet] // Role Admin only
        public async Task<IActionResult> GetAll() //From UserInfo
        {

            // check if loggedin 
            // if(User.Role == Admin) do this 
            _logger.LogInformation("Getting all users - Admin Request");
            var allUsers = await _dbContext.Users.ToListAsync();
            if (allUsers is null || allUsers.Count == 0)
            {
                _logger.LogWarning("No users found in the database.");
                return NotFound(new { errorMessage = "There are no users in the database." });
            }

            _logger.LogInformation("Successfully retrieved {UserCount} users.", allUsers.Count);
            return Ok(allUsers.Select(user => new GetUserDto(
                user.UserId,
                user.Username,
                user.FirstName,
                user.LastName,
                user.Email,
                user.ContactNo,
                user.Role,
                user.AccountStatus,
                user.CreatedAt,
                user.UpdatedAt,
                user.LastSignIn
            )));
            // else Return Unathorized({errorMessage: "You are not authorized for this action."})
        }

        [HttpGet("{id:guid}")] // mainly for Admin -- can be abused (User can use it too)
        public async Task<IActionResult> GetUserById(Guid id)
        {
            // check if logged in
            // check role 
            // if user _logger.LogInformation("Getting user - User {id} Request", id);
            // if admin _logger.LogInformation("Getting user - Admin Request");
            _logger.LogInformation("Getting user - Admin Request for user {UserId}", id); //temporary

            var user = await _dbContext.Users.FindAsync(id);
            if (user is null)
            {
                _logger.LogWarning("No user {UserId} found in the database.", id);
                return NotFound(new { errorMessage = $"There is no user with ID {id} in the database." });
            }

            _logger.LogInformation("Successfully retrieved user {UserId}.", id);
            return Ok(new GetUserDto(
                user.UserId,
                user.Username,
                user.FirstName,
                user.LastName,
                user.Email,
                user.ContactNo,
                user.Role,
                user.AccountStatus,
                user.CreatedAt,
                user.UpdatedAt,
                user.LastSignIn
            ));
            // else Return Unathorized({errorMessage: "You are not authorized for this action."})
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser(CreateUserDto dto)
        {
            // check if logged in
            _logger.LogInformation("Creating a user with username {Username}.", dto.Username);
            // Check for duplicates
            var existingUser = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Username == dto.Username || u.Email == dto.Email);

            if (existingUser != null)
            {
                _logger.LogWarning("User creation failed - Username {Username} or email {Email} already exists.", dto.Username, dto.Email);
                return BadRequest(new { errorMessage = "Username or email already exists." });
            }
            var user = new User
            {
                UserId = Guid.NewGuid(),
                Username = dto.Username,
                Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                ContactNo = dto.ContactNo,
                Role = Roles.User,
                AccountStatus = AccountStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LastSignIn = null
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            // publish event UserCreatedEvent
            _logger.LogInformation("Created user event for user {UserId} is passed to the message bus.", user.UserId);
            await _publishEndpoint.Publish(new UserCreatedEvent(
                user.UserId,
                user.Username,
                user.Email,
                user.ContactNo,
                user.CreatedAt
            ));
            _logger.LogInformation("Successfully created user {UserId}.", user.UserId);
            return CreatedAtAction(nameof(GetUserById), new { id = user.UserId },
                new GetUserDto(
                    user.UserId,
                    user.Username,
                    user.FirstName,
                    user.LastName,
                    user.Email,
                    user.ContactNo,
                    user.Role,
                    user.AccountStatus,
                    user.CreatedAt,
                    user.UpdatedAt,
                    user.LastSignIn
                ));

            // else Return Unathorized({errorMessage: "You are not authorized for this action."})
        }

        [HttpPatch("{id:guid}")]
        public async Task<IActionResult> EditUserInfo(Guid id, EditUserDto dto)
        {
            // check if logged in
            _logger.LogInformation("Updating user {UserId}.", id);
            var user = await _dbContext.Users.FindAsync(id);

            if (user is null)
            {
                _logger.LogWarning("No user {UserId} found in the database.", id);
                return NotFound(new { errorMessage = $"There is no user with ID {id} in the database." });
            }

            bool hasChanges = false;

            // Check for duplicate username if username is being updated
            if (!string.IsNullOrWhiteSpace(dto.Username) && dto.Username != user.Username)
            {
                var existingUserWithUsername = await _dbContext.Users
                    .FirstOrDefaultAsync(u => u.Username == dto.Username && u.UserId != id);
                if (existingUserWithUsername != null)
                {
                    _logger.LogWarning("User update failed - Username {Username} already exists.", dto.Username);
                    return BadRequest(new { errorMessage = "Username already exists." });
                }
                user.Username = dto.Username;
                hasChanges = true;
            }

            // Check for duplicate email if email is being updated
            if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email != user.Email)
            {
                var existingUserWithEmail = await _dbContext.Users
                    .FirstOrDefaultAsync(u => u.Email == dto.Email && u.UserId != id);
                if (existingUserWithEmail != null)
                {
                    _logger.LogWarning("User update failed - Email {Email} already exists.", dto.Email);
                    return BadRequest(new { errorMessage = "Email already exists." });
                }
                user.Email = dto.Email;
                hasChanges = true;
            }

            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                user.Password = BCrypt.Net.BCrypt.HashPassword(dto.Password);
                hasChanges = true;
            }
            if (!string.IsNullOrWhiteSpace(dto.FirstName) && dto.FirstName != user.FirstName)
            {
                user.FirstName = dto.FirstName;
                hasChanges = true;
            }
            if (!string.IsNullOrWhiteSpace(dto.LastName) && dto.LastName != user.LastName)
            {
                user.LastName = dto.LastName;
                hasChanges = true;
            }
            if (!string.IsNullOrWhiteSpace(dto.ContactNo) && dto.ContactNo != user.ContactNo)
            {
                user.ContactNo = dto.ContactNo;
                hasChanges = true;
            }

            if (hasChanges)
            {
                user.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();

                // publish event UserUpdatedEvent
                _logger.LogInformation("Updated user event for user {UserId} is passed to the message bus.", user.UserId);
                await _publishEndpoint.Publish(new UserUpdatedEvent(
                    user.UserId,
                    user.Username,
                    user.Email,
                    user.ContactNo,
                    user.UpdatedAt
                ));
                _logger.LogInformation("Successfully updated user {UserId}.", user.UserId);
            }
            else
            {
                _logger.LogInformation("No changes detected for user {UserId}.", id);
            }
            return Ok(new GetUserDto(
                user.UserId,
                user.Username,
                user.FirstName,
                user.LastName,
                user.Email,
                user.ContactNo,
                user.Role,
                user.AccountStatus,
                user.CreatedAt,
                user.UpdatedAt,
                user.LastSignIn
            ));

            // else Return Unathorized({errorMessage: "You are not authorized for this action."})
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            // check if logged in
            _logger.LogInformation("Deleting user {UserId}.", id);
            var user = await _dbContext.Users.FindAsync(id);
            if (user is null)
            {
                _logger.LogWarning("No user {UserId} found in the database.", id);
                return NotFound(new { errorMessage = $"There is no user with ID {id} in the database." });
            }

            _dbContext.Users.Remove(user);
            await _dbContext.SaveChangesAsync();

            // publish event UserDeletedEvent
            _logger.LogInformation("Deleted user event for user {UserId} is passed to the message bus.", user.UserId);
            await _publishEndpoint.Publish(new UserDeletedEvent(
                user.UserId,
                user.Username,
                user.Email,
                user.ContactNo,
                DateTime.UtcNow
            ));
            _logger.LogInformation("Successfully deleted user {UserId}.", user.UserId);
            return Ok(new { errorMessage = $"User with ID {id} is deleted successfully." });
            // else Return Unathorized({errorMessage: "You are not authorized for this action."})
        }
    }
}
