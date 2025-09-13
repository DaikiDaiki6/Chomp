namespace UserService.DTO;

public record RefreshTokenDto(
    string Token,
    string RefreshToken
);

public record AuthResponseDto(
    string Token,
    string RefreshToken,
    DateTime ExpiresAt,
    object User
);
