using System;
using System.ComponentModel.DataAnnotations;

namespace UserService.DTO;

public record RefreshTokenDto
(
    string AccessToken,
    string RefreshToken
);
