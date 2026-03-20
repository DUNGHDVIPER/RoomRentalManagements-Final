using DAL.Data;
using DAL.Entities.Common;
using DAL.Entities.Tenanting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace WebHostRazor.Pages.Host.Tenants;

[Authorize]
public class CreateModel : PageModel
{
    private readonly AppDbContext _context;

    public CreateModel(AppDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public List<SelectListItem> Customers { get; set; } = new();

    public class InputModel
    {
        public string UserId { get; set; } = null!;

        public string? CCCD { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? Address { get; set; }


        public TenantStatus Status { get; set; } = TenantStatus.Active;
    }

    public async Task OnGetAsync()
    {
        Customers = await (
            from user in _context.Users
            join ur in _context.UserRoles on user.Id equals ur.UserId
            join role in _context.Roles on ur.RoleId equals role.Id
            where role.Name == "Customer"

            // không phải Admin
            where !_context.UserRoles.Any(x =>
                x.UserId == user.Id &&
                _context.Roles.Any(r => r.Id == x.RoleId && r.Name == "Admin"))

            // không phải Host
            where !_context.UserRoles.Any(x =>
                x.UserId == user.Id &&
                _context.Roles.Any(r => r.Id == x.RoleId && r.Name == "Host"))

            // chưa có tenant
            where !_context.Tenants.Any(t => t.UserId == user.Id)

            select new SelectListItem
            {
                Value = user.Id,
                Text = user.Email
            }
        ).Distinct().ToListAsync();
    }
    public async Task<IActionResult> OnGetCustomerInfo(string userId)
    {
        var user = await _context.Users
            .Where(x => x.Id == userId)
            .Select(x => new
            {
                email = x.Email,
                phone = x.PhoneNumber
            })
            .FirstOrDefaultAsync();

        return new JsonResult(user);
    }
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return new JsonResult(new
            {
                success = false,
                message = "Invalid data"
            });
        }

        try
        {
            // ===== CHECK USER IS CUSTOMER =====
            var isCustomer = await (
                from ur in _context.UserRoles
                join r in _context.Roles on ur.RoleId equals r.Id
                where ur.UserId == Input.UserId && r.Name == "Customer"
                select ur
            ).AnyAsync();

            if (!isCustomer)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = "Selected user is not a customer"
                });
            }

            // ===== CHECK TENANT EXISTS =====
            var existed = await _context.Tenants
                .AnyAsync(x => x.UserId == Input.UserId);

            if (existed)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = "This customer already has a tenant profile"
                });
            }

            // ===== GET USER INFO =====
            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Id == Input.UserId);

            if (user == null)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = "Customer not found"
                });
            }

            // ===== DOB VALIDATION =====
            if (Input.DateOfBirth.HasValue)
            {
                var today = DateTime.Today;

                if (Input.DateOfBirth > today)
                {
                    return new JsonResult(new
                    {
                        success = false,
                        message = "Date of birth cannot be in the future"
                    });
                }

                var age = today.Year - Input.DateOfBirth.Value.Year;

                if (Input.DateOfBirth.Value.Date > today.AddYears(-age))
                    age--;

                if (age < 18)
                {
                    return new JsonResult(new
                    {
                        success = false,
                        message = "Tenant must be at least 18 years old"
                    });
                }
            }

            // ===== CREATE TENANT =====
            var tenant = new Tenant
            {
                UserId = user.Id,
                FullName = user.UserName ?? user.Email ?? "Customer",

                Email = user.Email,
                Phone = user.PhoneNumber,

                CCCD = Input.CCCD,
                Gender = Input.Gender,
                Address = Input.Address,
                DateOfBirth = Input.DateOfBirth,
                Status = Input.Status,

                CreatedAt = DateTime.UtcNow
            };

            _context.Tenants.Add(tenant);
            await _context.SaveChangesAsync();

            return new JsonResult(new
            {
                success = true,
                message = "Tenant created successfully!"
            });
        }
        catch
        {
            return new JsonResult(new
            {
                success = false,
                message = "Something went wrong"
            });
        }
    }
}