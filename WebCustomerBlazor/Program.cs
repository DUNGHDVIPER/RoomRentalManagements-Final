using DAL.Data;
using DAL.Seed;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebCustomerBlazor.Components;
using BLL.Services;
using BLL.Services.Interfaces;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Components.Authorization;
using WebCustomerBlazor.Services;

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

// ✅ FIXED: Use SHARED DataProtection keys with WebHostRazor
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "..", "DataProtection-Keys"))) // ✅ Same folder as WebHostRazor
    .SetApplicationName("RoomRentalApp"); // ✅ Same application name as WebHostRazor

// ✅ FIXED: Cookie Auth configuration for cross-domain compatibility
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/redirect-to-host-login";
    options.LogoutPath = "/Auth/Logout";
    options.AccessDeniedPath = "/Auth/AccessDenied";
    options.Cookie.Name = "CustomerAuth";
    // ✅ Cross-domain cookie settings
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.ExpireTimeSpan = TimeSpan.FromHours(24);
    options.SlidingExpiration = true;
});

// ✅ Custom Authentication State Provider
builder.Services.AddScoped<ICookieService, CookieService>();
builder.Services.AddScoped<CookieAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider => provider.GetRequiredService<CookieAuthenticationStateProvider>());

// Authorization
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

// =====================
// 2) Razor Components + Pages
// =====================
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();

// ✅ Configure Antiforgery for proper token handling
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
    options.Cookie.Name = "__RequestVerificationToken";
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// =====================
// 3) Services DI
// =====================
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<IContractService, ContractService>();
builder.Services.AddScoped<IStayHistoryService, StayHistoryService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IUserService, UserService>();

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
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();


app.MapRazorPages();
app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

app.Run();