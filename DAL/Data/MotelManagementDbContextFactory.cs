using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace DAL.Data;

public class MotelManagementDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();
        var webHostPath = Path.GetFullPath(Path.Combine(basePath, "..", "WebHostRazor"));
        if (!Directory.Exists(webHostPath))
            webHostPath = Path.GetFullPath(Path.Combine(basePath, "WebHostRazor"));

        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.Exists(webHostPath) ? webHostPath : basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var cs =
            config.GetConnectionString("DefaultConnection")
            ?? config.GetConnectionString("MotelManagementDB")
            ?? config["ConnectionStrings:DefaultConnection"]
            ?? config["ConnectionStrings:MotelManagementDB"];

        if (string.IsNullOrWhiteSpace(cs))
            cs = "Server=ADMIN\\SQL2022;Database=MotelManagementDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True";

        var opt = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(cs)
            .Options;

        return new AppDbContext(opt);
    }
}