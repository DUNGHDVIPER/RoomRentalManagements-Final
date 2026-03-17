using BLL.Services;
using BLL.Services.Interfaces;
using DAL.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection; // ✅ Thêm dòng này

var builder = WebApplication.CreateBuilder(args);

// =====================
// 1) MVC
// =====================
builder.Services.AddControllersWithViews();

// =====================
// 2) DbContext
// =====================
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// =====================
// 3) Identity
// =====================
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// ✅ DataProtection riêng biệt cho Admin
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "DataProtection-Keys-Admin")))
    .SetApplicationName("WebAdminMVC");

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Auth/Login";
    options.AccessDeniedPath = "/Auth/AccessDenied";
    options.Cookie.Name = "AdminAuth"; // ✅ Tên cookie riêng
});

// =====================
// 4) BLL Services
// =====================
builder.Services.AddScoped<IContractService, ContractService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IBillingService, BillingService>();
builder.Services.AddScoped<IUtilityService, UtilityService>();
builder.Services.AddScoped<IReportService, ReportService>();

// =====================
// 5) Session với tên riêng
// =====================
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = "AdminSession"; // ✅ Tên session cookie riêng
});

// =====================
// 6) Cookie Authentication
// =====================
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opt =>
    {
        opt.LoginPath = "/Account/Login";
        opt.AccessDeniedPath = "/Account/AccessDenied";
        opt.SlidingExpiration = true;
        opt.Cookie.Name = "AdminCookieAuth"; // ✅ Tên cookie auth riêng
    });

// =====================
// 7) Authorization
// =====================
builder.Services.AddAuthorization(opt =>
{
    opt.AddPolicy("Host", p => p.RequireRole("Host", "Admin"));
});

var app = builder.Build();

// =====================
// RESET ADMIN PASSWORD
// =====================
using (var scope = app.Services.CreateScope())
{
    var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

    var user = await userMgr.FindByEmailAsync("admin@demo.com");

    if (user != null)
    {
        var token = await userMgr.GeneratePasswordResetTokenAsync(user);
        await userMgr.ResetPasswordAsync(user, token, "Admin@12345!");
    }
}

// =====================
// Middleware
// =====================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// serve SharedUploads as /uploads
var sharedUploadsPath = Path.GetFullPath(
    Path.Combine(app.Environment.ContentRootPath, "..", "SharedUploads"));

Directory.CreateDirectory(sharedUploadsPath);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(sharedUploadsPath),
    RequestPath = "/uploads"
});

app.UseRouting();

// ✅ Session middleware order đúng
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// =====================
// Routing
// =====================
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

// =====================
// Seed Roles
// =====================
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    string[] roles = { "Admin", "Host", "Tenant" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }
}

app.Run();