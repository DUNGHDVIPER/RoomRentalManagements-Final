using BLL.Common;
using DAL.Data;
using DAL.Entities.Tenanting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
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

    public class InputModel
    {
        public string FullName { get; set; } = null!;
        public DateTime? DateOfBirth { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? CCCD { get; set; }
        public string? Gender { get; set; }
        public string Status { get; set; } = "Active";
        public string? Address { get; set; }
        public DateTime CheckInAt { get; set; }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return new JsonResult(new
            {
                success = false,
                message = "Invalid data"
            });

        try
        {
            var today = DateTime.Today;

            // ===== DATE OF BIRTH VALIDATION =====
            if (!Input.DateOfBirth.HasValue)
                return new JsonResult(new { success = false, message = "Date of birth is required" });

            if (Input.DateOfBirth > today)
                return new JsonResult(new { success = false, message = "Date of birth cannot be in future" });

            var age = today.Year - Input.DateOfBirth.Value.Year;
            if (Input.DateOfBirth.Value.Date > today.AddYears(-age))
                age--;

            if (age < 18)
                return new JsonResult(new
                {
                    success = false,
                    message = "Tenant must be at least 18 years old"
                });

            //// ===== CHECK-IN VALIDATION =====
            //if (Input.CheckInAt == default)
            //    return new JsonResult(new
            //    {
            //        success = false,
            //        message = "Check-in date is required"
            //    });

            //if (Input.CheckInAt < today)
            //    return new JsonResult(new
            //    {
            //        success = false,
            //        message = "Check-in date cannot be in the past"
            //    });

            // ===== DUPLICATE CHECK =====
            if (!string.IsNullOrWhiteSpace(Input.CCCD) &&
                await _context.Tenants.AnyAsync(x => x.CCCD == Input.CCCD))
                return new JsonResult(new { success = false, message = "CCCD already exists" });

            // ===== PHONE VALIDATION =====
            if (!string.IsNullOrWhiteSpace(Input.Phone))
            {
                Input.Phone = Input.Phone.Trim();

                // Remove all spaces inside
                Input.Phone = Input.Phone.Replace(" ", "");

                // Check only digits and length = 10
                if (!System.Text.RegularExpressions.Regex.IsMatch(Input.Phone, @"^0\d{9}$"))
                {
                    return new JsonResult(new
                    {
                        success = false,
                        message = "Phone must be 10 digits and start with 0"
                    });
                }
            }
            if (!string.IsNullOrWhiteSpace(Input.Phone) &&
                await _context.Tenants.AnyAsync(x => x.Phone == Input.Phone))
                return new JsonResult(new { success = false, message = "Phone already exists" });

            if (!string.IsNullOrWhiteSpace(Input.Email) &&
                await _context.Tenants.AnyAsync(x => x.Email == Input.Email))
                return new JsonResult(new { success = false, message = "Email already exists" });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // ===== CREATE =====
            var tenant = new Tenant
            {
                FullName = Input.FullName,
                DateOfBirth = Input.DateOfBirth,
                Phone = Input.Phone,
                Email = Input.Email,
                CCCD = Input.CCCD,
                Gender = Input.Gender,
                Status = Input.Status,
                Address = Input.Address,
                UserId = userId,
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