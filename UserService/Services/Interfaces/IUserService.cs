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
    Task DeleteUserAsync(Guid id); // Hard Delete (admin only)

    // Soft Delete
    Task<GetUserDto> SoftDeleteUserAsync(Guid id, string? reason = null);
    Task<GetUserDto> RestoreUserAsync(Guid id);
    Task<List<GetUserDto>> GetSoftDeletedUsersAsync();
}
