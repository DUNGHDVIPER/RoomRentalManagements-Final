using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DAL.Data;
using WebAdmin.MVC.Models.Audit;

namespace WebAdmin.MVC.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
public class AuditController : Controller
{
    private readonly AppDbContext _db;
    private readonly ILogger<AuditController> _logger;

    public AuditController(
        AppDbContext db,
        ILogger<AuditController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(AuditFilterVm filter)
    {
        var query = _db.AuditLogs
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var kw = filter.Keyword.Trim();
            query = query.Where(x =>
                x.Action.Contains(kw) ||
                x.EntityType.Contains(kw) ||
                (x.Note != null && x.Note.Contains(kw)) ||
                x.EntityId.Contains(kw));
        }

        if (filter.FromUtc.HasValue)
            query = query.Where(x => x.CreatedAt >= filter.FromUtc.Value);

        if (filter.ToUtc.HasValue)
            query = query.Where(x => x.CreatedAt <= filter.ToUtc.Value);

        var model = await query
            .OrderByDescending(x => x.CreatedAt)
            .Take(200)
            .Select(x => new AuditListItemVm
            {
                Id = x.Id,
                TimeUtc = x.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                Action = x.Action,
                Entity = $"{x.EntityType} #{x.EntityId}",
                User = x.ActorUserId.HasValue ? x.ActorUserId.Value.ToString() : "System"
            })
            .ToListAsync();

        ViewBag.Filter = filter;
        return View(model);
    }
}