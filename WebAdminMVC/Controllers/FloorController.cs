using BLL.DTOs.Property;
using BLL.Services.Interfaces;
using DAL.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebAdminMVC.Controllers
{
    public class FloorsController : Controller
    {
        private readonly IFloorService _service;
        private readonly AppDbContext _context;

        public FloorsController(IFloorService service, AppDbContext context)
        {
            _service = service;
            _context = context;
        }

        // ================= LIST =================
        public async Task<IActionResult> Index(int blockId)
        {
            ViewBag.BlockId = blockId;

            var blockName = await _context.Blocks
                .Where(x => x.Id == blockId)
                .Select(x => x.BlockName)
                .FirstOrDefaultAsync();

            ViewBag.BlockName = blockName;

            var floors = await _service.GetByBlockAsync(blockId);

            return View(floors);
        }

        // ================= DETAILS =================
        public async Task<IActionResult> Details(int id)
        {
            var floor = await _service.GetByIdAsync(id);

            if (floor == null)
                return NotFound();

            return View(floor);
        }

        // ================= CREATE GET =================
        [HttpGet]
        public async Task<IActionResult> Create(int blockId)
        {
            var block = await _context.Blocks
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == blockId);

            if (block == null)
                return BadRequest("Block không tồn tại.");

            return View(new FloorDto
            {
                BlockId = block.Id,
                BlockName = block.BlockName
            });
        }

        // ================= CREATE POST =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FloorDto dto)
        {
            var blockExists = await _context.Blocks
                .AnyAsync(x => x.Id == dto.BlockId);

            if (!blockExists)
            {
                ModelState.AddModelError(nameof(dto.BlockId), "Block không tồn tại.");
            }

            if (!ModelState.IsValid)
                return View(dto);

            try
            {
                await _service.CreateAsync(dto);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("FloorName", ex.Message);
                return View(dto);
            }

            return RedirectToAction(nameof(Index), new { blockId = dto.BlockId });
        }

        // ================= EDIT GET =================
        public async Task<IActionResult> Edit(int id)
        {
            var floor = await _service.GetByIdAsync(id);

            if (floor == null)
                return NotFound();

            return View(floor);
        }

        // ================= EDIT POST =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(FloorDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            try
            {
                await _service.UpdateAsync(dto);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("FloorName", ex.Message);
                return View(dto);
            }

            return RedirectToAction(nameof(Index), new { blockId = dto.BlockId });
        }

        // ================= DELETE =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, int blockId)
        {
            await _service.DeleteAsync(id);

            return RedirectToAction(nameof(Index), new { blockId });
        }
    }
}