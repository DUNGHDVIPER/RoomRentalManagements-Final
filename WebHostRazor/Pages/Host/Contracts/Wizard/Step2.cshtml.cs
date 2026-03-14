using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Globalization;

namespace WebHostRazor.Pages.Host.Contracts.Wizard;

public class Step2Model : PageModel
{
    [BindProperty] public int RoomId { get; set; }
    [BindProperty] public int TenantId { get; set; }

    [BindProperty] public DateTime StartDate { get; set; } = DateTime.Today;
    [BindProperty] public DateTime EndDate { get; set; } = DateTime.Today.AddMonths(6);
    [BindProperty] public decimal Rent { get; set; }
    [BindProperty] public decimal Deposit { get; set; }
    [BindProperty] public bool ActivateNow { get; set; } = true;

    public IActionResult OnGet()
    {
        // vẫn đọc từ TempData Step1 để set vào hidden (nếu TempData mất thì Step1 sẽ làm lại)
        var roomIdStr = TempData.Peek("W_RoomId") as string;
        var tenantIdStr = TempData.Peek("W_TenantId") as string;

        if (roomIdStr is null || tenantIdStr is null)
            return RedirectToPage("./Step1");

        RoomId = int.Parse(roomIdStr);
        TenantId = int.Parse(tenantIdStr);

        return Page();
    }

    public IActionResult OnPost()
    {
        if (RoomId <= 0 || TenantId <= 0)
            return RedirectToPage("./Step1");

        if (EndDate <= StartDate)
        {
            ModelState.AddModelError(string.Empty, "End date must be after start date.");
            return Page();
        }

        // ✅ Pass everything via query string to Step3 (no TempData)
        return RedirectToPage("./Step3", new
        {
            roomId = int.Parse(TempData.Peek("W_RoomId")!.ToString()!),
            tenantId = int.Parse(TempData.Peek("W_TenantId")!.ToString()!),
            start = StartDate.ToString("O"),
            end = EndDate.ToString("O"),
            rent = Rent.ToString(CultureInfo.InvariantCulture),
            deposit = Deposit.ToString(CultureInfo.InvariantCulture),
            activate = ActivateNow ? "1" : "0"
        });

    }

    public IActionResult OnPostBack()
        => RedirectToPage("./Step1");
}
