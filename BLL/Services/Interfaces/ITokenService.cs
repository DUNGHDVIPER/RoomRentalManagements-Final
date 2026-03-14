using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using BLL.DTOs.Auth; // ADD THIS

namespace BLL.Services.Interfaces;

public interface ITokenService
{
    Task<string> GenerateJwtTokenAsync(IdentityUser user, IList<string> roles);
    ClaimsPrincipal? ValidateToken(string token);
    Task<TokenInfoDto> GetTokenInfoAsync(string token);  
    string GenerateRefreshToken();
    Task SaveRefreshTokenAsync(string userId, string refreshToken);
    Task<bool> ValidateRefreshTokenAsync(string userId, string refreshToken);
    Task RevokeRefreshTokenAsync(string userId, string refreshToken);
}