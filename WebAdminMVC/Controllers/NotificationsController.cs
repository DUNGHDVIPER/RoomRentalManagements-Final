using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DAL.Data;
using DAL.Entities.System;
using DAL.Entities.Common;
using BLL.Services.Interfaces;
using BLL.DTOs.Notification;

namespace WebAdminMVC.Controllers
{
    public class NotificationsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly INotificationService _notificationService;

        public NotificationsController(
            AppDbContext context,
            UserManager<IdentityUser> userManager,
            INotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        // =========================
        // INDEX
        // =========================
        public async Task<IActionResult> Index()
        {
            var notifications = await _context.Notifications
                .Include(n => n.ReceiverUser)
                .Include(n => n.Contract)
                    .ThenInclude(c => c.Room)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            var model = notifications
      .GroupBy(n => new { n.Title, n.Content, n.CreatedAt, n.SourceType })
      .Select(g => new WebAdminMVC.Models.Notifications.AdminNotificationViewModel
      {
          Id = g.First().Id,
          Title = g.Key.Title,
          Content = g.Key.Content,
          CreatedAt = g.Key.CreatedAt,
          SourceType = g.Key.SourceType,   // 👈 thêm dòng này
          TotalReceivers = g.Count(),
          TotalRead = g.Count(x => x.IsRead),
          Items = g.ToList()
      })
      .OrderByDescending(x => x.CreatedAt)
      .ToList();

            return View(model);
        }

        // =========================
        // CREATE - GET
        // =========================
        public async Task<IActionResult> Create()
        {
            await LoadContractsToViewBag();
            return View();
        }

        // =========================
        // CREATE - POST
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            string title,
            string content,
             SourceType sourceType,
            string sendToRole,
            List<int>? contractIds)
        {
            if (string.IsNullOrWhiteSpace(title) ||
                string.IsNullOrWhiteSpace(content))
            {
                TempData["Error"] = "Vui lòng nhập đầy đủ thông tin.";
                await LoadContractsToViewBag();
                return View();
            }

            var dto = new BroadcastNotificationDto
            {
                Title = title,
                Content = content,
                SourceType = sourceType
            };

            if (sendToRole == "host")
            {
                dto.SendToHost = true;
            }
            else if (sendToRole == "contract" && contractIds != null)
            {
                dto.ContractIds = contractIds;
            }

            await _notificationService.BroadcastAsync(dto);

            TempData["Success"] = "Gửi thành công!";
            return RedirectToAction(nameof(Index));
        }

        // =========================
        // DELETE - GET
        // =========================
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null)
                return NotFound();

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id);

            if (notification == null)
                return NotFound();

            return View(notification);
        }

        // =========================
        // DELETE - POST
        // =========================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var first = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id);

            if (first == null)
                return NotFound();

            var group = await _context.Notifications
                .Where(n => n.Title == first.Title
                         && n.CreatedAt == first.CreatedAt)
                .ToListAsync();

            _context.Notifications.RemoveRange(group);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã xóa toàn bộ thông báo của lần gửi này!";
            return RedirectToAction(nameof(Index));
        }

        // =========================
        // DETAILS
        // =========================
        public async Task<IActionResult> Details(long id)
        {
            var first = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id);

            if (first == null)
                return NotFound();

            var notifications = await _context.Notifications
                .Include(n => n.ReceiverUser)
                .Include(n => n.Contract)
                    .ThenInclude(c => c.Room)
                .Where(n => n.Title == first.Title
                         && n.CreatedAt == first.CreatedAt)
                .ToListAsync();

            return View(notifications);
        }

        // =========================
        // PRIVATE HELPER
        // =========================
        private async Task LoadContractsToViewBag()
        {
            var contracts = await _context.Contracts
                .Include(c => c.Tenant)
                    .ThenInclude(t => t.User)
                .Include(c => c.Room)
                .Select(c => new
                {
                    Id = c.Id,
                    TenantName = c.Tenant.FullName,
                    RoomName = c.Room.RoomNo
                })
                .ToListAsync();

            ViewBag.Contracts = contracts;
        }
    }
}