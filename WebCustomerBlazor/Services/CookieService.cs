using Microsoft.JSInterop;

namespace WebCustomerBlazor.Services
{
    public class CookieService : ICookieService
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly ILogger<CookieService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private bool _jsInitialized = false;

        public CookieService(IJSRuntime jsRuntime, ILogger<CookieService> logger, IHttpContextAccessor httpContextAccessor)
        {
            _jsRuntime = jsRuntime;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        private async Task<bool> TryEnsureJavaScriptInitializedAsync()
        {
            if (_jsInitialized) return true;

            try
            {
                await _jsRuntime.InvokeVoidAsync("eval", @"
                    window.cookieHelper = {
                        getCookie: function(name) {
                            const value = '; ' + document.cookie;
                            const parts = value.split('; ' + name + '=');
                            if (parts.length === 2) return decodeURIComponent(parts.pop().split(';').shift());
                            return null;
                        },
                        setCookie: function(name, value, days) {
                            try {
                                const expires = new Date();
                                expires.setTime(expires.getTime() + (days * 24 * 60 * 60 * 1000));
                                const isLocalhost = window.location.hostname === 'localhost';
                                const secureAttribute = (!isLocalhost && window.location.protocol === 'https:') ? '; Secure' : '';
                                
                                const cookieString = `${name}=${encodeURIComponent(value)}; expires=${expires.toUTCString()}; path=/; SameSite=Lax${secureAttribute}`;
                                document.cookie = cookieString;
                                
                                console.log('Cookie set:', name, 'Value length:', value.length);
                                return true;
                            } catch (error) {
                                console.error('Error setting cookie', name, ':', error);
                                return false;
                            }
                        },
                        deleteCookie: function(name) {
                            document.cookie = name + '=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/; SameSite=Lax';
                        },
                        cookieExists: function(name) {
                            return document.cookie.split(';').some(c => c.trim().startsWith(name + '='));
                        },
                        getAllCookies: function() {
                            const cookies = {};
                            document.cookie.split(';').forEach(cookie => {
                                const parts = cookie.trim().split('=');
                                if (parts.length === 2) {
                                    cookies[parts[0]] = decodeURIComponent(parts[1]);
                                }
                            });
                            return cookies;
                        }
                    };
                ");
                _jsInitialized = true;
                return true;
            }
            catch (InvalidOperationException)
            {
                // JavaScript interop not available during static rendering
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize JavaScript cookie helper");
                return false;
            }
        }

        private string? GetServerSideCookie(string name)
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                return httpContext?.Request.Cookies[name];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting server-side cookie {CookieName}", name);
                return null;
            }
        }

        public async Task<string?> GetCookieAsync(string name)
        {
            try
            {
                // Try client-side first if JavaScript is available
                if (await TryEnsureJavaScriptInitializedAsync())
                {
                    return await _jsRuntime.InvokeAsync<string?>("cookieHelper.getCookie", name);
                }
                
                // Fallback to server-side cookies
                return GetServerSideCookie(name);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting cookie {CookieName}, trying server-side fallback", name);
                return GetServerSideCookie(name);
            }
        }

        public async Task SetCookieAsync(string name, string value, int days = 1)
        {
            try
            {
                if (await TryEnsureJavaScriptInitializedAsync())
                {
                    await _jsRuntime.InvokeAsync<bool>("cookieHelper.setCookie", name, value, days);
                }
                else
                {
                    _logger.LogWarning("Cannot set cookie {CookieName} - JavaScript not available", name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting cookie {CookieName}", name);
            }
        }

        public async Task DeleteCookieAsync(string name)
        {
            try
            {
                if (await TryEnsureJavaScriptInitializedAsync())
                {
                    await _jsRuntime.InvokeVoidAsync("cookieHelper.deleteCookie", name);
                }
                else
                {
                    _logger.LogWarning("Cannot delete cookie {CookieName} - JavaScript not available", name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting cookie {CookieName}", name);
            }
        }

        public async Task<bool> CookieExistsAsync(string name)
        {
            try
            {
                if (await TryEnsureJavaScriptInitializedAsync())
                {
                    return await _jsRuntime.InvokeAsync<bool>("cookieHelper.cookieExists", name);
                }
                
                // Fallback to server-side check
                return !string.IsNullOrEmpty(GetServerSideCookie(name));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking cookie existence {CookieName}", name);
                return false;
            }
        }

        public async Task<Dictionary<string, string>> GetAllCookiesAsync()
        {
            try
            {
                if (await TryEnsureJavaScriptInitializedAsync())
                {
                    return await _jsRuntime.InvokeAsync<Dictionary<string, string>>("cookieHelper.getAllCookies");
                }
                
                // Fallback to server-side cookies
                var httpContext = _httpContextAccessor.HttpContext;
                var result = new Dictionary<string, string>();
                if (httpContext != null)
                {
                    foreach (var cookie in httpContext.Request.Cookies)
                    {
                        result[cookie.Key] = cookie.Value;
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all cookies");
                return new Dictionary<string, string>();
            }
        }
    }
}