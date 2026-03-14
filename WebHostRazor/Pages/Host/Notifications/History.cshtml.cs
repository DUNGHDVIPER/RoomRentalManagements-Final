using Microsoft.AspNetCore.Mvc.RazorPages;
using BLL.Services;
using BLL.DTOs.Notification;
using DAL.Data;
using DAL.Entities.System;
using Microsoft.EntityFrameworkCore;
namespace WebHostRazor.Pages.Host.Notifications
{
    public class HistoryModel : PageModel
    {
        private readonly AppDbContext _context;

        public HistoryModel(AppDbContext context)
        {
            _context = context;
        }

        public List<Notification> Notifications { get; set; }

        public async Task OnGetAsync()
        {
            Notifications = await _context.Notifications
    .Include(n => n.Contract)
        .ThenInclude(c => c.Tenant)
    .OrderByDescending(x => x.CreatedAt)
    .ToListAsync();
        }
    }
}