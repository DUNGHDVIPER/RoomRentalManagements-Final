using BLL.DTOs.Property;
using BLL.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using WebAdminMVC.Models.Blocks;

public class BlocksController : Controller
{
    private readonly IBlockService _service;

    public BlocksController(IBlockService service)
    {
        _service = service;
    }

    // ================= LIST =================

    public async Task<IActionResult> Index()
    {
        var blocks = await _service.GetAllAsync();

        var vm = blocks.Select(b => new BlockListVm
        {
            BlockId = b.BlockId,
            BlockName = b.BlockName,
            Address = b.Address,
            Note = b.Note,
            Status = b.Status,
            TotalFloors = b.TotalFloors,
            TotalRooms = b.TotalRooms
        }).ToList();

        return View(vm);
    }

    // ================= CREATE =================

    [HttpGet]
    public IActionResult Create()
    {
        return View(new BlockDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BlockDto dto)
    {
        if (!ModelState.IsValid)
            return View(dto);

        await _service.CreateAsync(dto);

        return RedirectToAction(nameof(Index));
    }

    // ================= EDIT =================

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var block = await _service.GetByIdAsync(id);

        if (block == null)
            return NotFound();

        return View(block);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, BlockDto dto)
    {
        if (!ModelState.IsValid)
            return View(dto);

        await _service.UpdateAsync(id, dto);

        return RedirectToAction(nameof(Index));
    }

    // ================= CLOSE =================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(int id)
    {
        await _service.CloseAsync(id);

        return RedirectToAction(nameof(Index));
    }
}