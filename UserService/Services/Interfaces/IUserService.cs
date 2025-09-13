using System;
using UserService.DTO;
using UserService.Models;

namespace UserService.Services.Interfaces;

public interface IUserService
{
    Task<List<GetUserDto>> GetAllAsync();
    Task<GetUserDto> GetUserByIdAsync(Guid id);
    Task<GetUserDto> CreateUserAsync(CreateUserDto dto);
    Task<GetUserDto> EditUserInfoAsync(Guid id, EditUserDto dto);
    Task DeleteUserAsync(Guid id);
    
    // Soft Delete Methods
    Task<GetUserDto> SoftDeleteUserAsync(Guid id, string? reason = null);
    Task<GetUserDto> RestoreUserAsync(Guid id);
    Task<List<GetUserDto>> GetDeletedUsersAsync();
    
    Task<GetUserDto> ValidateUserCredentialAsync(string username, string password);
    Task UpdateLastSignInAsync(Guid userId);
    Task<string> GenerateRefreshTokenAsync(Guid userId);
    Task<GetUserDto> ValidateRefreshTokenAsync(string refreshToken);
    Task RevokeRefreshTokenAsync(string refreshToken, string reason);
    Task RevokeAllRefreshTokensAsync(Guid userId, string reason);
}
