namespace UserService.DTO;

public record AuthResponseDto
(
    string Token, 
    string RefreshToken,
    DateTime ExpiresAt,
    object User
);
