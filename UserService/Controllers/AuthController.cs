using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using UserService.DTO;
using UserService.Models;
using UserService.Services.Interfaces;

namespace UserService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly ILogger<AuthController> _logger;
        private readonly IUserService _userService;
        public AuthController(IConfiguration config, ILogger<AuthController> logger, IUserService userService)
        {
            _config = config;
            _logger = logger;
            _userService = userService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> LoginAsync([FromBody] LoginDto login)
        {
            try
            {
                var user = await _userService.ValidateUserCredentialAsync(login.Username, login.Password);

                // Generate JWT token (short-lived)
                var token = GenerateJwtToken(user.Username, user.UserId, user.Role, user.AccountStatus);
                
                // Generate refresh token (long-lived)
                var refreshToken = await _userService.GenerateRefreshTokenAsync(user.UserId);

                _logger.LogInformation("User {Username} logged in successfully", login.Username);

                return Ok(new AuthResponseDto(
                    Token: token,
                    RefreshToken: refreshToken,
                    ExpiresAt: DateTime.UtcNow.AddMinutes(Convert.ToDouble(_config["Jwt:ExpiryMinutes"] ?? "15")),
                    User: new { user.Username, user.Role }
                ));
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning("Failed login attempt for username: {Username}", login.Username);
                return Unauthorized(new { errorMessage = "Invalid username or password" });
            }
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken(RefreshTokenDto refreshDto)
        {
            try
            {
                // Validate refresh token from database
                var user = await _userService.ValidateRefreshTokenAsync(refreshDto.RefreshToken);
                
                // Generate new JWT token
                var newJwtToken = GenerateJwtToken(user.Username, user.UserId, user.Role, user.AccountStatus);
                
                // Generate new refresh token and revoke old one
                var newRefreshToken = await _userService.GenerateRefreshTokenAsync(user.UserId);
                await _userService.RevokeRefreshTokenAsync(refreshDto.RefreshToken, "Used for refresh");

                _logger.LogInformation("Token refreshed successfully for user: {Username}", user.Username);
                
                return Ok(new AuthResponseDto(
                    Token: newJwtToken,
                    RefreshToken: newRefreshToken,
                    ExpiresAt: DateTime.UtcNow.AddMinutes(Convert.ToDouble(_config["Jwt:ExpiryMinutes"] ?? "15")),
                    User: new { user.Username, user.Role }
                ));
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Refresh token validation failed: {Message}", ex.Message);
                return Unauthorized(new { errorMessage = "Invalid refresh token" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                return Unauthorized(new { errorMessage = "Token refresh failed" });
            }
        }

        [HttpPost("logout")]
        [Authorize] // Requires valid JWT
        public async Task<IActionResult> Logout()
        {
            try
            {
                var userIdClaim = User.FindFirst("userId")?.Value;
                var username = User.FindFirst("username")?.Value ?? User.Identity?.Name;

                if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var userId))
                {
                    // Revoke all refresh tokens for this user (proper logout)
                    await _userService.RevokeAllRefreshTokensAsync(userId, "User logout");
                    
                    // Update LastSignIn timestamp
                    try
                    {
                        await _userService.UpdateLastSignInAsync(userId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to update LastSignIn for user {UserId}", userId);
                        // Don't fail logout if this fails
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
                return Ok(new { message = "Logged out" }); // Still return success even if logging fails
            }
        }

        // Helper method to extract user info from expired tokens
        private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            try
            {
                var jwtConfig = _config.GetSection("Jwt");
                var jwtKey = jwtConfig["Key"];

                if (string.IsNullOrEmpty(jwtKey))
                {
                    return null;
                }
                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    ValidateIssuer = true,
                    ValidIssuer = jwtConfig["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = jwtConfig["Audience"],
                    ValidateLifetime = false, // Allow expired tokens for refresh
                    ClockSkew = TimeSpan.Zero
                };

                var tokenHandler = new JwtSecurityTokenHandler();
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken validatedToken);

                if (validatedToken is not JwtSecurityToken jwtSecurityToken ||
                    !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    return null;
                }
                return principal;
            }
            catch
            {
                return null;
            }
        }

        private string GenerateJwtToken(string username, Guid userId, Roles role, AccountStatus accountStatus)
        {
            var jwtConfig = _config.GetSection("Jwt");
            var jwtKey = jwtConfig["Key"] ?? string.Empty;
            if (string.IsNullOrEmpty(jwtKey))
            {
                _logger.LogWarning("UserService - Jwt Key is missing from configuration");
                throw new InvalidOperationException("Jwt Key is missing from configuration");
            }
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]{
                new Claim(JwtRegisteredClaimNames.Sub, username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role.ToString()),
                new Claim("username", username),
                new Claim("AccountStatus", accountStatus.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: jwtConfig["Issuer"],
                audience: jwtConfig["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(jwtConfig["ExpiryMinutes"])),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
