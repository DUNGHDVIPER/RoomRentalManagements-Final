using System.Security.Claims;
using BLL.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebHostRazor.Pages.Host.Contracts;

public class ReminderScanModel : PageModel
{
    private readonly IContractService _service;
    public ReminderScanModel(IContractService service) => _service = service;

    [BindProperty] public int DaysBeforeEnd { get; set; } = 7;

    public void OnGet() { }

    public async Task<IActionResult> OnPost()
    {
        try
        {
            var actorUserId = TryGetUserId();

            var sent = await _service.ScanAndSendExpiryRemindersAsync(
                daysBeforeEnd: DaysBeforeEnd,
                remindType: $"Expiry_{DaysBeforeEnd}d",
                actorUserId: actorUserId);

            TempData["Ok"] = $"Scan done. Sent {sent} reminder(s).";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            TempData["Err"] = ex.Message;
            return RedirectToPage();
        }
    }

    private int? TryGetUserId()
    {
        // tuỳ claim type bạn dùng: NameIdentifier / "UserId"
        var s = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("UserId");
        return int.TryParse(s, out var id) ? id : null;
    }
}