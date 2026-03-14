using DAL.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace WebHostRazor.Pages.Host.Contracts;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(AppDbContext db, ILogger<IndexModel> logger)
    {
        _db = db;
        _logger = logger;
    }

    public List<ContractRowVm> Items { get; set; } = new();

    public string DbName { get; set; } = "";
    public string DbServer { get; set; } = "";
    public int TotalContracts { get; set; }

    public async Task OnGet()
    {
        DbName = _db.Database.GetDbConnection().Database;
        DbServer = _db.Database.GetDbConnection().DataSource;

        _logger.LogInformation("Contracts Index using DB: {Server} | {Db}", DbServer, DbName);

        TotalContracts = await _db.Contracts.AsNoTracking().CountAsync();

        Items = await _db.Contracts.AsNoTracking()
            .OrderByDescending(c => c.ContractId)
            .Select(c => new ContractRowVm
            {
                Id = c.ContractId,
                RoomId = c.RoomId,
                TenantId = c.TenantId,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                Rent = c.BaseRent,
                Deposit = c.DepositAmount,

                // Status có thể là string hoặc enum -> ToString() đều chạy
                StatusText = c.Status.ToString(),

                IsActive = c.Status.ToString() == "Active"
            })
            .ToListAsync();
    }

    public class ContractRowVm
    {
        public long Id { get; set; }
        public int RoomId { get; set; }
        public int TenantId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal Rent { get; set; }
        public decimal Deposit { get; set; }

        public string StatusText { get; set; } = "";
        public bool IsActive { get; set; }
    }
}
