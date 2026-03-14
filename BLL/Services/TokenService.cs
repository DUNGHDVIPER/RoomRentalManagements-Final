using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BLL.Services.Interfaces;
using BLL.DTOs.Auth; // ADD THIS

namespace BLL.Services;

public class TokenService(
    UserManager<IdentityUser> userManager,
    IConfiguration configuration,
    ILogger<TokenService> logger) : ITokenService
{
    private readonly UserManager<IdentityUser> _userManager = userManager;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<TokenService> _logger = logger;
    private const string REFRESH_TOKEN_PURPOSE = "RefreshToken";

    public async Task<string> GenerateJwtTokenAsync(IdentityUser user, IList<string> roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Name, user.UserName!),
            new("jti", Guid.NewGuid().ToString()),
            new("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = Encoding.UTF8.GetBytes(_configuration["JWT:SecretKey"] ?? "your-super-secret-key-must-be-at-least-32-characters-long");
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(24),
            Issuer = _configuration["JWT:Issuer"] ?? "WebHostRazor",
            Audience = _configuration["JWT:Audience"] ?? "RoomRentalUsers",
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["JWT:SecretKey"] ?? "your-super-secret-key-must-be-at-least-32-characters-long");

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _configuration["JWT:Issuer"] ?? "WebHostRazor",
                ValidateAudience = true,
                ValidAudience = _configuration["JWT:Audience"] ?? "RoomRentalUsers",
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            return principal;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            return null;
        }
    }

    public async Task<TokenInfoDto> GetTokenInfoAsync(string token) // CHANGED to TokenInfoDto
    {
        var principal = ValidateToken(token);
        if (principal == null)
            return new TokenInfoDto { IsValid = false };

        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = principal.FindFirst(ClaimTypes.Email)?.Value;
        var roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(email))
            return new TokenInfoDto { IsValid = false };

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return new TokenInfoDto { IsValid = false };

        return new TokenInfoDto // CHANGED to TokenInfoDto
        {
            IsValid = true,
            UserId = userId,
            Email = email,
            Roles = roles,
            User = user
        };
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[32];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public async Task SaveRefreshTokenAsync(string userId, string refreshToken)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogError("User not found for ID: {UserId}", userId);
                return;
            }

            // Remove old refresh token
            await _userManager.RemoveAuthenticationTokenAsync(user, "Default", REFRESH_TOKEN_PURPOSE);

            // Save new refresh token to AspNetUserTokens
            await _userManager.SetAuthenticationTokenAsync(user, "Default", REFRESH_TOKEN_PURPOSE, refreshToken);

            _logger.LogInformation("Refresh token saved for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving refresh token for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> ValidateRefreshTokenAsync(string userId, string refreshToken)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return false;

            var storedToken = await _userManager.GetAuthenticationTokenAsync(user, "Default", REFRESH_TOKEN_PURPOSE);
            return !string.IsNullOrEmpty(storedToken) && storedToken == refreshToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating refresh token for user {UserId}", userId);
            return false;
        }
    }

    public async Task RevokeRefreshTokenAsync(string userId, string refreshToken)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                await _userManager.RemoveAuthenticationTokenAsync(user, "Default", REFRESH_TOKEN_PURPOSE);
                _logger.LogInformation("Refresh token revoked for user {UserId}", userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking refresh token for user {UserId}", userId);
            throw;
        }
    }
}