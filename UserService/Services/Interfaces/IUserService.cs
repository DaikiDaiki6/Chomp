using System;
using UserService.DTO;
using UserService.Models;

namespace UserService.Services.Interfaces;

public interface IUserService
{
    Task<List<GetUserDto>> GetAllAsync(); // returns List or [] if no value
    Task<GetUserDto?> GetUserByIdAsync(Guid id); // returns GetUserDto or null
    Task<GetUserDto> CreateUserAsync(CreateUserDto dto); // returns GetUserDto or exception if validation error
    Task<GetUserDto> EditUserInfoAsync(Guid id, EditUserDto dto); // returns GetUserDto or null
    Task<bool> DeleteUserAsync(Guid id); // returns true or false if deleted
}
