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

    public async Task<GetUserDto> SoftDeleteUserAsync(Guid id, string? reason = null)
    {
        _logger.LogInformation("Attempting to soft delete user: {UserId}", id);

        var user = await _dbContext.Users.FindAsync(id);
        if (user == null)
        {
            _logger.LogWarning("User with ID {UserId} not found for soft deletion", id);
            throw new KeyNotFoundException($"There is no user with ID {id} in the database.");
        }

        if (user.IsDeleted)
        {
            _logger.LogWarning("User with ID {UserId} is already soft deleted", id);
            throw new InvalidOperationException("User is already deleted.");
        }

        // Set soft delete properties
        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        user.PermanentDeletionAt = DateTime.UtcNow.AddDays(7); // 7-day grace period
        user.DeletionReason = reason ?? "User requested deletion";
        user.AccountStatus = AccountStatus.PendingDeletion;
        user.UpdatedAt = DateTime.UtcNow;

        // Revoke all refresh tokens
        await RevokeAllRefreshTokensAsync(id, "User account deleted");

        await _dbContext.SaveChangesAsync();

        var userDto = new GetUserDto(
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

        _logger.LogInformation("Successfully soft deleted user: {UserId}. Permanent deletion scheduled for: {DeletionDate}", 
            user.UserId, user.PermanentDeletionAt);

        return userDto;
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
            _logger.LogWarning("User with ID {UserId} is not deleted, cannot restore", id);
            throw new InvalidOperationException("User is not deleted.");
        }

        if (user.PermanentDeletionAt.HasValue && user.PermanentDeletionAt <= DateTime.UtcNow)
        {
            _logger.LogWarning("User with ID {UserId} is past permanent deletion date, cannot restore", id);
            throw new InvalidOperationException("User deletion grace period has expired and cannot be restored.");
        }

        // Restore user
        user.IsDeleted = false;
        user.DeletedAt = null;
        user.PermanentDeletionAt = null;
        user.DeletionReason = null;
        user.AccountStatus = AccountStatus.Active;
        user.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        var userDto = new GetUserDto(
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

        _logger.LogInformation("Successfully restored user: {UserId}", user.UserId);
        return userDto;
    }

    public async Task<List<GetUserDto>> GetDeletedUsersAsync()
    {
        _logger.LogInformation("Fetching all soft deleted users");

        var deletedUsers = await _dbContext.Users
            .Where(u => u.IsDeleted)
            .Select(u => new GetUserDto(
                u.UserId,
                u.Username,
                u.FirstName,
                u.LastName,
                u.Email,
                u.ContactNo,
                u.Role,
                u.AccountStatus,
                u.CreatedAt,
                u.UpdatedAt,
                u.LastSignIn
            ))
            .ToListAsync();

        _logger.LogInformation("Retrieved {Count} deleted users", deletedUsers.Count);
        return deletedUsers;
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
            .Where(u => !u.IsDeleted)  // Only return non-deleted users
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
            .Where(u => u.UserId == id && !u.IsDeleted)  // Only return non-deleted users
            .Select(UserProjection)
            .FirstOrDefaultAsync();

        if (user is null)
        {
            throw new KeyNotFoundException($"There is no user with ID {id} in the database.");
        }

        return user;
    }

    public async Task<GetUserDto> ValidateUserCredentialAsync(string username, string password)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Username == username && !u.IsDeleted);

        if (user is null)
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        if (!BCrypt.Net.BCrypt.Verify(password, user.Password))
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        if (user.AccountStatus != AccountStatus.Active)
        {
            throw new UnauthorizedAccessException("Account is not active.");
        }

        return UserProjection.Compile()(user);
    }

    public async Task UpdateLastSignInAsync(Guid userId)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        if (user != null)
        {
            user.LastSignIn = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
            
            _logger.LogInformation("Updated LastSignIn for user: {UserId}", userId);
        }
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

    public async Task<string> GenerateRefreshTokenAsync(Guid userId)
    {
        // Generate cryptographically secure random token
        var randomBytes = new byte[32];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        var token = Convert.ToBase64String(randomBytes);

        // Create refresh token record
        var refreshToken = new RefreshToken
        {
            RefreshTokenId = Guid.NewGuid(),
            UserId = userId,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddDays(7), // 7 days expiry
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Generated refresh token for user: {UserId}", userId);
        return token;
    }

    public async Task<GetUserDto> ValidateRefreshTokenAsync(string refreshToken)
    {
        var tokenRecord = await _dbContext.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken && rt.IsActive);

        if (tokenRecord == null)
        {
            _logger.LogWarning("Invalid or expired refresh token used");
            throw new UnauthorizedAccessException("Invalid refresh token");
        }

        if (tokenRecord.User.AccountStatus != AccountStatus.Active)
        {
            _logger.LogWarning("Refresh token used for inactive user: {UserId}", tokenRecord.UserId);
            throw new UnauthorizedAccessException("Account is not active");
        }

        _logger.LogInformation("Valid refresh token used for user: {UserId}", tokenRecord.UserId);
        return UserProjection.Compile()(tokenRecord.User);
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken, string reason)
    {
        var tokenRecord = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (tokenRecord != null && tokenRecord.IsActive)
        {
            tokenRecord.RevokedAt = DateTime.UtcNow;
            tokenRecord.RevokedReason = reason;
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Revoked refresh token for user: {UserId}, Reason: {Reason}", 
                tokenRecord.UserId, reason);
        }
    }

    public async Task RevokeAllRefreshTokensAsync(Guid userId, string reason)
    {
        var activeTokens = await _dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.IsActive)
            .ToListAsync();

        foreach (var token in activeTokens)
        {
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedReason = reason;
        }

        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Revoked {TokenCount} refresh tokens for user: {UserId}, Reason: {Reason}", 
            activeTokens.Count, userId, reason);
    }
}
