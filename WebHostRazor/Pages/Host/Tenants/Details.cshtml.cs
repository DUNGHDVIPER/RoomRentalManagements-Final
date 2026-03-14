using DAL.Data;
using DAL.Entities.Tenanting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebHostRazor.Pages.Host.Tenants
{
    public class DetailsModel : PageModel
    {
        private readonly AppDbContext _context;

        public DetailsModel(AppDbContext context)
        {
            _context = context;
        }

        public Tenant Tenant { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Tenant = await _context.Tenants.FindAsync(id);

            if (Tenant == null)
            {
                return NotFound();
            }

            return Page();
        }
    }
}