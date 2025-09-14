using System;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using UserService.Data;
using UserService.DTO;
using UserService.Models;
using UserService.Services.Helpers;
using UserService.Services.Interfaces;

namespace UserService.Services;

public class TokenService : ITokenService
{
    private readonly UsersDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TokenService> _logger;
    public TokenService(UsersDbContext dbContext,
        IConfiguration configuration,
        ILogger<TokenService> logger)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _logger = logger;
    }
    public async Task<string> GenerateRefreshTokenAsync(Guid userId)
    {
        var randomBytes = new byte[32];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        var token = Convert.ToBase64String(randomBytes);

        var refreshToken = new RefreshToken
        {
            RefreshTokenId = Guid.NewGuid(),
            UserId = userId,
            RefreshTokenValue = token,
            ExpiresAt = DateTime.UtcNow.AddDays(_configuration.GetValue<int>("Jwt:RefreshTokenExpiryDays", 7)),
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Generated refresh token for user: {UserId}", userId);
        return token;
    }

    public async Task RevokeAllRefreshTokenAsync(Guid userId, string reason)
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

    public async Task RevokeRefreshTokenAsync(string refreshToken, string reason)
    {
        var tokenRecord = await _dbContext.RefreshTokens
                .FirstOrDefaultAsync(u => u.RefreshTokenValue == refreshToken);

        if (tokenRecord != null && tokenRecord.IsActive)
        {
            tokenRecord.RevokedAt = DateTime.UtcNow;
            tokenRecord.RevokedReason = reason;
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Revoked refresh token for user: {UserId}, Reason: {Reason}", tokenRecord.UserId, reason);
        }
    }

    public async Task<GetUserDto> ValidateRefreshTokenAsync(string refreshToken)
    {
        var tokenRecord = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(u => u.RefreshTokenValue == refreshToken);

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
        return UserProjections.ToGetUserDto.Compile()(tokenRecord.User);
    }

}
