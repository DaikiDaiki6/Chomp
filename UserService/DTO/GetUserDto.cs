using System;
using UserService.Models;

namespace UserService.DTO;

public record GetUserDto
(
    Guid UserId,
    string Username,
    string FirstName,
    string LastName,
    string Email,
    string ContactNo,
    Roles Role,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime LastSignIn
);
