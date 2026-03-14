using BLL.DTOs.Contract;
using BLL.Services.Interfaces;
using DAL.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
namespace WebHostRazor.Pages.Host.Contracts;

public class DetailsModel : PageModel
{
    private readonly IContractService _service;

    // ✅ THÊM DbContext để đọc ContractAttachments
    private readonly AppDbContext _db;

    public DetailsModel(IContractService service, AppDbContext db)
    {
        _service = service;
        _db = db;
    }

    public ContractDto? Contract { get; set; }

    // ✅ THÊM list attachments để hiển thị
    public List<AttachmentVm> Attachments { get; set; } = new();

    public async Task<IActionResult> OnGet(int id)
    {
        try
        {
            Contract = await _service.GetByIdAsync(id);
            if (Contract != null)
            {
                DepositAmountForm.NewDeposit = Contract.Deposit;

                DepositStatusForm.DepositAmount = Contract.Deposit;
                DepositStatusForm.DepositStatus = Contract.DepositStatus ?? "Unpaid";
                DepositStatusForm.PaidAt = Contract.DepositPaidAt;
                DepositStatusForm.PaidAmount = Contract.DepositPaidAmount;
            }
            // ✅ THÊM: load attachments theo ContractId
            var contractId = (long)id;
            Attachments = await _db.ContractAttachments.AsNoTracking()
                .Where(a => a.ContractId == contractId)
                .OrderByDescending(a => a.UploadedAt)
                .Select(a => new AttachmentVm
                {
                    FileName = a.FileName ?? "",
                    Url = a.FileUrl,
                    UploadedAt = a.UploadedAt
                })
                .ToListAsync();

            return Page();
        }
        catch (Exception ex)
        {
            TempData["Err"] = ex.Message;
            return RedirectToPage("/Host/Contracts/Index");
        }
    }

    public async Task<IActionResult> OnPostActivateAsync(int id, CancellationToken ct)
    {
        try
        {
            await _service.ActivateAsync(id, actorUserId: null, ct);

            TempData["Ok"] = "Activated successfully.";
            // Backward compatibility with existing UI blocks
            TempData["Success"] = TempData["Ok"];

            return RedirectToPage(new { id });
        }
        catch (Exception ex)
        {
            TempData["Err"] = ex.Message;
            // Backward compatibility with existing UI blocks
            TempData["Error"] = TempData["Err"];

            return RedirectToPage(new { id });
        }
    }

    // ✅ THÊM: view model cho attachment
    public class AttachmentVm
    {
        public string FileName { get; set; } = "";
        public string Url { get; set; } = "";
        public DateTime UploadedAt { get; set; }
    }
    [BindProperty]
    public DepositAmountFormVm DepositAmountForm { get; set; } = new();

    [BindProperty]
    public DepositStatusFormVm DepositStatusForm { get; set; } = new();

    public class DepositAmountFormVm
    {
        public decimal NewDeposit { get; set; }
    }

    public class DepositStatusFormVm
    {
        public decimal DepositAmount { get; set; }
        public string DepositStatus { get; set; } = "Unpaid"; // Unpaid/Paid/Refunded/Forfeit
        public decimal? PaidAmount { get; set; }
        public DateTime? PaidAt { get; set; }
        public string? Note { get; set; }
    }

    private int? TryGetUserId()
    {
        var s = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("UserId");
        return int.TryParse(s, out var id) ? id : null;
    }

    public async Task<IActionResult> OnPostUpdateDepositAmountAsync(int id, CancellationToken ct)
    {
        try
        {
            var actorUserId = TryGetUserId();
            await _service.UpdateDepositAsync(id, DepositAmountForm.NewDeposit, actorUserId, ct);

            TempData["Ok"] = "Deposit amount updated.";
            TempData["Success"] = TempData["Ok"];
            return RedirectToPage(new { id });
        }
        catch (Exception ex)
        {
            TempData["Err"] = ex.Message;
            TempData["Error"] = TempData["Err"];
            return RedirectToPage(new { id });
        }
    }

    public async Task<IActionResult> OnPostUpdateDepositStatusAsync(int id, CancellationToken ct)
    {
        try
        {
            var actorUserId = TryGetUserId();

            var dto = new UpdateDepositDto
            {
                ContractId = id,
                DepositAmount = DepositStatusForm.DepositAmount,
                DepositStatus = DepositStatusForm.DepositStatus,
                PaidAmount = DepositStatusForm.PaidAmount,
                PaidAt = DepositStatusForm.PaidAt,
                Note = DepositStatusForm.Note
            };

            await _service.UpdateDepositAsync(dto, actorUserId, ct);

            TempData["Ok"] = "Deposit status updated.";
            TempData["Success"] = TempData["Ok"];
            return RedirectToPage(new { id });
        }
        catch (Exception ex)
        {
            TempData["Err"] = ex.Message;
            TempData["Error"] = TempData["Err"];
            return RedirectToPage(new { id });
        }
    }
}