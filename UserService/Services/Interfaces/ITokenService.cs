using System;
using UserService.DTO;

namespace UserService.Services.Interfaces;

public interface ITokenService
{
    Task<string> GenerateRefreshTokenAsync(Guid userId);
    Task<GetUserDto> ValidateRefreshTokenAsync(string refreshToken);
    Task RevokeRefreshTokenAsync(string refreshToken, string reason);
    Task RevokeAllRefreshTokenAsync(Guid userId, string reason);
}
