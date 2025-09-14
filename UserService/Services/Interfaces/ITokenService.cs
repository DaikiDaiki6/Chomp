using System;
using UserService.DTO;
using UserService.Models;

namespace UserService.Services.Interfaces;

public interface ITokenService
{
    Task<string> GenerateRefreshTokenAsync(Guid userId);
    Task<GetUserDto> ValidateRefreshTokenAsync(string refreshToken);
    Task RevokeRefreshTokenAsync(string refreshToken, string reason);
    Task RevokeAllRefreshTokenAsync(Guid userId, string reason);

    Task<AuthResponseDto> RefreshTokenAsync(string expiredAccessToken, string refreshToken);
    string GenerateJwtToken(string username, Guid userId, Roles role, AccountStatus accountStatus);
}
