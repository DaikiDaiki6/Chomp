using System;
using System.Linq.Expressions;
using UserService.DTO;
using UserService.Models;

namespace UserService.Services.Helpers;

public static class UserProjections
{
    public static readonly Expression<Func<User, GetUserDto>> ToGetUserDto = user => new GetUserDto(
            user.UserId,
            user.Username,
            user.FirstName,
            user.LastName,
            user.Email,
            user.ContactNo,
            user.Role,
            user.AccountStatus,
            user.CreatedAt,
            user.UpdatedAt,
            user.LastSignIn
        );
}
