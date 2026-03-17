using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.Text.Json;
using WebCustomerBlazor.Services;

namespace WebCustomerBlazor.Services
{
    public class CookieAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly ICookieService _cookieService;
        private readonly ILogger<CookieAuthenticationStateProvider> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CookieAuthenticationStateProvider(
            ICookieService cookieService,
            ILogger<CookieAuthenticationStateProvider> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _cookieService = cookieService;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                // Try to get token from cookies (client-side)
                string? token = await _cookieService.GetCookieAsync("AuthToken");
                string? userEmail = await _cookieService.GetCookieAsync("UserEmail");
                string? userId = await _cookieService.GetCookieAsync("UserId");

                // Fallback to server-side cookies if client-side fails
                if (string.IsNullOrEmpty(token))
                {
                    var httpContext = _httpContextAccessor.HttpContext;
                    if (httpContext != null)
                    {
                        token = httpContext.Request.Cookies["AuthToken"];
                        userEmail = httpContext.Request.Cookies["UserEmail"];
                        userId = httpContext.Request.Cookies["UserId"];
                    }
                }

                if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(userId))
                {
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }

                var claims = ExtractClaimsFromJwt(token);
                if (claims == null || !claims.Any())
                {
                    // Create basic claims from cookie data
                    claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, userId),
                        new Claim(ClaimTypes.Name, userEmail ?? "User"),
                        new Claim(ClaimTypes.Email, userEmail ?? ""),
                        new Claim(ClaimTypes.Role, "User")
                    };
                }

                var identity = new ClaimsIdentity(claims, "jwt");
                var user = new ClaimsPrincipal(identity);

                return new AuthenticationState(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting authentication state");
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }
        }

        private List<Claim>? ExtractClaimsFromJwt(string jwt)
        {
            try
            {
                var parts = jwt.Split('.');
                if (parts.Length != 3)
                    return null;

                var payload = parts[1];
                
                // Add padding if needed
                switch (payload.Length % 4)
                {
                    case 2: payload += "=="; break;
                    case 3: payload += "="; break;
                }

                var jsonBytes = Convert.FromBase64String(payload);
                var json = System.Text.Encoding.UTF8.GetString(jsonBytes);
                var tokenData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

                if (tokenData == null)
                    return null;

                var claims = new List<Claim>();

                foreach (var kvp in tokenData)
                {
                    switch (kvp.Key)
                    {
                        case "sub":
                        case "nameid":
                            claims.Add(new Claim(ClaimTypes.NameIdentifier, kvp.Value.GetString() ?? ""));
                            break;
                        case "email":
                            claims.Add(new Claim(ClaimTypes.Email, kvp.Value.GetString() ?? ""));
                            claims.Add(new Claim(ClaimTypes.Name, kvp.Value.GetString() ?? ""));
                            break;
                        case "role":
                            if (kvp.Value.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var role in kvp.Value.EnumerateArray())
                                {
                                    claims.Add(new Claim(ClaimTypes.Role, role.GetString() ?? ""));
                                }
                            }
                            else
                            {
                                claims.Add(new Claim(ClaimTypes.Role, kvp.Value.GetString() ?? ""));
                            }
                            break;
                        case "name":
                            claims.Add(new Claim(ClaimTypes.Name, kvp.Value.GetString() ?? ""));
                            break;
                    }
                }

                return claims;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting claims from JWT");
                return null;
            }
        }

        public void NotifyAuthenticationStateChanged()
        {
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }
    }
}