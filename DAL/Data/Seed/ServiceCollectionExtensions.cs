using DAL.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DAL.Seed;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Gọi từ Web startup: builder.Services.AddDal(builder.Configuration);
    /// </summary>
    public static IServiceCollection AddDal(this IServiceCollection services, IConfiguration configuration)
    {
        var conn = configuration.GetConnectionString("DefaultConnection")
                   ?? throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection");

        services.AddDbContext<AppDbContext>(opt =>
        {
            opt.UseSqlServer(conn, sql =>
            {
                // đảm bảo migrations nằm trong DAL/Migrations

                sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);

                
            });
        });

        services.AddIdentity<IdentityUser, IdentityRole>(options =>
        {
            // giữ policy đủ mạnh (phù hợp password demo bên trên)
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 8;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        // tự migrate + seed roles/users khi web start
        services.AddHostedService<IdentitySeedHostedService>();

        return services;
    }
}