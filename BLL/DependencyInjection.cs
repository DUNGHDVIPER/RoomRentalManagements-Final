using BLL.ApiClients;
using BLL.Services;
using BLL.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BLL;

public static class DependencyInjection
{
    public static IServiceCollection AddBll(this IServiceCollection services, IConfiguration config)
    {
        services.AddHttpClient<MockApiClient>(client =>
        {
            client.BaseAddress = new Uri("https://localhost:5009/mock-api");
        });

        // Register services
        services.AddScoped<IRoomService, RoomService>();
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }

    // Keep the old method for backward compatibility if needed
    public static IServiceCollection AddBLL(
       this IServiceCollection services,
       IConfiguration configuration)
    {
        var baseUrl = configuration["ApiSettings:BaseUrl"];

        services.AddHttpClient<IRoomService, RoomService>(client =>
        {
            client.BaseAddress = new Uri(baseUrl!);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        // Register other services
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}