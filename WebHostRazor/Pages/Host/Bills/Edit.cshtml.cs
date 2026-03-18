using BLL.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebHostRazor.Pages.Host.Bills;

public class EditModel : PageModel
{
    private readonly IBillingService _billing;

    public EditModel(IBillingService billing)
    {
        _billing = billing;
    }

    [BindProperty]
    public int Id { get; set; }

    [BindProperty]
    public string Month { get; set; }

    [BindProperty]
    public decimal Total { get; set; }

    [BindProperty]
    public string Status { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var b = await _billing.GetBillAsync(id, default);
        if (b == null) return NotFound();

        Id = b.Id;
        Month = $"{b.Period / 100:D4}-{b.Period % 100:D2}";
        Total = b.TotalAmount;
        Status = b.Status;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var (ok, err) = await _billing.UpdateBillAsync(Id, Month, Status, Total, default);

        if (!ok)
        {
            ModelState.AddModelError("", err);
            return Page();
        }

        return RedirectToPage("Index");
    }
}