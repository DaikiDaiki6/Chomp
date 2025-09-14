using System;
using System.ComponentModel.DataAnnotations;

namespace UserService.DTO;

public record RefreshTokenDto
(
    string Token,
    string RefreshToken
);
