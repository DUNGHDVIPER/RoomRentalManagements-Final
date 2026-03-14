using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebHostRazor.Pages;

public class IndexModel : PageModel
{
    public IActionResult OnGet()
        => RedirectToPage("/Host/Dashboard/Index");
}
