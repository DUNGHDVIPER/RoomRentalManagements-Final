using DAL.Data;
using DAL.Entities.Billing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAdmin.MVC.Models.Billing;

namespace WebAdmin.MVC.Controllers;

public class ExtraFeesController : Controller
{
    private readonly AppDbContext _db;

    public ExtraFeesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var list = await _db.ExtraFees
            .AsNoTracking()
            .OrderByDescending(x => x.IsActive)
            .ThenBy(x => x.Name)
            .ToListAsync(ct);

        return View(list);
    }

    [HttpGet]
    public IActionResult Create() => View(new ExtraFeeEditVm());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ExtraFeeEditVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(vm);

        var entity = new ExtraFee
        {
            Name = vm.Name.Trim(),
            DefaultAmount = vm.DefaultAmount,
            IsActive = vm.IsActive
        };

        _db.ExtraFees.Add(entity);
        await _db.SaveChangesAsync(ct);

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var entity = await _db.ExtraFees.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity == null) return NotFound();

        return View(new ExtraFeeEditVm
        {
            Id = entity.Id,
            Name = entity.Name,
            DefaultAmount = entity.DefaultAmount,
            IsActive = entity.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ExtraFeeEditVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(vm);

        var entity = await _db.ExtraFees.FirstOrDefaultAsync(x => x.Id == vm.Id, ct);
        if (entity == null) return NotFound();

        entity.Name = vm.Name.Trim();
        entity.DefaultAmount = vm.DefaultAmount;
        entity.IsActive = vm.IsActive;

        await _db.SaveChangesAsync(ct);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id, CancellationToken ct)
    {
        var entity = await _db.ExtraFees.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity == null) return NotFound();

        entity.IsActive = !entity.IsActive;
        await _db.SaveChangesAsync(ct);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _db.ExtraFees.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity == null) return RedirectToAction(nameof(Index));

        _db.ExtraFees.Remove(entity);
        await _db.SaveChangesAsync(ct);

        return RedirectToAction(nameof(Index));
    }
}