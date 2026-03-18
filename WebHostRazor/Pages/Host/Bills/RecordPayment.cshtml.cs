using BLL.DTOs.Billing;
using BLL.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebHostRazor.Pages.Host.Bills;

public class RecordPaymentModel : PageModel
{
    private readonly IBillingService _billing;

    public RecordPaymentModel(IBillingService billing)
    {
        _billing = billing;
    }

    [BindProperty]
    public int BillId { get; set; }

    [BindProperty]
    public decimal Amount { get; set; }

    [BindProperty]
    public string Method { get; set; } = "Cash";

    // 👉 thêm
    public decimal Remaining { get; set; }
    public decimal Total { get; set; }
    public decimal Paid { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var bill = await _billing.GetBillAsync(id, default);
        if (bill == null) return NotFound();

        BillId = id;

        Paid = bill.Payments?
    .Where(p => p.Status == "Paid")
    .Sum(p => p.Amount) ?? 0;

        Total = bill.TotalAmount;
        Remaining = Math.Max(0, Total - Paid);

        // 👉 AUTO AMOUNT
        Amount = Remaining;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Amount <= 0)
        {
            ModelState.AddModelError("", "Amount phải > 0");
            return Page();
        }

        var dto = new RecordPaymentDto
        {
            BillId = BillId,
            Amount = Amount,
            Method = Method,
            PaidAt = DateTime.UtcNow
        };

        var (ok, err) = await _billing.RecordPaymentAsync(dto, default);

        if (!ok)
        {
            ModelState.AddModelError("", err ?? "Payment failed");
            return Page();
        }

        return RedirectToPage("Details", new { id = BillId });
    }
}