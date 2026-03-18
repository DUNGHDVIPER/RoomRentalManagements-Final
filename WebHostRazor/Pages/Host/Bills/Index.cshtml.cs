using BLL.DTOs.Billing;
using BLL.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebHostRazor.Pages.Host.Bills;

public class IndexModel : PageModel
{
    private readonly IBillingService _billing;

    public IndexModel(IBillingService billing)
    {
        _billing = billing;
    }

    public List<BillDto> Bills { get; set; } = new();

    public async Task OnGetAsync()
    {
        Bills = await _billing.GetBillsAsync(null, null, null, default);
    }
}