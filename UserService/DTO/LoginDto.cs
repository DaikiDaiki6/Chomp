using System;
using System.ComponentModel.DataAnnotations;

namespace UserService.DTO;

public record LoginDto
(
    [Required(ErrorMessage = "Username is required.")]
    string Username,
    [Required(ErrorMessage = "Password is required.")]
    string Password
);
