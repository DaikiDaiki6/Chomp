using System;

namespace UserService.Models;

public enum Roles
{
    User,
    Admin
}
public class User
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string ContactNo { get; set; } = string.Empty;
    public Roles Role { get; set; } = Roles.User;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastSignIn { get; set; } = DateTime.UtcNow;
}
