using System;
using UserService.DTO;
using UserService.Models;

namespace UserService.Services.Interfaces;

public interface IUserService
{
    Task<List<GetUserDto>> GetAllAsync(); // returns List or throws KeyNotFoundException if no users
    Task<GetUserDto> GetUserByIdAsync(Guid id); // returns GetUserDto or throws KeyNotFoundException
    Task<GetUserDto> CreateUserAsync(CreateUserDto dto); // returns GetUserDto or throws InvalidOperationException
    Task<GetUserDto> EditUserInfoAsync(Guid id, EditUserDto dto); // returns GetUserDto or throws exceptions
    Task DeleteUserAsync(Guid id); // void or throws exceptions
}
