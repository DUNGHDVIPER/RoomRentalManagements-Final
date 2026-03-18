using BLL.DTOs.Utility;
using BLL.Services.Interfaces;
using DAL.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
 
using DAL.Entities.Property;

namespace WebHostRazor.Pages.Host.Utilities;

public class BulkModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IUtilityService _utility;

    public BulkModel(AppDbContext db, IUtilityService utility)
    {
        _db = db;
        _utility = utility;
    }

    [BindProperty]
    public BulkUtilityReadingDto Dto { get; set; } = new();

    public List<Room> Rooms { get; set; } = new();

    public async Task OnGetAsync(int? period)
    {
        var now = DateTime.Today;
        var p = period ?? (now.Year * 100 + now.Month);

        Rooms = await _db.Rooms
            .AsNoTracking()
            .OrderBy(r => r.RoomCode)
            .ToListAsync();

        var existing = await _db.UtilityReadings
            .Where(x => x.Period == p)
            .ToListAsync();

        Dto.Period = p;

        foreach (var r in Rooms)
        {
            var ex = existing.FirstOrDefault(x => x.RoomId == r.RoomId);

            Dto.Items.Add(new BulkUtilityReadingDto.RoomReadingItem
            {
                RoomId = r.RoomId,
                ElectricKwh = ex?.ElectricKwh ?? 0,
                WaterM3 = ex?.WaterM3 ?? 0
            });
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Dto.Items == null || Dto.Items.Count == 0)
        {
            ModelState.AddModelError("", "Không có dữ liệu.");
            return Page();
        }

        await _utility.BulkUpsertReadingsAsync(Dto);

        TempData["success"] = $"Đã lưu kỳ {Dto.Period}";
        return RedirectToPage("/Host/Utilities/Bulk", new { period = Dto.Period });
    }
}