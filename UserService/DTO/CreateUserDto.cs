using System;
using UserService.Models;

namespace UserService.DTO;

public record CreateUserDto
(
    Guid UserId,
    string Username,
    string Password,
    string FirstName,
    string LastName,
    string Email,
    string ContactNo,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? LastSignIn
);
