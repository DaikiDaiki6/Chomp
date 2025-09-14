using System;

namespace UserService.Models;

public enum Roles
{
    User,
    Admin
}
public enum AccountStatus
{
    Active,
    Banned,
    PendingDeletion,
    Deleted
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
    public AccountStatus AccountStatus { get; set; } = AccountStatus.Active;

    // Soft Delete properties
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public DateTime? PermanentDeletionAt { get; set; }
    public string? DeletionReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastSignIn { get; set; } = DateTime.UtcNow;

    // Navigation Property
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
