using BLL.DTOs.Utility;
using BLL.Services.Interfaces;
using DAL.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebAdmin.MVC.Controllers;

public class UtilityReadingsController : Controller
{
    private readonly AppDbContext _db;
    private readonly IUtilityService _utility;

    public UtilityReadingsController(AppDbContext db, IUtilityService utility)
    {
        _db = db;
        _utility = utility;
    }

    // GET
    [HttpGet]
    public async Task<IActionResult> Bulk(int? period, CancellationToken ct)
    {
        var now = DateTime.Today;
        var p = period ?? (now.Year * 100 + now.Month);

        var rooms = await _db.Rooms
            .AsNoTracking()
            .OrderBy(r => r.RoomCode)
            .ToListAsync(ct);

        var existing = await _db.UtilityReadings
            .AsNoTracking()
            .Where(x => x.Period == p)
            .ToListAsync(ct);

        var dto = new BulkUtilityReadingDto
        {
            Period = p
        };

        foreach (var r in rooms)
        {
            var ex = existing.FirstOrDefault(x => x.RoomId == r.RoomId);

            dto.Items.Add(new BulkUtilityReadingDto.RoomReadingItem
            {
                RoomId = r.RoomId,
                ElectricKwh = ex?.ElectricKwh ?? 0,
                WaterM3 = ex?.WaterM3 ?? 0
            });
        }

        ViewBag.Rooms = rooms; // để view hiển thị tên phòng
        return View(dto);
    }

    // POST
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Bulk(BulkUtilityReadingDto dto, CancellationToken ct)
    {
        if (dto.Items == null || dto.Items.Count == 0)
        {
            ModelState.AddModelError("", "Không có dữ liệu.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Rooms = await _db.Rooms.ToListAsync(ct);
            return View(dto);
        }

        await _utility.BulkUpsertReadingsAsync(dto, ct);

        TempData["success"] = $"Đã lưu chỉ số kỳ {dto.Period}.";
        return RedirectToAction(nameof(Bulk), new { period = dto.Period });
    }
}