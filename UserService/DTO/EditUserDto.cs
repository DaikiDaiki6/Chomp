using System;
using System.ComponentModel.DataAnnotations;
using UserService.Models;

namespace UserService.DTO;

public record EditUserDto
(
    [StringLength(128, MinimumLength = 6, ErrorMessage = "Username must be between 6 and 128 characters.")]
    string? Username,
    [StringLength(128, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 128 characters.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{8,}$", ErrorMessage ="Password must contain at least one uppercase, one lowercase, one number, and one special character.")]
    string? Password,
    [StringLength(128, MinimumLength = 1, ErrorMessage = "First name must be between 1 and 128 characters.")]
    [RegularExpression(@"^[a-zA-Z\s'-]+$", ErrorMessage = "First name can only contain letters, spaces, apostrophes, and hyphens.")]
    string? FirstName,
    [StringLength(128, MinimumLength = 1, ErrorMessage = "Last name must be between 1 and 128 characters.")]
    [RegularExpression(@"^[a-zA-Z\s'-]+$", ErrorMessage = "Last name can only contain letters, spaces, apostrophes, and hyphens.")]
    string? LastName,
    [EmailAddress(ErrorMessage = "Invalid email address format.")]
    string? Email,
    [RegularExpression(@"^(\+63|0)9\d{9}$", ErrorMessage = "Contact number must be a valid Philippine mobile number (e.g., +639123456789 or 09123456789).")]
    string? ContactNo
);
