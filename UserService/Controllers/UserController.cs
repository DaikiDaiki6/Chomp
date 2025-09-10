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

        public UserController(UsersDbContext dbContext, IPublishEndpoint publishEndpoint)
        {
            _dbContext = dbContext;
            _publishEndpoint = publishEndpoint;
            //Serilog
        }

        [HttpGet] // Role Admin only
        public async Task<IActionResult> GetAll() //From UserInfo
        {
            // check if loggedin 
            // if(User.Role == Admin) do this 
            var allUsers = await _dbContext.Users.ToListAsync();
            if (allUsers is null || allUsers.Count == 0)
            {
                return NotFound(new { message = "There are no users in the database." });
            }
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
            // else Return Unathorized({message: "You are not authorized for this action."})
        }

        [HttpGet("{id:guid}")] // mainly for Admin -- can be abused (User can use it too)
        public async Task<IActionResult> GetUserById(Guid id)
        {
            // check if logged in
            var user = await _dbContext.Users.FindAsync(id);
            if (user is null)
            {
                return NotFound(new { message = $"There is no user with ID {id} in the database." });
            }
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
            // else Return Unathorized({message: "You are not authorized for this action."})
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser(CreateUserDto dto)
        {
            // check if logged in
            // Check for duplicates
            var existingUser = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Username == dto.Username || u.Email == dto.Email);

            if (existingUser != null)
            {
                return BadRequest(new { message = "Username or email already exists." });
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
            await _publishEndpoint.Publish(new UserCreatedEvent(
                user.UserId,
                user.Username,
                user.Email,
                user.ContactNo,
                user.CreatedAt
            ));

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

            // else Return Unathorized({message: "You are not authorized for this action."})
        }

        [HttpPatch("{id:guid}")]
        public async Task<IActionResult> EditUserInfo(Guid id, EditUserDto dto)
        {
            // check if logged in
            var user = await _dbContext.Users.FindAsync(id);

            if (user is null)
            {
                return NotFound(new { message = $"There is no user with ID {id} in the database." });
            }

            // Check for duplicate username if username is being updated
            if (!string.IsNullOrWhiteSpace(dto.Username) && dto.Username != user.Username)
            {
                var existingUserWithUsername = await _dbContext.Users
                    .FirstOrDefaultAsync(u => u.Username == dto.Username && u.UserId != id);
                if (existingUserWithUsername != null)
                {
                    return BadRequest(new { message = "Username already exists." });
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
                    return BadRequest(new { message = "Email already exists." });
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
            await _publishEndpoint.Publish(new UserUpdatedEvent(
                user.UserId,
                user.Username,
                user.Email,
                user.ContactNo,
                user.UpdatedAt
            )); 

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

            // else Return Unathorized({message: "You are not authorized for this action."})
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            // check if logged in
            var user = await _dbContext.Users.FindAsync(id);
            if (user is null)
            {
                return NotFound(new { message = $"There is no user with ID {id} in the database." });
            }

            _dbContext.Users.Remove(user);
            await _dbContext.SaveChangesAsync();

            // publish event UserDeletedEvent
            await _publishEndpoint.Publish(new UserDeletedEvent(
                user.UserId,
                user.Username,
                user.Email,
                user.ContactNo,
                DateTime.UtcNow
            )); 

            return Ok(new { message = $"User with ID {id} is deleted successfully." });
            // else Return Unathorized({message: "You are not authorized for this action."})
        }
    }
}
