using System;
using UserService.Models;

namespace UserService.DTO;

public record CreateUserDto
(
    string Username,
    string Password,
    string FirstName,
    string LastName,
    string Email,
    string ContactNo
);
