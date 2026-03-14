using BLL.Services;
using BLL.Services.Interfaces;
using DAL.Data;
using DAL.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;

    options.User.RequireUniqueEmail = true;

    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Auth/Login";
    options.AccessDeniedPath = "/Auth/AccessDenied";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(24);
});

builder.Services.AddScoped<CloudinaryService>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<IContractService, ContractService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IBlockService, BlockService>();
builder.Services.AddScoped<IBlockRepository, BlockRepository>();
builder.Services.AddScoped<IFloorService, FloorService>();
builder.Services.AddScoped<IFloorRepository, FloorRepository>();
builder.Services.AddScoped<IAmenityService, AmenityService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IBillingService, BillingService>();
builder.Services.AddScoped<IUtilityService, UtilityService>();
builder.Services.AddScoped<IReportService, ReportService>();

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminOnly", policy => policy.RequireRole("Admin", "SuperAdmin"))
    .AddPolicy("AdminOrHost", policy => policy.RequireRole("Admin", "SuperAdmin", "Host"));

var app = builder.Build();

await InitializeDatabaseAsync(app.Services);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

var sharedUploadsPath = Path.GetFullPath(
    Path.Combine(app.Environment.ContentRootPath, "..", "SharedUploads"));

Directory.CreateDirectory(sharedUploadsPath);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(sharedUploadsPath),
    RequestPath = "/uploads"
});

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();

static async Task InitializeDatabaseAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();

    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        await db.Database.MigrateAsync();

        // KHÔNG seed toàn bộ role/user ở đây nữa.
        // Việc seed chỉ để WebHostRazor xử lý để tránh race condition khi chạy 3 app cùng lúc.

        if (!await roleManager.RoleExistsAsync("SuperAdmin"))
        {
            try
            {
                var createRoleResult = await roleManager.CreateAsync(new IdentityRole("SuperAdmin"));

                if (!createRoleResult.Succeeded && !await roleManager.RoleExistsAsync("SuperAdmin"))
                {
                    var msg = string.Join(", ", createRoleResult.Errors.Select(e => $"{e.Code}: {e.Description}"));
                    throw new InvalidOperationException($"Create role 'SuperAdmin' failed: {msg}");
                }

                logger.LogInformation("Verified role: SuperAdmin");
            }
            catch
            {
                if (!await roleManager.RoleExistsAsync("SuperAdmin"))
                    throw;
            }
        }

        var adminUser = await userManager.FindByEmailAsync("admin@demo.com");
        if (adminUser != null)
        {
            var roles = await userManager.GetRolesAsync(adminUser);

            if (!roles.Contains("SuperAdmin"))
            {
                var addRoleResult = await userManager.AddToRoleAsync(adminUser, "SuperAdmin");

                if (!addRoleResult.Succeeded)
                {
                    var rolesAfter = await userManager.GetRolesAsync(adminUser);
                    if (!rolesAfter.Contains("SuperAdmin"))
                    {
                        var msg = string.Join(", ", addRoleResult.Errors.Select(e => $"{e.Code}: {e.Description}"));
                        throw new InvalidOperationException($"Grant SuperAdmin to admin@demo.com failed: {msg}");
                    }
                }

                logger.LogInformation("Granted SuperAdmin role to admin@demo.com");
            }

            await userManager.SetLockoutEndDateAsync(adminUser, null);
            adminUser.AccessFailedCount = 0;
            await userManager.UpdateAsync(adminUser);
        }
        else
        {
            logger.LogWarning("admin@demo.com not found. Make sure WebHostRazor has seeded the Identity data first.");
        }

        logger.LogInformation("WebAdminMVC database initialization completed successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error initializing database");
        throw;
    }
}