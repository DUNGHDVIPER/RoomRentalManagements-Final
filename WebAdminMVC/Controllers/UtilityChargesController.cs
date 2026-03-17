using BLL.DTOs.Utility;
using BLL.Services.Interfaces;
using DAL.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebAdmin.MVC.Controllers;

public class UtilityChargesController : Controller
{
    private readonly AppDbContext _db;
    private readonly IUtilityService _utility;

    public UtilityChargesController(AppDbContext db, IUtilityService utility)
    {
        _db = db;
        _utility = utility;
    }

    [HttpGet]
    public async Task<IActionResult> Preview(int period, CancellationToken ct)
    {
        var rooms = await _db.Rooms
            .AsNoTracking()
            .OrderBy(r => r.RoomNo)
            .ToListAsync(ct);

        var results = new List<UtilityChargeResultDto>();

        foreach (var r in rooms)
        {
            try
            {
                var charge = await _utility.CalculateChargesAsync(r.Id, period, ct);
                results.Add(charge);
            }
            catch
            {
                // nếu phòng chưa có reading thì bỏ qua
            }
        }

        ViewBag.Period = period;
        ViewBag.Rooms = rooms;

        return View(results);
    }
}