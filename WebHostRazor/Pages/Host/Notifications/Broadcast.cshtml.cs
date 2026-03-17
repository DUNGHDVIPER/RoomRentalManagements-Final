using System.Text.Json;
using BLL.DTOs.Notification;
using DAL.Data;
using DAL.Entities.Common;
using DAL.Entities.System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using WebHostRazor.Hubs;

namespace WebHostRazor.Pages.Host.Notifications
{
    public class BroadcastModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<NotificationHub> _hub;
        public string ContractsJson { get; set; }
        public string BlocksJson { get; set; }
        public string FloorsJson { get; set; }
        public BroadcastModel(AppDbContext context,
                              IHubContext<NotificationHub> hub)
        {
            _context = context;
            _hub = hub;
        }

        [BindProperty]
        public BroadcastNotificationDto Input { get; set; }
        public List<SelectListItem> SourceTypeList { get; set; } = new();

        public void OnGet()
        {
            Input = new BroadcastNotificationDto();

            // SourceType dropdown
            SourceTypeList = Enum.GetValues(typeof(SourceType))
                .Cast<SourceType>()
                .Select(x => new SelectListItem
                {
                    Value = ((int)x).ToString(),
                    Text = x switch
                    {
                        SourceType.Manual => "📢 Thông báo chung",
                        SourceType.Maintenance => "🔧 Bảo trì / Sự cố",
                        SourceType.System => "🚨 Hệ Thống",
                        SourceType.Billing => "💳 Thanh toán",
                        _ => x.ToString()
                    }
                }).ToList();

            // 🔥 LOAD CONTRACT + TENANT + ROOM + FLOOR + BLOCK
            var contracts = _context.Contracts
      .Include(c => c.Tenant)
      .Include(c => c.Room)
          .ThenInclude(r => r.Floor)
              .ThenInclude(f => f.Block)
      .Where(c => c.Status == "Active")
      .Select(c => new
      {
          id = c.ContractId,
          tenant = c.Tenant.FullName,
          room = c.Room.Name,
          blockId = c.Room.Floor.BlockId,
          floorId = c.Room.FloorId
      })
      .ToList();

            var blocks = _context.Blocks
                .Select(b => new { id = b.Id, name = b.Name })
                .ToList();

            var floors = _context.Floors
                .Select(f => new { id = f.Id, name = f.Name, blockId = f.BlockId })
                .ToList();

            ContractsJson = JsonSerializer.Serialize(contracts);
            BlocksJson = JsonSerializer.Serialize(blocks);
            FloorsJson = JsonSerializer.Serialize(floors);
        }
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            if (Input.ContractIds == null || !Input.ContractIds.Any())
            {
                ModelState.AddModelError("", "Vui lòng chọn ít nhất 1 tenant.");
                return Page();
            }

            foreach (var contractId in Input.ContractIds)
            {
                var notification = new Notification
                {
                    Title = Input.Title,
                    Content = Input.Content,
                    SourceType = Input.SourceType,
                    IsRead = false,
                    ContractId = contractId
                };

                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();

            foreach (var contractId in Input.ContractIds)
            {
                await _hub.Clients
                    .Group($"Contract_{contractId}")
                    .SendAsync("ReceiveNotification",
                        Input.Title,
                        Input.Content);
            }

            TempData["Success"] = "Gửi thông báo thành công!";
            return RedirectToPage();
        }
    }
}