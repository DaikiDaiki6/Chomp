using System;
using Microsoft.EntityFrameworkCore;
using UserService.Data;
using UserService.DTO;
using UserService.Models;
using UserService.Services.Helpers;
using UserService.Services.Interfaces;

namespace UserService.Services;

public class AuthService : IAuthService
{
    private readonly UsersDbContext _dbContext;
    private readonly ILogger<AuthService> _logger;

    public AuthService(UsersDbContext dbContext,
        ILogger<AuthService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }
    public async Task UpdateLastSignInAsync(Guid userId)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.UserId == userId);
        if (user != null)
        {
            user.LastSignIn = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Updated LastSigIn for user: {UserId}", userId);
        }
    }

    public async Task<GetUserDto> ValidateUserCredentialAsync(string username, string password)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Username == username && !u.IsDeleted);

        if (user is null)
        {
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        if (!BCrypt.Net.BCrypt.Verify(password, user.Password))
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        if (user.AccountStatus != AccountStatus.Active)
        {
            throw new UnauthorizedAccessException("Account is not active.");
        }

        return UserProjections.ToGetUserDto.Compile()(user);
    }
}
