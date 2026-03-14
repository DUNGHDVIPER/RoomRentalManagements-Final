
using BLL.DTOs.Room;
using BLL.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using WebAdmin.MVC.Models.Amenities;

namespace WebAdmin.MVC.Controllers;

public class AmenitiesController : Controller
{
    private readonly IAmenityService _amenityService;

    public AmenitiesController(IAmenityService amenityService)
    {
        _amenityService = amenityService;
    }

    // ===================== LIST =====================
    public async Task<IActionResult> Index()
    {
        var amenities = await _amenityService.GetAllAsync();

        var model = amenities.Select(a => new AmenityListItemVm
        {
            AmenityId = a.AmenityId,
            AmenityName = a.AmenityName
        }).ToList();

        return View(model);
    }

    // ===================== CREATE =====================
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AmenityDto dto)
    {
        if (!ModelState.IsValid)
            return View(dto);

        try
        {
            await _amenityService.CreateAsync(dto);
            TempData["Success"] = "Amenity created successfully.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return View(dto);
        }

        return RedirectToAction(nameof(Index));
    }

    // ===================== EDIT =====================
    public async Task<IActionResult> Edit(int id)
    {
        var amenity = await _amenityService.GetByIdAsync(id);

        if (amenity == null)
            return NotFound();

        return View(amenity);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AmenityDto dto)
    {
        if (!ModelState.IsValid)
            return View(dto);

        try
        {
            await _amenityService.UpdateAsync(dto);
            TempData["Success"] = "Amenity updated successfully.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return View(dto);
        }

        return RedirectToAction(nameof(Index));
    }

    // ===================== DELETE =====================
    public async Task<IActionResult> Delete(int id)
    {
        var amenity = await _amenityService.GetByIdAsync(id);

        if (amenity == null)
            return NotFound();

        return View(amenity);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            await _amenityService.DeleteAsync(id);
            TempData["Success"] = "Amenity deleted successfully.";
        }
        catch
        {
            TempData["Error"] = "Cannot delete this amenity because it is used by rooms.";
        }

        return RedirectToAction(nameof(Index));
    }
}
