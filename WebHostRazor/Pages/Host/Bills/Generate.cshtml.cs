using BLL.DTOs.Billing;
using BLL.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebHostRazor.Pages.Host.Bills;

public class GenerateModel : PageModel
{
    private readonly IBillingService _billing;

    public GenerateModel(IBillingService billing)
    {
        _billing = billing;
    }

    [BindProperty]
    public string Month { get; set; }

    [BindProperty]
    public DateTime DueDate { get; set; }

    [BindProperty]
    public bool IncludeRent { get; set; } = true;

    [BindProperty]
    public bool IncludeUtilities { get; set; } = true;

    public async Task<IActionResult> OnGetAsync()
    {
        Month = DateTime.Today.ToString("yyyy-MM");
        var now = DateTime.Today;
        DueDate = new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month));
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!DateTime.TryParseExact(Month, "yyyy-MM", null,
            System.Globalization.DateTimeStyles.None, out var dt))
        {
            ModelState.AddModelError("", "Sai format yyyy-MM");
            return Page();
        }

        var period = dt.Year * 100 + dt.Month;

        var req = new GenerateBillsRequestDto
        {
            Period = period,
            DueDate = DueDate
        };

        var (ok, err, created, skipped) =
            await _billing.GenerateBillsAsync(req, new List<int>(), IncludeRent, IncludeUtilities, default);

        if (!ok)
        {
            ModelState.AddModelError("", err);
            return Page();
        }

        TempData["msg"] = $"Tạo {created} bill, bỏ qua {skipped}";
        return RedirectToPage("Index");
    }
}