using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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
    private readonly GetPrincipalFromExpiredToken _expiredTokenHelper;
    public TokenService(UsersDbContext dbContext,
        IConfiguration configuration,
        ILogger<TokenService> logger,
        GetPrincipalFromExpiredToken expiredTokenHelper)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _logger = logger;
        _expiredTokenHelper = expiredTokenHelper;
    }

    public string GenerateJwtToken(string username, Guid userId, Roles role, AccountStatus accountStatus)
    {
        var jwtConfig = _configuration.GetSection("Jwt");
        var jwtKey = jwtConfig["Key"] ?? throw new InvalidOperationException("Jwt key is missing from configuration.");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]{
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new Claim(JwtRegisteredClaimNames.UniqueName, username),
            new Claim(ClaimTypes.Role, role.ToString()),
            new Claim("username", username),
            new Claim("AccountStatus", accountStatus.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwtConfig["issuer"],
            audience: jwtConfig["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(jwtConfig["ExpiryMinutes"])),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
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

    public async Task<AuthResponseDto> RefreshTokenAsync(string expiredAccessToken, string refreshToken)
    {
        try
        {
            // STEP 1: Extract user info from expired JWT token
            // Even though JWT is expired, we can still read the claims inside it (like reading an expired ID card)
            var principal = _expiredTokenHelper.GetUserInfoFromExpiredToken(expiredAccessToken);
            if (principal == null)
            {
                _logger.LogWarning("Invalid expired JWT token format in refresh request");
                throw new UnauthorizedAccessException("Invalid token format");
            }

            // STEP 2: Get the User ID from the expired JWT
            // This tells us which user originally owned this token
            var jwtUserIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(jwtUserIdClaim) || !Guid.TryParse(jwtUserIdClaim, out var jwtUserId))
            {
                _logger.LogWarning("Invalid or missing user ID in expired JWT");
                throw new UnauthorizedAccessException("Invalid user ID in token");
            }

            // STEP 3: Validate the refresh token and get current user info
            // This checks if the refresh token is still valid in our database
            var user = await ValidateRefreshTokenAsync(refreshToken);

            // STEP 4: SECURITY CHECK - Ensure both tokens belong to the same user
            // This prevents someone from using User A's refresh token with User B's expired JWT
            if (user.UserId != jwtUserId)
            {
                _logger.LogWarning("Token mismatch: JWT user {JwtUserID} does not match refresh token {RefreshToken}",
                    jwtUserId, user.UserId);
                throw new UnauthorizedAccessException("Token mismatch - potential security breach");
            }

            // STEP 5: Additional timeline security check (prevents replay attacks)
            // Ensure the JWT was created AFTER the refresh token (not before)
            var jwtIssuedAtClaim = principal.FindFirst(JwtRegisteredClaimNames.Iat)?.Value;
            if (!string.IsNullOrEmpty(jwtIssuedAtClaim) && long.TryParse(jwtIssuedAtClaim, out var jwtIssuedAt))
            {
                // Convert Unix timestamp to DateTime
                var jwtIssuedTime = DateTimeOffset.FromUnixTimeSeconds(jwtIssuedAt).DateTime;

                // Get the refresh token record from database
                var refreshTokenRecord = await _dbContext.RefreshTokens
                    .FirstOrDefaultAsync(rt => rt.RefreshTokenValue == refreshToken);

                // Check if JWT was issued before refresh token was created (suspicious!)
                if (refreshTokenRecord != null && jwtIssuedTime < refreshTokenRecord.CreatedAt.AddMinutes(-1))
                {
                    _logger.LogWarning("JWT token issued before refresh token creation - possible replay attack");
                    throw new UnauthorizedAccessException("Invalid token timeline");
                }
            }

            // STEP 6: Generate brand new tokens (both JWT and refresh token)
            // Create fresh JWT token (valid for 15 minutes)
            var newJwtToken = GenerateJwtToken(user.Username, user.UserId, user.Role, user.AccountStatus);
            
            // Create fresh refresh token (valid for 7 days)
            var newRefreshToken = await GenerateRefreshTokenAsync(user.UserId);
            
            // Revoke the old refresh token so it can't be used again (security best practice)
            await RevokeRefreshTokenAsync(refreshToken, "Used for refresh");

            _logger.LogInformation("Token refreshed successfully for user: {Username}", user.Username);

            // STEP 7: Return the new tokens to the client
            return new AuthResponseDto(
                Token: newJwtToken,        // New JWT token (15 minutes)
                RefreshToken: newRefreshToken, // New refresh token (7 days)
                ExpiresAt: DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["Jwt:ExpiryMinutes"] ?? "15")),
                User: new { user.Username, user.Role }
            );
        }
        catch (UnauthorizedAccessException)
        {
            // Re-throw authorization exceptions as-is (these are expected security failures)
            throw;
        }
        catch (Exception ex)
        {
            // Log unexpected errors and convert to authorization exception for security
            // (Don't expose internal error details to potential attackers)
            _logger.LogError(ex, "Unexpected error during token refresh");
            throw new UnauthorizedAccessException("Token refresh failed");
        }
    }

    public async Task RevokeAllRefreshTokenAsync(Guid userId, string reason)
    {
        var activeTokens = await _dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null && DateTime.UtcNow < rt.ExpiresAt)
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
            .Include(rt => rt.User)
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
