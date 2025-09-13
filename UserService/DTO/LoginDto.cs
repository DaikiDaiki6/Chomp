using System.ComponentModel.DataAnnotations;

namespace UserService.DTO;

public record LoginDto
(
    [Required(ErrorMessage = "Username is required.")]
    [StringLength(128, MinimumLength = 6, ErrorMessage = "Username must be between 6 and 128 characters.")]
    string Username,
    [Required(ErrorMessage = "Password is required.")]
    [StringLength(128, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 128 characters.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{8,}$", ErrorMessage ="Password must contain at least one uppercase, one lowercase, one number, and one special character.")]
    string Password
);
