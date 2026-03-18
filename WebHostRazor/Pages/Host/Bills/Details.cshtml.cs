using BLL.DTOs.Billing;
using BLL.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebHostRazor.Pages.Host.Bills;

public class DetailsModel : PageModel
{
    private readonly IBillingService _billing;

    public DetailsModel(IBillingService billing)
    {
        _billing = billing;
    }

    public BillDto Bill { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Bill = await _billing.GetBillAsync(id, default);
        if (Bill == null) return NotFound();

        return Page();
    }
}