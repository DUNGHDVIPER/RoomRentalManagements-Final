using System.ComponentModel.DataAnnotations;
using BLL.DTOs.Contract;
using BLL.Services.Interfaces;
using DAL.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace WebHostRazor.Pages.Host.Contracts;

public class CreateModel : PageModel
{
    private readonly IContractService _service;
    private readonly AppDbContext _db;

    public CreateModel(IContractService service, AppDbContext db)
    {
        _service = service;
        _db = db;
    }

    [BindProperty]
    public CreateVm Vm { get; set; } = new();

    public List<SelectListItem> RoomOptions { get; set; } = new();
    public List<SelectListItem> TenantOptions { get; set; } = new();

    // dùng cho JS tự fill rent
    public Dictionary<int, decimal> RoomPriceMap { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadOptionsAsync();

        if (Vm.StartDate == default) Vm.StartDate = DateTime.Today;
        if (Vm.EndDate == default) Vm.EndDate = DateTime.Today.AddMonths(6);
        Vm.ActivateNow = true;
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        await LoadOptionsAsync();

        if (!ModelState.IsValid) return Page();

        try
        {
            var dto = new CreateContractDto
            {
                RoomId = Vm.RoomId,
                TenantId = Vm.TenantId,
                StartDate = Vm.StartDate,
                EndDate = Vm.EndDate,
                Rent = Vm.Rent,
                Deposit = Vm.Deposit,
                ActivateNow = Vm.ActivateNow
            };

            var created = await _service.CreateAsync(dto, actorUserId: null, ct);

            TempData["Ok"] = "Contract created successfully.";
            TempData["Success"] = TempData["Ok"];

            return RedirectToPage("./Details", new { id = created.Id });
        }
        catch (ValidationException ex)
        {
            TempData["Err"] = ex.Message;
            TempData["Error"] = TempData["Err"];
            return Page();
        }
        catch (InvalidOperationException ex)
        {
            TempData["Err"] = ex.Message;
            TempData["Error"] = TempData["Err"];
            return Page();
        }
        catch (Exception ex)
        {
            TempData["Err"] = ex.Message;
            TempData["Error"] = TempData["Err"];
            return Page();
        }
    }

    private async Task LoadOptionsAsync()
    {
        // chỉ lấy phòng chưa có contract ACTIVE
        var activeRoomIds = await _db.Contracts
            .AsNoTracking()
            .Where(c => c.Status != null && c.Status.ToUpper() == "ACTIVE")
            .Select(c => c.RoomId)
            .Distinct()
            .ToListAsync();

        var rooms = await _db.Rooms
            .AsNoTracking()
            .Where(r => !activeRoomIds.Contains(r.RoomId))
            .OrderBy(r => r.RoomCode)
            .Select(r => new
            {
                r.RoomId,
                r.RoomCode,
                r.RoomName,
           
                r.Status
            })
            .ToListAsync();

        RoomOptions = rooms
            .Select(r => new SelectListItem
            {
                Value = r.RoomId.ToString(),
                Text = $"{r.RoomCode} - {(string.IsNullOrWhiteSpace(r.RoomName) ? "Room" : r.RoomName)}đ"
            })
            .ToList();

        //RoomPriceMap = rooms.ToDictionary(x => x.RoomId, x => x.BasePrice);

        TenantOptions = await _db.Tenants
            .AsNoTracking()
            .OrderBy(t => t.FullName)
            .Select(t => new SelectListItem
            {
                Value = t.Id.ToString(),
                Text = $"{t.FullName} - {t.Phone ?? "No phone"}"
            })
            .ToListAsync();
    }

    public class CreateVm
    {
        [Required(ErrorMessage = "Please select a room")]
        [Display(Name = "Room")]
        public int RoomId { get; set; }

        [Required(ErrorMessage = "Please select a tenant")]
        [Display(Name = "Tenant")]
        public int TenantId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Rent must be >= 0")]
        [Display(Name = "Rent")]
        public decimal Rent { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Deposit must be >= 0")]
        [Display(Name = "Deposit")]
        public decimal Deposit { get; set; }

        [Display(Name = "Activate Now")]
        public bool ActivateNow { get; set; } = true;
    }
}