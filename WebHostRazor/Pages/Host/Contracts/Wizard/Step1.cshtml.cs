using DAL.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace WebHostRazor.Pages.Host.Contracts.Wizard;
public class Step1Model : PageModel
{
    private readonly AppDbContext _db;
    public Step1Model(AppDbContext db) => _db = db;

    public List<(int RoomId, string RoomCode)> Rooms { get; set; } = [];
    public List<(int TenantId, string FullName)> Tenants { get; set; } = [];

    [BindProperty] public int RoomId { get; set; }
    [BindProperty] public int TenantId { get; set; }

    public async Task OnGet()
    {
        Rooms = await _db.Rooms.AsNoTracking()
            .OrderBy(r => r.RoomNo)
            .Select(r => new ValueTuple<int, string>(r.Id, r.RoomNo))
            .ToListAsync();

        Tenants = await _db.Tenants.AsNoTracking()
            .OrderBy(t => t.FullName)
            .Select(t => new ValueTuple<int, string>(t.Id, t.FullName))
            .ToListAsync();
    }

    public IActionResult OnPost()
    {
        if (RoomId <= 0 || TenantId <= 0)
        {
            ModelState.AddModelError(string.Empty, "Please select a room and a tenant.");
            return Page();
        }

        TempData["W_RoomId"] = RoomId.ToString();
        TempData["W_TenantId"] = TenantId.ToString();

        return RedirectToPage("./Step2");
    }
    public IActionResult OnPostCancel()
    {
        TempData.Clear();
        return RedirectToPage("/Host/Contracts/Index");
    }

}
