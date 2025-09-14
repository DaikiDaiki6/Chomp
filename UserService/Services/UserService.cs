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
        var existingUser = await _dbContext.Users
            .FirstOrDefaultAsync(u => (u.Username == dto.Username || u.Email == dto.Email) && !u.IsDeleted);

        if (existingUser != null)
        {
            if (existingUser.Username == dto.Username)
            {
                throw new InvalidOperationException("A user with that username already exists.");
            }
            else
            {
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

        // Publish event
        await _publishEndpoint.Publish(new UserCreatedEvent(
            user.UserId,
            user.Username,
            user.Email,
            user.ContactNo,
            user.CreatedAt
        ));

        _logger.LogInformation("Successfully created user: {Username} (ID: {UserId})", user.Username, user.UserId);

        return UserProjection.Compile()(user);
    }

    public async Task DeleteUserAsync(Guid id)
    {
        var user = await _dbContext.Users.FindAsync(id);
        if (user is null)
        {
            throw new KeyNotFoundException($"There is no user with ID {id} in the database.");
        }

        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync();

        // Publish event
        await _publishEndpoint.Publish(new UserDeletedEvent(
            user.UserId,
            user.Username,
            user.Email,
            user.ContactNo,
            DateTime.UtcNow
        ));

        _logger.LogInformation("Successfully deleted user: {UserId}", user.UserId);
    }

    public async Task<GetUserDto> EditUserInfoAsync(Guid id, EditUserDto dto)
    {
        var user = await _dbContext.Users.FindAsync(id);
        if (user is null)
        {
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
            throw new InvalidOperationException("Username already exists.");
        }
        if (duplicates.Any(d => d.Email == dto.Email))
        {
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

            // Publish event
            await _publishEndpoint.Publish(new UserUpdatedEvent(
                user.UserId,
                user.Username,
                user.Email,
                user.ContactNo,
                user.UpdatedAt
            ));
            _logger.LogInformation("Successfully updated user {UserId}", user.UserId);
        }

        return UserProjection.Compile()(user);
    }

    public async Task<List<GetUserDto>> GetAllAsync()
    {
        var allUsers = await _dbContext.Users
            .Where(u => !u.IsDeleted) // only non-deleted users
            .Select(UserProjection)
            .ToListAsync();

        if (allUsers.Count == 0)
        {
            throw new KeyNotFoundException("There are no users in the database.");
        }

        return allUsers;
    }

    public async Task<GetUserDto> GetUserByIdAsync(Guid id)
    {
        var user = await _dbContext.Users
            .Where(u => u.UserId == id && !u.IsDeleted) // only non-deleted users
            .Select(UserProjection)
            .FirstOrDefaultAsync();

        if (user is null)
        {
            throw new KeyNotFoundException($"There is no user with ID {id} in the database.");
        }

        return user;
    }

    public async Task<GetUserDto> SoftDeleteUserAsync(Guid id, string? reason = null)
    {
        _logger.LogInformation("Attempting to soft delete user: {UserId}", id);

        var user = await _dbContext.Users.FindAsync(id);
        if (user == null)
        {
            _logger.LogWarning("User with ID {UserId} not found for soft deletion", id);
            throw new KeyNotFoundException($"There is no user with ID {id} in the database");
        }

        if (user.IsDeleted)
        {
            _logger.LogWarning("User with ID {UserId} is already soft deleted", id);
            throw new InvalidOperationException("User is already deleted");
        }

        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        user.PermanentDeletionAt = DateTime.UtcNow.AddDays(7); // 7-day grace period
        user.DeletionReason = reason ?? "User requested deletion";
        user.AccountStatus = AccountStatus.PendingDeletion;
        user.UpdatedAt = DateTime.UtcNow;

        await RevokeAllRefreshToken(id, "User account deleted");

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Successfully soft deleted user: {UserId}. Permanent deletion scheduled for: {DeletionData}",
            user.UserId, user.PermanentDeletionAt);

        return UserProjection.Compile()(user);
    }

    public async Task<GetUserDto> RestoreUserAsync(Guid id)
    {
        _logger.LogInformation("Attempting to restore deleted user: {UserId}", id);

        var user = await _dbContext.Users.FindAsync(id);
        if (user == null)
        {
            _logger.LogWarning("User with ID {UserId} not found for restoration", id);
            throw new KeyNotFoundException($"There is no user with ID {id} in the database.");
        }

        if (!user.IsDeleted)
        {
            _logger.LogWarning("User with ID {UserID} is not deleted, cannot restore", id);
            throw new InvalidOperationException("User is not deleted");
        }

        if (user.PermanentDeletionAt.HasValue && user.PermanentDeletionAt <= DateTime.UtcNow)
        {
            _logger.LogWarning("User with ID {UserID} is past permanent deletion date, cannot restore", id);
            throw new InvalidOperationException("User deletion grace period has expired and cannot be restored");
        }

        user.IsDeleted = false;
        user.DeletedAt = null;
        user.PermanentDeletionAt = null;
        user.DeletionReason = null;
        user.AccountStatus = AccountStatus.Active;
        user.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Successfully restored user: {UserId}", user.UserId);
        return UserProjection.Compile()(user);
    }

    public async Task<List<GetUserDto>> GetSoftDeletedUsersAsync()
    {
        _logger.LogInformation("Fetching all soft deleted users");

        var deletedUsers = await _dbContext.Users
                                .Where(u => u.IsDeleted)
                                .Select(UserProjection)
                                .ToListAsync();
        _logger.LogInformation("Retrieved {Count} deleted users", deletedUsers.Count);
        return deletedUsers;
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
