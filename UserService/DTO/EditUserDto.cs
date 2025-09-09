using System;
using UserService.Models;

namespace UserService.DTO;

public record EditUserDto
(
    string Username,
    string Password,
    string FirstName,
    string LastName,
    string Email,
    string ContactNo
);
