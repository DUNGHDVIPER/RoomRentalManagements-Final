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

        // LIST
        public async Task<IActionResult> Index(int blockId)
        {
            ViewBag.BlockId = blockId;

            var floors = await _service.GetByBlockAsync(blockId);

            return View(floors);
        }

        // DETAILS
        public async Task<IActionResult> Details(int id)
        {
            var floor = await _service.GetByIdAsync(id);

            if (floor == null)
                return NotFound();

            return View(floor);
        }

        // CREATE GET
        [HttpGet]
        public async Task<IActionResult> Create(int blockId)
        {
            var blockExists = await _context.Blocks.AnyAsync(x => x.Id == blockId);
            if (!blockExists)
                return BadRequest("Block không tồn tại hoặc thiếu blockId.");

            return View(new FloorDto { BlockId = blockId });
        }

        // CREATE POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FloorDto dto)
        {
            var blockExists = await _context.Blocks.AnyAsync(x => x.Id == dto.BlockId);
            if (!blockExists)
            {
                ModelState.AddModelError(nameof(dto.BlockId), "Block không tồn tại.");
            }

            if (!ModelState.IsValid)
                return View(dto);

            await _service.CreateAsync(dto);

            return RedirectToAction(nameof(Index), new { blockId = dto.BlockId });
        }

        // EDIT GET
        public async Task<IActionResult> Edit(int id)
        {
            var floor = await _service.GetByIdAsync(id);

            if (floor == null)
                return NotFound();

            return View(floor);
        }

        // EDIT POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(FloorDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            await _service.UpdateAsync(dto);

            return RedirectToAction(nameof(Index), new { blockId = dto.BlockId });
        }

        // DELETE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, int blockId)
        {
            await _service.DeleteAsync(id);

            return RedirectToAction(nameof(Index), new { blockId });
        }
    }
}