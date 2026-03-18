using BLL.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebHostRazor.Pages.Host.Bills;

public class DeleteModel : PageModel
{
    private readonly IBillingService _billing;

    public DeleteModel(IBillingService billing)
    {
        _billing = billing;
    }

    public int Id { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Id = id;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        await _billing.DeleteBillAsync(id, default);
        return RedirectToPage("Index");
    }
}