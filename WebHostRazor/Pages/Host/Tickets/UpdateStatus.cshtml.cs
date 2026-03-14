using DAL.Data;
using DAL.Entities.Maintenance;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using DAL.Entities.Common;

public class UpdateStatusModel : PageModel
{
    private readonly AppDbContext _context;

    public UpdateStatusModel(AppDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Ticket Ticket { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Ticket = await _context.Tickets.FindAsync(id);

        if (Ticket == null)
            return NotFound();

        return Page();
    }

    public async Task<IActionResult> OnPostUpdateStatusAsync(int id, int status)
    {
        var ticket = await _context.Tickets.FindAsync(id);

        if (ticket == null)
            return new JsonResult(new { success = false, message = "Ticket not found" });

        // ✅ Kiểm tra status có hợp lệ không
        if (!Enum.IsDefined(typeof(TicketStatus), status))
            return new JsonResult(new { success = false, message = "Invalid status" });

        ticket.Status = (TicketStatus)status;
        ticket.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return new JsonResult(new { success = true });
    }
}