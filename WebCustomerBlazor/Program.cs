using Microsoft.Extensions.FileProviders;
using DAL.Data;
using DAL.Seed;
using DAL.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebCustomerBlazor.Components;
using BLL.Services;
using BLL.Services.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebApplication.CreateBuilder(args);

// =====================
// 1) DB + Identity
// =====================
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<IdentityUser, IdentityRole>(opt =>
{
    opt.Password.RequireNonAlphanumeric = false;
    opt.Password.RequireUppercase = false;
    opt.Password.RequireLowercase = false;
    opt.Password.RequireDigit = false;
    opt.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// Cookie Auth
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Auth/Login";
    options.LogoutPath = "/Auth/Logout";
    options.AccessDeniedPath = "/Auth/AccessDenied";
});

// Authorization
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();

// =====================
// 2) Razor Components
// =====================
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// =====================
// 3) Services DI
// =====================
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// CORE BUSINESS SERVICES
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IContractService, ContractService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<IStayHistoryService, StayHistoryService>();
builder.Services.AddScoped<IBillingService, BillingService>();
builder.Services.AddScoped<IUtilityService, UtilityService>();

// REPOSITORIES
builder.Services.AddScoped<ITenantRepository, TenantRepository>();

// OTHER SERVICES
builder.Services.AddMemoryCache();

// BLAZOR-SPECIFIC AUTHENTICATION
builder.Services.AddScoped<TokenAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider =>
    provider.GetRequiredService<TokenAuthenticationStateProvider>());

var app = builder.Build();

// =====================
// 4) Migrate + Seed
// =====================
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

    await IdentitySeeder.SeedAsync(roleMgr, userMgr);

    // Reset password Admin
    var admin = await userMgr.FindByEmailAsync("admin@demo.com");

    if (admin != null)
    {
        var token = await userMgr.GeneratePasswordResetTokenAsync(admin);
        await userMgr.ResetPasswordAsync(admin, token, "Admin@123");

        await userMgr.SetLockoutEndDateAsync(admin, null);
        admin.AccessFailedCount = 0;
        await userMgr.UpdateAsync(admin);
    }
}

// =====================
// 5) Middleware
// =====================
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

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

// =====================
// 6) Map Blazor
// =====================
app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

app.Run();
static async Task InitializeDatabaseAsync(IServiceProvider services)
{
    await using var scope = services.CreateAsyncScope();

    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        await db.Database.MigrateAsync();

        // KHÔNG seed role/user ở đây nữa.
        // Việc seed chỉ để WebHostRazor xử lý để tránh duplicate role khi chạy đồng thời.

        var customer = await userManager.FindByEmailAsync("customer@demo.com");
        var tenant = await userManager.FindByEmailAsync("tenant@demo.com");

        if (customer == null && tenant == null)
        {
            logger.LogWarning("Customer/Tenant demo accounts not found yet. Run WebHostRazor first so it can seed Identity data.");
        }

        logger.LogInformation("WebCustomerBlazor database initialization completed successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error initializing WebCustomerBlazor database");
        throw;
    }
}