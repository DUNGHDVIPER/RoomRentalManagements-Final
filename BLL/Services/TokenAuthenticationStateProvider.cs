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

    private readonly ClaimsPrincipal _anonymous = new(new ClaimsIdentity());
    private ClaimsPrincipal _currentUser;

    public TokenAuthenticationStateProvider(
        ITokenService tokenService,
        ILogger<TokenAuthenticationStateProvider> logger,
        IJSRuntime jsRuntime)
    {
        _tokenService = tokenService;
        _logger = logger;
        _jsRuntime = jsRuntime;
        _currentUser = _anonymous;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            // Nếu đã có user trong memory thì trả về luôn
            if (_currentUser.Identity?.IsAuthenticated == true)
            {
                return new AuthenticationState(_currentUser);
            }

            var token = await GetTokenFromStorageAsync();

            if (string.IsNullOrWhiteSpace(token))
            {
                _currentUser = _anonymous;
                return new AuthenticationState(_anonymous);
            }

            var authState = await BuildAuthenticationStateFromTokenAsync(token, removeInvalidToken: true);
            _currentUser = authState.User;

            return authState;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAuthenticationStateAsync");
            _currentUser = _anonymous;
            return new AuthenticationState(_anonymous);
        }
    }

    public async Task NotifyUserAuthenticationAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            await NotifyUserLogoutAsync();
            return;
        }

        // Lưu token trước
        await SetTokenAsync(token);

        // Quan trọng: build auth state trực tiếp từ token vừa login,
        // không phụ thuộc phải đọc lại từ localStorage
        var authState = await BuildAuthenticationStateFromTokenAsync(token, removeInvalidToken: false);
        _currentUser = authState.User;

        NotifyAuthenticationStateChanged(Task.FromResult(authState));
    }

    public async Task NotifyUserLogoutAsync()
    {
        _currentUser = _anonymous;
        await RemoveTokenAsync();

        var anonymousState = new AuthenticationState(_anonymous);
        NotifyAuthenticationStateChanged(Task.FromResult(anonymousState));
    }

    private async Task<AuthenticationState> BuildAuthenticationStateFromTokenAsync(string token, bool removeInvalidToken)
    {
        try
        {
            var tokenInfo = await _tokenService.GetTokenInfoAsync(token);

            if (tokenInfo == null || !tokenInfo.IsValid)
            {
                if (removeInvalidToken)
                {
                    await RemoveTokenAsync();
                }

                return new AuthenticationState(_anonymous);
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, tokenInfo.UserId ?? string.Empty),
                new(ClaimTypes.Email, tokenInfo.Email ?? string.Empty),
                new(ClaimTypes.Name, tokenInfo.Email ?? string.Empty)
            };

            if (tokenInfo.Roles != null)
            {
                foreach (var role in tokenInfo.Roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }
            }

            var identity = new ClaimsIdentity(claims, "jwt");
            var user = new ClaimsPrincipal(identity);

            _logger.LogInformation(
                "User authenticated: {Email} with roles: {Roles}",
                tokenInfo.Email,
                tokenInfo.Roles != null ? string.Join(",", tokenInfo.Roles) : "");

            return new AuthenticationState(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");

            if (removeInvalidToken)
            {
                await RemoveTokenAsync();
            }

            return new AuthenticationState(_anonymous);
        }
    }

    private async Task<string?> GetTokenFromStorageAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authToken");
        }
        catch (InvalidOperationException)
        {
            // JS chưa sẵn sàng (thường xảy ra lúc prerender)
            return null;
        }
        catch (JSException)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get token from localStorage");
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
        catch (InvalidOperationException)
        {
            // JS chưa sẵn sàng, nhưng memory auth state vẫn đủ để dùng trong session hiện tại
            return false;
        }
        catch (JSException ex)
        {
            _logger.LogError(ex, "Failed to save token to localStorage");
            return false;
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
        catch (InvalidOperationException)
        {
        }
        catch (JSException ex)
        {
            _logger.LogError(ex, "Failed to remove token from localStorage");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove token from localStorage");
        }
    }
}