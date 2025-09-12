using System;
using System.Linq.Expressions;
using Contracts;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using UserService.Data;
using UserService.DTO;
using UserService.Models;
using UserService.Services.Interfaces;

namespace UserService.Services;

public class UserService : IUserService
{
    private readonly UsersDbContext _dbContext;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<UserService> _logger;
    public UserService(ILogger<UserService> logger,
        UsersDbContext dbContext,
        IPublishEndpoint publishEndpoint)
    {
        _dbContext = dbContext;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }
    public async Task<GetUserDto> CreateUserAsync(CreateUserDto dto)
    {
        _logger.LogInformation("Creating new user with username: {Username}", dto.Username);

        var existingUser = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Username == dto.Username || u.Email == dto.Email);

        if (existingUser != null)
        {
            if (existingUser.Username == dto.Username)
            {
                _logger.LogWarning("User creation failed - username already exists: {Username}", dto.Username);
                throw new InvalidOperationException("A user with that username already exists.");
            }
            else
            {
                _logger.LogWarning("User creation failed - email already exists: {Email}", dto.Email);
                throw new InvalidOperationException("A user with that email already exists.");
            }
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

        _logger.LogInformation("Successfully created user: {Username} (ID: {UserId})", user.Username, user.UserId);

        // publish event UserCreatedEvent
        await _publishEndpoint.Publish(new UserCreatedEvent(
            user.UserId,
            user.Username,
            user.Email,
            user.ContactNo,
            user.CreatedAt
        ));

        _logger.LogInformation("Published UserCreatedEvent for user: {UserId}", user.UserId);

        // Use UserProjection as a lambda function to get GetUserDto
        return UserProjection.Compile()(user);
    }

    public async Task<bool> DeleteUserAsync(Guid id)
    {
        _logger.LogInformation("Deleting user with ID: {ID}", id);

        var user = await _dbContext.Users.FindAsync(id);
        if (user is null)
        {
            _logger.LogWarning("User deletion failed - user with ID does not exist in the database: {ID}", id);
            return false;
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

        _logger.LogInformation("Published UserDeletedEvent for user: {UserId}", user.UserId);

        return true;
    }

    public async Task<GetUserDto> EditUserInfoAsync(Guid id, EditUserDto dto)
    {
        _logger.LogInformation("Editing user with user ID: {ID}", id);

        var user = await _dbContext.Users.FindAsync(id);
        if (user is null)
        {
            _logger.LogWarning("User update failed - user not found with ID: {UserId}", id);
            throw new KeyNotFoundException($"There is no user with ID {id} in the database.");
        }

        bool hasChanges = false;

        var duplicates = await _dbContext.Users
            .Where(u => u.UserId != id && 
                        (u.Username == dto.Username || u.Email == dto.Email))
            .Select(u => new { u.Username, u.Email })
            .ToListAsync();

        // Check for duplicate username and email
        if (duplicates.Any(d => d.Username == dto.Username))
        {
            _logger.LogWarning("User update failed - Username {Username} already exists.", dto.Username);
            throw new InvalidOperationException("Username already exists.");
        }
        if (duplicates.Any(d => d.Email == dto.Email))
        {
            _logger.LogWarning("User update failed - Email {Email} already exists.", dto.Email);
            throw new InvalidOperationException("Email already exists.");
        }
        
        // Update fields
        if (!string.IsNullOrWhiteSpace(dto.Username) && dto.Username != user.Username)
        {
            user.Username = dto.Username;
            hasChanges = true;
        }
        if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email != user.Email)
        {
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
            await _publishEndpoint.Publish(new UserUpdatedEvent(
                user.UserId,
                user.Username,
                user.Email,
                user.ContactNo,
                user.UpdatedAt
            ));
            _logger.LogInformation("Published UserUpdatedEvent for user: {UserId}", user.UserId);
            _logger.LogInformation("Successfully updated user {UserId}.", user.UserId);
        }
        else
        {
            _logger.LogInformation("No changes detected for user {UserId}.", id);
        }

        // Use UserProjection as a lambda function to get GetUserDto
        return UserProjection.Compile()(user);
    }

    public async Task<List<GetUserDto>> GetAllAsync()
    {
         _logger.LogInformation("Getting all users in the database.");

        var allUsers = await _dbContext.Users
            .Select(UserProjection)
            .ToListAsync();

        _logger.LogInformation("Successfully retrieved all the users.");

        // returns List or [] if no value
        return allUsers;
    }

    public async Task<GetUserDto?> GetUserByIdAsync(Guid id)
    {
        _logger.LogInformation("Getting user with ID: {id} in the database.", id);

        var user = await _dbContext.Users
            .Select(UserProjection)
            .FirstOrDefaultAsync(user => user.UserId == id);

        _logger.LogInformation("Successfully retrieved the user with ID: {id}.", id);

        // returns GetUserDto or null
        return user;
    }

    private static readonly Expression<Func<User, GetUserDto>> UserProjection = user => new GetUserDto(
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
    );
}
