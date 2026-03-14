using DAL.Data;
using DAL.Entities.Tenanting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace WebHostRazor.Pages.Host.Tenants
{
    public class EditModel : PageModel
    {
        private readonly AppDbContext _context;

        public EditModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Tenant Tenant { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Tenant = await _context.Tenants.FindAsync(id);

            if (Tenant == null)
                return NotFound();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                var today = DateTime.Today;

                var tenantInDb = await _context.Tenants.FindAsync(Tenant.Id);
                if (tenantInDb == null)
                    return new JsonResult(new { success = false, message = "Tenant not found" });

                // ===== DOB VALIDATION =====
                if (!Tenant.DateOfBirth.HasValue)
                    return new JsonResult(new { success = false, message = "Date of birth is required" });

                if (Tenant.DateOfBirth > today)
                    return new JsonResult(new { success = false, message = "Date of birth cannot be in future" });

                var age = today.Year - Tenant.DateOfBirth.Value.Year;
                if (Tenant.DateOfBirth.Value.Date > today.AddYears(-age))
                    age--;

                if (age < 18)
                    return new JsonResult(new { success = false, message = "Tenant must be at least 18 years old" });

         

                // ===== DUPLICATE CHECK =====
                if (!string.IsNullOrWhiteSpace(Tenant.CCCD) &&
                    await _context.Tenants.AnyAsync(x =>
                        x.CCCD == Tenant.CCCD && x.Id != Tenant.Id))
                    return new JsonResult(new { success = false, message = "CCCD already exists" });
                // ===== PHONE VALIDATION =====
                if (!string.IsNullOrWhiteSpace(Tenant.Phone))
                {
                    Tenant.Phone = Tenant.Phone.Trim();

                    // Remove all spaces inside
                    Tenant.Phone = Tenant.Phone.Replace(" ", "");

                    // Check only digits and length = 10
                    if (!System.Text.RegularExpressions.Regex.IsMatch(Tenant.Phone, @"^0\d{9}$"))
                    {
                        return new JsonResult(new
                        {
                            success = false,
                            message = "Phone must be 10 digits and start with 0"
                        });
                    }
                }

                if (!string.IsNullOrWhiteSpace(Tenant.Phone) &&
                    await _context.Tenants.AnyAsync(x =>
                        x.Phone == Tenant.Phone && x.Id != Tenant.Id))
                    return new JsonResult(new { success = false, message = "Phone already exists" });

                if (!string.IsNullOrWhiteSpace(Tenant.Email) &&
                    await _context.Tenants.AnyAsync(x =>
                        x.Email == Tenant.Email && x.Id != Tenant.Id))
                    return new JsonResult(new { success = false, message = "Email already exists" });

                // ===== UPDATE =====
                tenantInDb.FullName = Tenant.FullName;
                tenantInDb.DateOfBirth = Tenant.DateOfBirth;
                tenantInDb.Phone = Tenant.Phone;
                tenantInDb.Email = Tenant.Email;
                tenantInDb.Address = Tenant.Address;
                tenantInDb.CCCD = Tenant.CCCD;
                tenantInDb.Gender = Tenant.Gender;
                tenantInDb.Status = Tenant.Status;
                tenantInDb.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return new JsonResult(new
                {
                    success = true,
                    message = "Tenant updated successfully!"
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
}