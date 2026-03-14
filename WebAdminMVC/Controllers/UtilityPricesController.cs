using BLL.DTOs.Utility;
using BLL.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebAdmin.MVC.Controllers;

public class UtilityPricesController : Controller
{
    private readonly IUtilityService _utility;

    public UtilityPricesController(IUtilityService utility)
    {
        _utility = utility;
    }

    // GET: /UtilityPrices
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var current = await _utility.GetCurrentPriceAsync(ct); // UtilityPriceDto
        return View(current);
    }

    // GET: /UtilityPrices/Set
    [HttpGet]
    public IActionResult Set()
    {
        return View(new UtilityPriceDto
        {
            EffectiveFrom = DateTime.Today
        });
    }

    // POST: /UtilityPrices/Set
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Set(UtilityPriceDto dto, CancellationToken ct)
    {
        if (dto.EffectiveFrom == default)
            ModelState.AddModelError(nameof(dto.EffectiveFrom), "Vui lòng chọn ngày áp dụng.");

        if (dto.ElectricPerKwh < 0)
            ModelState.AddModelError(nameof(dto.ElectricPerKwh), "Giá điện không được âm.");

        if (dto.WaterPerM3 < 0)
            ModelState.AddModelError(nameof(dto.WaterPerM3), "Giá nước không được âm.");

        if (!ModelState.IsValid)
            return View(dto);

        await _utility.SetPriceAsync(dto, ct);

        TempData["success"] = "Đã cập nhật bảng giá điện/nước.";
        return RedirectToAction(nameof(Index));
    }
}