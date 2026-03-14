using DAL.Data;
using DAL.Repositories.Interfaces;
using DAL.Repositories.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DAL.Repositories;

namespace DAL;

public static class DependencyInjection
{
    public static IServiceCollection AddDal(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var cs = configuration.GetConnectionString("DefaultConnection")
                 ?? throw new InvalidOperationException("Missing DefaultConnection");

        services.AddDbContext<AppDbContext>(opt =>
            opt.UseSqlServer(cs, sql =>
                sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        services.AddScoped<IRoomRepository, RoomRepository>();
        services.AddScoped<IBlockRepository, BlockRepository>();
        services.AddScoped<IFloorRepository, FloorRepository>();

        return services;
    }
}