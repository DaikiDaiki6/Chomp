using System;
using UserService.DTO;

namespace UserService.Services.Interfaces;

public interface IAuthService
{
    Task<GetUserDto> ValidateUserCredentialAsync(string username, string password);
    Task UpdateLastSignInAsync(Guid userId);
}
