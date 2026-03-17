namespace WebCustomerBlazor.Services
{
    public interface ICookieService
    {
        Task<string?> GetCookieAsync(string name);
        Task SetCookieAsync(string name, string value, int days = 1);
        Task DeleteCookieAsync(string name);
        Task<bool> CookieExistsAsync(string name);
        Task<Dictionary<string, string>> GetAllCookiesAsync();
    }
}