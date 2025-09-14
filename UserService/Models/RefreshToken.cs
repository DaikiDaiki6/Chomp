using System;
using System.ComponentModel.DataAnnotations;

namespace UserService.Models;

public class RefreshToken
{
    [Key]
    public Guid RefreshTokenId { get; set; }
    [Required]
    [MaxLength(500)]
    public string RefreshTokenValue { get; set; } = string.Empty;
    [Required]
    public DateTime ExpiresAt { get; set; }
    [Required]
    public DateTime CreatedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    [MaxLength(200)]
    public string? RevokedReason { get; set; }
    public bool IsActive => RevokedAt == null && DateTime.UtcNow < ExpiresAt;

    // Navigation Property
    [Required]
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

}
