using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UserService.DTO;
using UserService.Services.Helpers;
using UserService.Services.Interfaces;

namespace UserService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;
        private readonly IAuthService _authService;
        private readonly ITokenService _tokenService;

        public AuthController(IConfiguration configuration,
            ILogger<AuthController> logger,
            IAuthService authService,
            ITokenService tokenService)
        {
            _configuration = configuration;
            _logger = logger;
            _authService = authService;
            _tokenService = tokenService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto login)
        {
            try
            {
                var user = await _authService.ValidateUserCredentialAsync(login.Username, login.Password);
                var token = _tokenService.GenerateJwtToken(user.Username, user.UserId, user.Role, user.AccountStatus);
                var refreshToken = await _tokenService.GenerateRefreshTokenAsync(user.UserId);

                _logger.LogInformation("User {username} logged in successfully", login.Username);

                return Ok(new AuthResponseDto(
                    Token: token,
                    RefreshToken: refreshToken,
                    ExpiresAt: DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["Jwt:ExpiryMinutes"] ?? "15")),
                    User: new { user.Username, user.Role }
                ));
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning("Failed login attempt for username: {Username}", login.Username);
                return Unauthorized(new { errorMessage = "Invalid username or Password." });
            }
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken(RefreshTokenDto refreshDto)
        {
            try
            {
                var response = await _tokenService.RefreshTokenAsync(refreshDto.AccessToken, refreshDto.RefreshToken);
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Refresh token validation failed: {Message}", ex.Message);
                return Unauthorized(new { errorMessage = "Invalid refresh token" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                return Unauthorized(new { errorMessage = "Token refresh failed." });
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var userIdClaim = User.FindFirst("userId")?.Value;
                var username = User.FindFirst("username")?.Value ?? User.Identity?.Name;

                if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var userId))
                {
                    await _tokenService.RevokeAllRefreshTokenAsync(userId, "User logout");

                    try
                    {
                        await _authService.UpdateLastSignInAsync(userId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to update LastSigIn for user {UserId}", userId);
                    }
                }
                _logger.LogInformation("User logged out successfully: {Username}", username ?? "Unknown");

                return Ok(new
                {
                    message = "Logged out successfully - all refresh tokens revoked",
                    timestamp = DateTime.UtcNow,
                    instruction = "Please delete stored JWT and refresh tokens from client"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return Ok(new { message = "Logged out" });
            }
        }
    }
}
