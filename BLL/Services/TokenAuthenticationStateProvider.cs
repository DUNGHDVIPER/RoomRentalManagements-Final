using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using BLL.Services.Interfaces;
using Microsoft.JSInterop;

namespace BLL.Services;

public class TokenAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly ITokenService _tokenService;
    private readonly ILogger<TokenAuthenticationStateProvider> _logger;
    private readonly IJSRuntime _jsRuntime;

    public TokenAuthenticationStateProvider(ITokenService tokenService, ILogger<TokenAuthenticationStateProvider> logger, IJSRuntime jsRuntime)
    {
        _tokenService = tokenService;
        _logger = logger;
        _jsRuntime = jsRuntime;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var token = await GetTokenFromStorageAsync();

            if (string.IsNullOrEmpty(token))
            {
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            var tokenInfo = await _tokenService.GetTokenInfoAsync(token);

            if (!tokenInfo.IsValid)
            {
                await RemoveTokenAsync();
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, tokenInfo.UserId),
                new(ClaimTypes.Email, tokenInfo.Email),
                new(ClaimTypes.Name, tokenInfo.Email)
            };

            foreach (var role in tokenInfo.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var identity = new ClaimsIdentity(claims, "jwt");
            var user = new ClaimsPrincipal(identity);

            _logger.LogInformation("User authenticated: {Email} with roles: {Roles}",
                tokenInfo.Email, string.Join(",", tokenInfo.Roles));

            return new AuthenticationState(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in authentication state");
            await RemoveTokenAsync();
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
    }

    public async Task NotifyUserAuthenticationAsync(string token)
    {
        var success = await SetTokenAsync(token);
        if (success)
        {
            var authState = await GetAuthenticationStateAsync();
            NotifyAuthenticationStateChanged(Task.FromResult(authState));
        }
    }

    public async Task NotifyUserLogoutAsync()
    {
        await RemoveTokenAsync();
        var anonymous = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        NotifyAuthenticationStateChanged(Task.FromResult(anonymous));
    }

    // Helper methods for token management using localStorage
    private async Task<string?> GetTokenFromStorageAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authToken");
        }
        catch
        {
            return null;
        }
    }

    private async Task<bool> SetTokenAsync(string token)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "authToken", token);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save token to localStorage");
            return false;
        }
    }

    private async Task RemoveTokenAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "authToken");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove token from localStorage");
        }
    }
}