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
            // if user _logger.LogInformation("Getting all users - User {id} Request", id);
            // if admin _logger.LogInformation("Getting all users - Admin Request");
            _logger.LogInformation("Getting all users - Admin Request"); //temporary

            var user = await _dbContext.Users.FindAsync(id);
            if (user is null)
            {
                _logger.LogWarning("No user {id} found in the database.", id);
                return NotFound(new { errorMessage = $"There is no user with ID {id} in the database." });
            }

            _logger.LogInformation("Successfully retrieved user {id}.", id);
            return Ok(new GetUserDto(
                user.UserId,
                user.Username,
                user.FirstName,
                user.LastName,
                user.Email,
                user.ContactNo,
                user.Role,
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
            _logger.LogInformation("Creating a user."); // add UserId when logged in is added
            // Check for duplicates
            var existingUser = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Username == dto.Username || u.Email == dto.Email);

            if (existingUser != null)
            {
                _logger.LogWarning("User {id} already exists in the database.", existingUser.UserId);
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
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LastSignIn = null
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            // publish event UserCreatedEvent
            _logger.LogInformation("Created user event for user {id} is passed to the message bus.", user.UserId);
            await _publishEndpoint.Publish(new UserCreatedEvent(
                user.UserId,
                user.Username,
                user.Email,
                user.ContactNo,
                user.CreatedAt
            ));
            _logger.LogInformation("Successfully created user {id}.", user.UserId);
            return CreatedAtAction(nameof(GetUserById), new { id = user.UserId },
                new GetUserDto(
                    user.UserId,
                    user.Username,
                    user.FirstName,
                    user.LastName,
                    user.Email,
                    user.ContactNo,
                    user.Role,
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
            _logger.LogInformation("Updating a user."); // add UserId when logged in is added
            var user = await _dbContext.Users.FindAsync(id);

            if (user is null)
            {
                _logger.LogWarning("No user {id} found in the database.", id);
                return NotFound(new { errorMessage = $"There is no user with ID {id} in the database." });
            }

            // Check for duplicate username if username is being updated
            if (!string.IsNullOrWhiteSpace(dto.Username) && dto.Username != user.Username)
            {
                var existingUserWithUsername = await _dbContext.Users
                    .FirstOrDefaultAsync(u => u.Username == dto.Username && u.UserId != id);
                if (existingUserWithUsername != null)
                {
                    _logger.LogWarning("Username {username} already exists in the database.", user.Username);
                    return BadRequest(new { errorMessage = "Username already exists." });
                }
                user.Username = dto.Username;
            }

            // Check for duplicate email if email is being updated
            if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email != user.Email)
            {
                var existingUserWithEmail = await _dbContext.Users
                    .FirstOrDefaultAsync(u => u.Email == dto.Email && u.UserId != id);
                if (existingUserWithEmail != null)
                {
                    _logger.LogWarning("Email {email} already exists in the database.", user.Email);
                    return BadRequest(new { errorMessage = "Email already exists." });
                }
                user.Email = dto.Email;
            }

            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                user.Password = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            }
            if (!string.IsNullOrWhiteSpace(dto.FirstName))
            {
                user.FirstName = dto.FirstName;
            }
            if (!string.IsNullOrWhiteSpace(dto.LastName))
            {
                user.LastName = dto.LastName;
            }
            if (!string.IsNullOrWhiteSpace(dto.ContactNo))
            {
                user.ContactNo = dto.ContactNo;
            }

            user.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            // publish event UserUpdatedEvent
            _logger.LogInformation("Updated user event for user {id} is passed to the message bus.", user.UserId);
            await _publishEndpoint.Publish(new UserUpdatedEvent(
                user.UserId,
                user.Username,
                user.Email,
                user.ContactNo,
                user.UpdatedAt
            )); 
            _logger.LogInformation("Successfully updated user {id}.", user.UserId);
            return Ok(new GetUserDto(
                user.UserId,
                user.Username,
                user.FirstName,
                user.LastName,
                user.Email,
                user.ContactNo,
                user.Role,
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
            _logger.LogInformation("Deleting a user."); // add UserId when logged in is added
            var user = await _dbContext.Users.FindAsync(id);
            if (user is null)
            {
                _logger.LogWarning("No user {id} found in the database.", id);
                return NotFound(new { errorMessage = $"There is no user with ID {id} in the database." });
            }

            _dbContext.Users.Remove(user);
            await _dbContext.SaveChangesAsync();

            // publish event UserDeletedEvent
            _logger.LogInformation("Deleted user event for user {id} is passed to the message bus.", user.UserId);
            await _publishEndpoint.Publish(new UserDeletedEvent(
                user.UserId,
                user.Username,
                user.Email, 
                user.ContactNo,
                DateTime.UtcNow
            )); 
            _logger.LogInformation("Successfully deleted user {id}.", user.UserId);
            return Ok(new { errorMessage = $"User with ID {id} is deleted successfully." });
            // else Return Unathorized({errorMessage: "You are not authorized for this action."})
        }
    }
}
