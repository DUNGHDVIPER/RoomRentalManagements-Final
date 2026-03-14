using DAL.Data;
using DAL.Entities.Maintenance;
using DAL.Entities.Common;
using BLL.DTOs.Notification;
using BLL.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
namespace WebHostRazor.Pages.Host.Tickets;
public class IndexModel : PageModel
{
    private readonly AppDbContext _context;

    public IndexModel(AppDbContext context)
    {
        _context = context;
    }

    public IList<Ticket> Tickets { get; set; } = new List<Ticket>();

    // 🔎 FILTER
    [BindProperty(SupportsGet = true)]
    public string? SearchString { get; set; }

    [BindProperty(SupportsGet = true)]
    public TicketCategory? Category { get; set; }

    [BindProperty(SupportsGet = true)]
    public TicketStatus? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? FromDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? ToDate { get; set; }

    public string? FromDateString => FromDate?.ToString("yyyy-MM-dd");
    public string? ToDateString => ToDate?.ToString("yyyy-MM-dd");

    // 📄 PAGINATION
    public int PageIndex { get; set; } = 1;
    public int TotalPages { get; set; }
    public int PageSize { get; set; } = 5;

    public async Task OnGetAsync(int? pageIndex)
    {
        if (pageIndex.HasValue)
            PageIndex = pageIndex.Value;

        var query = _context.Tickets
            .Include(t => t.Room)
            .Include(t => t.Tenant)
            .AsQueryable();

        // 🔍 SEARCH
        if (!string.IsNullOrEmpty(SearchString))
        {
            query = query.Where(t =>
                t.Title.Contains(SearchString) ||
                t.Id.ToString().Contains(SearchString) ||
                t.Room.RoomName.Contains(SearchString));
        }

        // 🎯 CATEGORY ENUM
        // 🎯 CATEGORY ENUM
        if (Category.HasValue)
        {
            query = query.Where(t => t.Category == Category.Value);
        }

        // 📌 STATUS ENUM
        if (Status.HasValue)
        {
            query = query.Where(t => t.Status == Status.Value);
        }

        // 📅 DATE FILTER
        if (FromDate.HasValue)
        {
            query = query.Where(t => t.CreatedAt >= FromDate.Value);
        }

        if (ToDate.HasValue)
        {
            query = query.Where(t => t.CreatedAt <= ToDate.Value);
        }

        // COUNT FOR PAGINATION
        var count = await query.CountAsync();
        TotalPages = (int)Math.Ceiling(count / (double)PageSize);

        // APPLY PAGING
        Tickets = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((PageIndex - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();
    }

    // 🔥 AJAX UPDATE STATUS
    public async Task<IActionResult> OnPostUpdateStatusAsync(int id, int status)
    {
        var ticket = await _context.Tickets.FindAsync(id);

        if (ticket == null)
            return new JsonResult(new { success = false });

        ticket.Status = (TicketStatus)status;
        ticket.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return new JsonResult(new { success = true });
    }
}