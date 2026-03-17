using System.Security.Claims;
using BLL.DTOs.Contract;
using BLL.Services.Interfaces;
using DAL.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAdmin.MVC.Models.Contracts;
using System.ComponentModel.DataAnnotations;

namespace WebAdmin.MVC.Controllers;

[Authorize(Roles = "Admin")]
[Route("Contracts")]
[Route("Admin/Contracts")]
public class ContractsController : Controller
{
    private readonly AppDbContext _db;
    private readonly IContractService _contractService;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ContractsController> _logger;

    public ContractsController(
        AppDbContext db,
        IContractService contractService,
        IWebHostEnvironment env,
        ILogger<ContractsController> logger)
    {
        _db = db;
        _contractService = contractService;
        _env = env;
        _logger = logger;
    }

    private int? GetActorUserIdInt()
    {
        var s = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("UserId");
        return int.TryParse(s, out var id) ? id : null;
    }

    private void SetOk(string msg)
    {
        TempData["Ok"] = msg;
        TempData["Success"] = msg; // compatible
    }

    private void SetErr(string msg)
    {
        TempData["Err"] = msg;
        TempData["Error"] = msg; // compatible
    }

    // =========================
    // INDEX (Admin system-wide)
    // GET /Contracts  or /Admin/Contracts
    // =========================
    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(
        string? q,
        int? status,
        int? roomId,
        int? tenantId,
        DateTime? dateFrom,
        DateTime? dateTo,
        int page = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 10 : pageSize;

        var query = _db.Contracts.AsNoTracking()
            .Include(c => c.Room)
                .ThenInclude(r => r.Floor)
                    .ThenInclude(f => f.Block)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var kw = q.Trim();
            query = query.Where(c =>
                (c.ContractCode ?? "").Contains(kw) ||
                c.RoomId.ToString().Contains(kw) ||
                c.TenantId.ToString().Contains(kw) ||
                (c.Room.RoomNo ?? "").Contains(kw) ||
                (c.Room.Name ?? "").Contains(kw) ||
                (c.Room.Floor.Name ?? "").Contains(kw) ||
                (c.Room.Floor.Block.Name ?? "").Contains(kw));
        }

        if (status.HasValue)
        {
            var s = status.Value switch
            {
                0 => "PENDING",
                1 => "ACTIVE",
                2 => "EXPIRED",
                3 => "TERMINATED",
                4 => "RENEWED",
                _ => null
            };

            if (s != null)
                query = query.Where(c => c.Status != null && c.Status.ToUpper() == s);
        }

        if (roomId.HasValue) query = query.Where(c => c.RoomId == roomId.Value);
        if (tenantId.HasValue) query = query.Where(c => c.TenantId == tenantId.Value);
        if (dateFrom.HasValue) query = query.Where(c => c.StartDate.Date >= dateFrom.Value.Date);
        if (dateTo.HasValue) query = query.Where(c => c.EndDate.Date <= dateTo.Value.Date);

        var total = await query.CountAsync(ct);

        var rows = await query
            .OrderByDescending(c => c.ContractId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new ContractRowVm
            {
                ContractId = c.ContractId,
                ContractCode = c.ContractCode,
                RoomId = c.RoomId,
                TenantId = c.TenantId,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                Rent = c.BaseRent,
                Deposit = c.DepositAmount,
                Status = c.Status != null && c.Status.ToUpper() == "PENDING" ? 0 :
                         c.Status != null && c.Status.ToUpper() == "ACTIVE" ? 1 :
                         c.Status != null && c.Status.ToUpper() == "EXPIRED" ? 2 :
                         c.Status != null && c.Status.ToUpper() == "TERMINATED" ? 3 :
                         c.Status != null && c.Status.ToUpper() == "RENEWED" ? 4 : 99,
                IsActive = c.Status != null && c.Status.ToUpper() == "ACTIVE",
                BlockName = c.Room.Floor.Block.Name,
                FloorName = c.Room.Floor.Name,
                RoomNo = c.Room.RoomNo
            })
            .ToListAsync(ct);

        var vm = new ContractsIndexVm
        {
            Query = q,
            Status = status,
            RoomId = roomId,
            TenantId = tenantId,
            DateFrom = dateFrom?.Date,
            DateTo = dateTo?.Date,
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = rows
        };

        return View("Index", vm);
    }

    // =========================
    // CREATE
    // =========================
    [HttpGet("Create")]
    public IActionResult Create()
    {
        return View("Create", new ContractCreateVm());
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ContractCreateVm vm, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
            return View("Create", vm);

        try
        {
            var actorUserId = GetActorUserIdInt();

            var dto = new CreateContractDto
            {
                RoomId = vm.RoomId,
                TenantId = vm.TenantId,
                StartDate = vm.StartDate,
                EndDate = vm.EndDate,
                Rent = vm.Rent,
                Deposit = vm.Deposit,
                ActivateNow = vm.ActivateNow
            };

            var created = await _contractService.CreateAsync(dto, actorUserId, ct);

            SetOk("Created successfully.");
            return RedirectToAction(nameof(Details), new { id = created.Id });
        }
        catch (ValidationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View("Create", vm);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View("Create", vm);
        }
    }

    // =========================
    // ACTIVATE
    // =========================
    [HttpPost("Activate/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(int id, CancellationToken ct = default)
    {
        try
        {
            var actorUserId = GetActorUserIdInt();
            await _contractService.ActivateAsync(id, actorUserId: actorUserId, ct);

            SetOk("Activated successfully.");
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (ValidationException ex)
        {
            SetErr(ex.Message);
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (InvalidOperationException ex)
        {
            SetErr(ex.Message);
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (DbUpdateException)
        {
            SetErr("Room already has an ACTIVE contract.");
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    // =========================
    // DETAILS
    // =========================
    [HttpGet("Details/{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken ct = default)
    {
        try
        {
            var dto = await _contractService.GetByIdAsync(id, ct);

            var attachments = await _db.ContractAttachments.AsNoTracking()
                .Where(a => a.ContractId == (long)id)
                .OrderByDescending(a => a.UploadedAt)
                .Select(a => new AttachmentRowVm
                {
                    FileName = a.FileName,
                    Url = a.FileUrl,
                    ContentType = null,
                    Size = 0,
                    UploadedAt = a.UploadedAt
                })
                .ToListAsync(ct);

            var vm = new ContractDetailsVm
            {
                Contract = dto,
                Attachments = attachments
            };

            return View("Details", vm);
        }
        catch (InvalidOperationException ex)
        {
            SetErr(ex.Message);
            return RedirectToAction(nameof(Index));
        }
    }

    // =========================
    // ADMIN-ONLY: FORCE EXPIRE
    // =========================
    [HttpPost("ForceExpire/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForceExpire(int id, CancellationToken ct = default)
    {
        try
        {
            var actorUserId = GetActorUserIdInt();
            await _contractService.ForceExpireAsync(id, actorUserId, ct);

            SetOk("Force-expired successfully.");
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex)
        {
            SetErr(ex.Message);
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    // =========================
    // DEPOSIT UPDATE (G1/G2)
    // =========================
    [HttpPost("UpdateDepositAmount/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateDepositAmount(int id, decimal newDeposit, CancellationToken ct = default)
    {
        try
        {
            var actorUserId = GetActorUserIdInt();
            await _contractService.UpdateDepositAsync(id, newDeposit, actorUserId, ct);

            SetOk("Deposit amount updated.");
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (ValidationException ex)
        {
            SetErr(ex.Message);
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (InvalidOperationException ex)
        {
            SetErr(ex.Message);
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [HttpPost("UpdateDepositStatus/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateDepositStatus(
        int id,
        decimal depositAmount,
        string depositStatus,
        decimal? paidAmount,
        DateTime? paidAt,
        string? note,
        CancellationToken ct = default)
    {
        try
        {
            var actorUserId = GetActorUserIdInt();

            var dto = new UpdateDepositDto
            {
                ContractId = id,
                DepositAmount = depositAmount,
                DepositStatus = depositStatus,
                PaidAmount = paidAmount,
                PaidAt = paidAt,
                Note = note
            };

            await _contractService.UpdateDepositAsync(dto, actorUserId, ct);

            SetOk("Deposit status updated.");
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (ValidationException ex)
        {
            SetErr(ex.Message);
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (InvalidOperationException ex)
        {
            SetErr(ex.Message);
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    // =========================
    // RENEW
    // GET + POST
    // =========================
    [HttpGet("Renew/{id:int}")]
    public async Task<IActionResult> Renew(int id, CancellationToken ct = default)
    {
        var vm = new RenewVm { ContractId = id };
        await TryReloadRenewHeader(vm, ct);
        return View("Renew", vm);
    }

    [HttpPost("Renew/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Renew(int id, RenewVm vm, CancellationToken ct = default)
    {
        vm.ContractId = id;

        if (vm.NewEndDate < vm.NewStartDate)
            ModelState.AddModelError(nameof(vm.NewEndDate), "New end date must be after start date.");

        if (!ModelState.IsValid)
        {
            await TryReloadRenewHeader(vm, ct);
            return View("Renew", vm);
        }

        try
        {
            var actorUserId = GetActorUserIdInt();

            var dto = new RenewContractDto
            {
                ContractId = vm.ContractId,
                NewStartDate = vm.NewStartDate,
                NewEndDate = vm.NewEndDate,
                NewRent = vm.NewRent,
                NewDeposit = vm.NewDeposit,
                Reason = vm.Reason,
                RequireStartNextDay = true
            };

            var renewed = await _contractService.RenewAsync(dto, actorUserId, ct);

            SetOk("Renewed successfully.");
            return RedirectToAction(nameof(Details), new { id = renewed.Id });
        }
        catch (ValidationException ex)
        {
            ModelState.AddModelError(nameof(vm.NewStartDate), ex.Message);
            await TryReloadRenewHeader(vm, ct);
            return View("Renew", vm);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            await TryReloadRenewHeader(vm, ct);
            return View("Renew", vm);
        }
    }

    private async Task TryReloadRenewHeader(RenewVm vm, CancellationToken ct)
    {
        try
        {
            var c = await _contractService.GetByIdAsync(vm.ContractId, ct);
            vm.CurrentStatus = c.Status;
            vm.CurrentStartDate = c.StartDate;
            vm.CurrentEndDate = c.EndDate;
            vm.CurrentRent = c.Rent;
            vm.CurrentDeposit = c.Deposit;
        }
        catch { }
    }

    // =========================
    // TERMINATE
    // GET + POST
    // =========================
    [HttpGet("Terminate/{id:int}")]
    public async Task<IActionResult> Terminate(int id, CancellationToken ct = default)
    {
        var vm = new TerminateVm { ContractId = id };
        await TryReloadTerminateHeader(vm, ct);
        return View("Terminate", vm);
    }

    [HttpPost("Terminate/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Terminate(int id, TerminateVm vm, CancellationToken ct = default)
    {
        vm.ContractId = id;

        if (!ModelState.IsValid)
        {
            await TryReloadTerminateHeader(vm, ct);
            return View("Terminate", vm);
        }

        try
        {
            var actorUserId = GetActorUserIdInt();

            var dto = new TerminateContractDto
            {
                ContractId = vm.ContractId,
                TerminateDate = vm.TerminateDate,
                Reason = vm.Reason
            };

            await _contractService.TerminateAsync(dto, actorUserId, ct);

            SetOk("Terminated successfully.");
            return RedirectToAction(nameof(Details), new { id = vm.ContractId });
        }
        catch (ValidationException ex)
        {
            ModelState.AddModelError(nameof(vm.TerminateDate), ex.Message);
            await TryReloadTerminateHeader(vm, ct);
            return View("Terminate", vm);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            await TryReloadTerminateHeader(vm, ct);
            return View("Terminate", vm);
        }
    }

    private async Task TryReloadTerminateHeader(TerminateVm vm, CancellationToken ct)
    {
        try
        {
            var c = await _contractService.GetByIdAsync(vm.ContractId, ct);
            vm.CurrentStatus = c.Status;
            vm.CurrentStartDate = c.StartDate;
            vm.CurrentEndDate = c.EndDate;
            vm.CurrentRent = c.Rent;
            vm.CurrentDeposit = c.Deposit;
        }
        catch { }
    }

    // =========================
    // UPLOAD ATTACHMENT
    // =========================
    [HttpGet("UploadAttachment/{id:int}")]
    public async Task<IActionResult> UploadAttachment(int id, CancellationToken ct = default)
    {
        var vm = new UploadAttachmentVm { ContractId = id };

        vm.Attachments = await _db.ContractAttachments.AsNoTracking()
            .Where(a => a.ContractId == (long)id)
            .OrderByDescending(a => a.UploadedAt)
            .Select(a => new AttachmentRowVm
            {
                FileName = a.FileName,
                Url = a.FileUrl,
                ContentType = null,
                Size = 0,
                UploadedAt = a.UploadedAt
            })
            .ToListAsync(ct);

        return View("UploadAttachment", vm);
    }

    [HttpPost("UploadAttachment/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadAttachment(int id, IFormFile? file, CancellationToken ct = default)
    {
        if (file == null || file.Length == 0)
            ModelState.AddModelError("", "Please choose a file.");
        else
            ValidateAttachment(file);

        if (!ModelState.IsValid)
            return await UploadAttachment(id, ct);

        try
        {
            var actorUserId = GetActorUserIdInt();
            var saved = await SaveContractAttachmentAsync(id, file!, ct);

            await _contractService.AddAttachmentStubAsync(
                contractId: id,
                fileName: saved.OriginalFileName,
                url: saved.PublicUrl,
                actorUserId: actorUserId,
                ct: ct);

            SetOk("Uploaded successfully.");
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return await UploadAttachment(id, ct);
        }
    }

    private void ValidateAttachment(IFormFile file)
    {
        const long maxBytes = 5 * 1024 * 1024;
        if (file.Length > maxBytes)
        {
            ModelState.AddModelError("", "File too large. Max 5MB.");
            return;
        }

        var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        var allowedExt = new HashSet<string> { ".pdf", ".jpg", ".jpeg", ".png" };
        if (string.IsNullOrWhiteSpace(ext) || !allowedExt.Contains(ext))
        {
            ModelState.AddModelError("", "Only PDF/JPG/PNG are allowed.");
            return;
        }

        var contentType = (file.ContentType ?? "").ToLowerInvariant();
        var okCt = contentType == "application/pdf" || contentType == "image/jpeg" || contentType == "image/png";
        if (!okCt)
            ModelState.AddModelError("", "Invalid content type. Only PDF/JPG/PNG are allowed.");
    }

    private async Task<SavedAttachment> SaveContractAttachmentAsync(int contractId, IFormFile file, CancellationToken ct)
    {
        // Save into shared folder so HostRazor & AdminMVC can both serve /uploads
        var sharedContractsDir = Path.GetFullPath(
            Path.Combine(_env.ContentRootPath, "..", "SharedUploads", "contracts"));
        Directory.CreateDirectory(sharedContractsDir);

        var safeFileName = Path.GetFileName(file.FileName);
        var storedName = $"{Guid.NewGuid():N}_{safeFileName}";
        var path = Path.Combine(sharedContractsDir, storedName);

        await using var stream = System.IO.File.Create(path);
        await file.CopyToAsync(stream, ct);

        return new SavedAttachment
        {
            OriginalFileName = safeFileName,
            StoredFileName = storedName,
            PublicUrl = $"/uploads/contracts/{storedName}"
        };
    }

    private sealed class SavedAttachment
    {
        public string OriginalFileName { get; init; } = "";
        public string StoredFileName { get; init; } = "";
        public string PublicUrl { get; init; } = "";
    }

    // =========================
    // EXPORT PDF
    // =========================
    [HttpGet("ExportPdf/{id:int}")]
    public async Task<IActionResult> ExportPdf(int id, CancellationToken ct = default)
    {
        try
        {
            var actorUserId = GetActorUserIdInt();
            var bytes = await _contractService.ExportPdfStubAsync(id, actorUserId, ct);
            return File(bytes, "application/pdf", $"Contract-{id}.pdf");
        }
        catch (InvalidOperationException ex)
        {
            SetErr(ex.Message);
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    // =========================
    // VERSIONS
    // =========================
    [HttpGet("Versions/{id:int}")]
    public async Task<IActionResult> Versions(int id, CancellationToken ct = default)
    {
        try
        {
            var versions = await _contractService.GetVersionsAsync(id, ct);

            var vm = new ContractVersionsVm
            {
                ContractId = id,
                Versions = versions.Select(v => new ContractVersionVm
                {
                    VersionId = v.VersionId,
                    VersionNumber = v.VersionNumber,
                    ChangedAt = v.ChangedAt,
                    ChangedByUserId = v.ChangedByUserId,
                    ChangeNote = v.ChangeNote,
                    SnapshotJson = v.SnapshotJson
                }).ToList()
            };

            return View("Versions", vm);
        }
        catch (InvalidOperationException ex)
        {
            SetErr(ex.Message);
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    // =========================
    // REMINDER SCAN
    // =========================
    [HttpGet("ReminderScan")]
    public IActionResult ReminderScan(int daysBeforeEnd = 7)
    {
        ViewBag.DaysBeforeEnd = daysBeforeEnd;
        return View("ReminderScan");
    }

    [HttpPost("ReminderScan")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReminderScanPost(int daysBeforeEnd, CancellationToken ct = default)
    {
        try
        {
            var actorUserId = GetActorUserIdInt();

            var sent = await _contractService.ScanAndSendExpiryRemindersAsync(
                daysBeforeEnd: daysBeforeEnd,
                remindType: $"Expiry_{daysBeforeEnd}d",
                actorUserId: actorUserId,
                ct: ct);

            SetOk($"Scan done. Sent {sent} reminder(s).");
            return RedirectToAction(nameof(ReminderScan), new { daysBeforeEnd });
        }
        catch (Exception ex)
        {
            SetErr(ex.Message);
            return RedirectToAction(nameof(ReminderScan), new { daysBeforeEnd });
        }
    }
}