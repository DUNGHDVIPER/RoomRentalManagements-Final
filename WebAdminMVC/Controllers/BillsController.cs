using BLL.DTOs.Billing;
using BLL.Services.Interfaces;
using DAL.Entities.Billing;
using DAL.Entities.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebAdmin.MVC.Models.Billing;

namespace WebAdmin.MVC.Controllers;

public class BillsController : Controller
{
    private readonly IBillingService _billing;

    public BillsController(IBillingService billing)
    {
        _billing = billing;
    }

    // =========================
    // LIST
    // =========================
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [HttpGet]
    public async Task<IActionResult> Index(string? q, string? status, string? month, CancellationToken ct)
    {
        var dtos = await _billing.GetBillsAsync(q, status, month, ct);

        var list = dtos.Select(b => new BillListItemVm
        {
            Id = b.Id,
            RoomName = b.RoomName ?? "-",
            Month = PeriodToMonth(b.Period),
            Total = b.TotalAmount,
            Status = ToUiStatus((BillStatus)b.Status, b.DueDate)
        }).ToList();

        return View(list);
    }

    // =========================
    // DETAILS
    // =========================
    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var dto = await _billing.GetBillAsync(id, ct);
        if (dto == null) return NotFound();

        var vm = new BillDetailsVm
        {
            Id = dto.Id,
            RoomName = dto.RoomName ?? "-",
            Month = PeriodToMonth(dto.Period),
            Total = dto.TotalAmount,
            Status = ToUiStatus((BillStatus)dto.Status, dto.DueDate),

            // bạn có thể đổi IssuedAt -> CreatedAtUtc nếu VM muốn
            CreatedAtUtc = dto.IssuedAt,

            Items = (dto.Items ?? new List<BillItemDto>()).Select(i => new BillItemLineVm
            {
                Id = i.Id,
                Name = i.Name,
                Amount = i.Amount,
                ExtraFeeId = i.ExtraFeeId
            }).ToList(),

            Payments = (dto.Payments ?? new List<PaymentDto>()).Select(p => new PaymentHistoryItemVm
            {
                Id = p.Id,
                Amount = p.Amount,
                Method = p.Method,
                Status = ((PaymentStatus)p.Status).ToString(),
                PaidAtUtc = p.PaidAt,
                TransactionRef = p.TransactionRef
            }).ToList()
        };

        return View(vm);
    }

    // =========================
    // CREATE
    // =========================
    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        ViewBag.RoomOptions = await _billing.GetActiveRoomOptionsAsync(ct);

        return View(new BillCreateVm
        {
            Month = DateTime.Today.ToString("yyyy-MM"),
            Status = "Unpaid"
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BillCreateVm vm, CancellationToken ct)
    {
        // Validate basic (UI)
        if (!TryParseMonth(vm.Month, out _))
            ModelState.AddModelError(nameof(vm.Month), "Month phải theo định dạng yyyy-MM (vd: 2026-02).");

        if (string.IsNullOrWhiteSpace(vm.RoomName))
            ModelState.AddModelError(nameof(vm.RoomName), "Vui lòng chọn phòng.");

        if (!IsUiStatusValid(vm.Status))
            ModelState.AddModelError(nameof(vm.Status), "Status chỉ nhận: Unpaid, Paid, Overdue.");

        if (!ModelState.IsValid)
        {
            ViewBag.RoomOptions = await _billing.GetActiveRoomOptionsAsync(ct);
            return View(vm);
        }

        var (ok, err) = await _billing.CreateBillAsync(vm.RoomName.Trim(), vm.Month, vm.Status, vm.Total, ct);
        if (!ok)
        {
            ModelState.AddModelError("", err ?? "Tạo bill thất bại.");
            ViewBag.RoomOptions = await _billing.GetActiveRoomOptionsAsync(ct);
            return View(vm);
        }

        TempData["msg"] = "Tạo bill thành công.";
        return RedirectToAction(nameof(Index));
    }

    // =========================
    // EDIT
    // =========================
    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var dto = await _billing.GetBillAsync(id, ct);
        if (dto == null) return NotFound();

        var vm = new BillEditVm
        {
            Id = dto.Id,
            RoomName = dto.RoomName ?? "-",
            Month = PeriodToMonth(dto.Period),
            Total = dto.TotalAmount,
            Status = ToUiStatus((BillStatus)dto.Status, dto.DueDate)
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(BillEditVm vm, CancellationToken ct)
    {
        if (!TryParseMonth(vm.Month, out _))
            ModelState.AddModelError(nameof(vm.Month), "Month phải theo định dạng yyyy-MM (vd: 2026-02).");

        if (!IsUiStatusValid(vm.Status))
            ModelState.AddModelError(nameof(vm.Status), "Status chỉ nhận: Unpaid, Paid, Overdue.");

        if (!ModelState.IsValid) return View(vm);

        var (ok, err) = await _billing.UpdateBillAsync(vm.Id, vm.Month, vm.Status, vm.Total, ct);
        if (!ok)
        {
            ModelState.AddModelError("", err ?? "Cập nhật bill thất bại.");
            return View(vm);
        }

        TempData["msg"] = "Cập nhật bill thành công.";
        return RedirectToAction(nameof(Index));
    }

    // =========================
    // DELETE
    // =========================
    [HttpGet]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var dto = await _billing.GetBillAsync(id, ct);
        if (dto == null) return NotFound();

        var vm = new BillDetailsVm
        {
            Id = dto.Id,
            RoomName = dto.RoomName ?? "-",
            Month = PeriodToMonth(dto.Period),
            Total = dto.TotalAmount,
            Status = ToUiStatus((BillStatus)dto.Status, dto.DueDate),
            CreatedAtUtc = dto.IssuedAt
        };

        return View(vm);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken ct)
    {
        var (ok, err) = await _billing.DeleteBillAsync(id, ct);
        if (!ok)
        {
            TempData["msg"] = err ?? "Xóa bill thất bại.";
            return RedirectToAction(nameof(Index));
        }

        TempData["msg"] = "Xóa bill thành công.";
        return RedirectToAction(nameof(Index));
    }

    // =========================
    // RECORD PAYMENT
    // =========================
    [HttpGet]
    public async Task<IActionResult> RecordPayment(int id, CancellationToken ct)
    {
        var dto = await _billing.GetBillAsync(id, ct);
        if (dto == null) return NotFound();

        var paidSoFar = (dto.Payments ?? new List<PaymentDto>())
            .Where(p => (PaymentStatus)p.Status == PaymentStatus.Completed)
            .Sum(p => p.Amount);

        var vm = new PaymentCreateVm
        {
            BillId = dto.Id,
            RoomName = dto.RoomName ?? "-",
            Month = PeriodToMonth(dto.Period),
            BillTotal = dto.TotalAmount,
            PaidSoFar = paidSoFar,
            Amount = Math.Max(0, dto.TotalAmount - paidSoFar)
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordPayment(PaymentCreateVm vm, CancellationToken ct)
    {
        if (vm.Amount <= 0)
            ModelState.AddModelError(nameof(vm.Amount), "Số tiền phải lớn hơn 0.");

        if (!ModelState.IsValid) return View(vm);

        var dto = new RecordPaymentDto
        {
            BillId = vm.BillId,
            Amount = vm.Amount,
            Method = vm.Method,
            TransactionRef = vm.TransactionRef,
            PaidAt = DateTime.UtcNow
        };

        var (ok, err) = await _billing.RecordPaymentAsync(dto, ct);
        if (!ok)
        {
            ModelState.AddModelError("", err ?? "Thanh toán thất bại.");
            return View(vm);
        }

        TempData["msg"] = "Thanh toán thành công.";
        return RedirectToAction(nameof(Details), new { id = vm.BillId });
    }

    // =========================
    // GENERATE BILLS
    // =========================
    [HttpGet]
    public async Task<IActionResult> GenerateBatch(string? month, CancellationToken ct)
    {
        ViewBag.ExtraFees = await _billing.GetActiveExtraFeesAsync(ct);

        // default month
        var m = string.IsNullOrWhiteSpace(month) ? DateTime.Today.ToString("yyyy-MM") : month;
        var period = TryParseMonth(m, out var p) ? p : (DateTime.Today.Year * 100 + DateTime.Today.Month);

        var y = period / 100;
        var mm = period % 100;

        var vm = new BillGenerateBatchVm
        {
            Month = $"{y:D4}-{mm:D2}",
            DueDate = new DateTime(y, mm, DateTime.DaysInMonth(y, mm)),
            IncludeRent = true,
            IncludeUtilities = true,
            ExtraFeeIds = new List<int>()
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateBatch(BillGenerateBatchVm vm, CancellationToken ct)
    {
        ViewBag.ExtraFees = await _billing.GetActiveExtraFeesAsync(ct);

        if (!TryParseMonth(vm.Month, out var period))
            ModelState.AddModelError(nameof(vm.Month), "Month phải có dạng yyyy-MM (vd: 2026-02).");

        if (vm.DueDate == default)
            ModelState.AddModelError(nameof(vm.DueDate), "Vui lòng chọn Due Date.");

        if (!ModelState.IsValid) return View(vm);

        // validate due date in month
        var y = period / 100;
        var m = period % 100;
        var firstDay = new DateTime(y, m, 1);
        var lastDay = new DateTime(y, m, DateTime.DaysInMonth(y, m));

        if (vm.DueDate < firstDay || vm.DueDate > lastDay)
        {
            ModelState.AddModelError(nameof(vm.DueDate), $"Due Date phải nằm trong tháng {y:D4}-{m:D2}.");
            return View(vm);
        }

        var req = new GenerateBillsRequestDto
        {
            Period = period,
            DueDate = vm.DueDate,
            BlockId = null,
            FloorId = null
        };

        var extraFeeIds = vm.ExtraFeeIds ?? new List<int>();

        var (ok, err, created, skipped) =
            await _billing.GenerateBillsAsync(req, extraFeeIds, vm.IncludeRent, vm.IncludeUtilities, ct);

        if (!ok)
        {
            ModelState.AddModelError("", err ?? "Generate thất bại.");
            return View(vm);
        }

        TempData["success"] = $"Generate xong {vm.Month} (Period {period}): tạo {created} bill, bỏ qua {skipped} bill.";
        return RedirectToAction(nameof(Index), new { month = vm.Month, t = DateTime.UtcNow.Ticks });
    }

    // =========================
    // Helpers (MVC)
    // =========================

    private static bool TryParseMonth(string? month, out int period)
    {
        period = 0;
        if (string.IsNullOrWhiteSpace(month)) return false;

        if (!DateTime.TryParseExact(month.Trim(), "yyyy-MM",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var dt))
            return false;

        period = dt.Year * 100 + dt.Month;
        return true;
    }

    private static string PeriodToMonth(int period)
        => $"{period / 100:D4}-{period % 100:D2}";

    private static bool IsUiStatusValid(string? ui)
    {
        var s = NormalizeUiStatus(ui);
        return s is "Unpaid" or "Paid" or "Overdue";
    }

    private static string NormalizeUiStatus(string? ui)
    {
        if (string.IsNullOrWhiteSpace(ui)) return "Unpaid";
        var s = ui.Trim();
        if (s.Equals("paid", StringComparison.OrdinalIgnoreCase)) return "Paid";
        if (s.Equals("overdue", StringComparison.OrdinalIgnoreCase)) return "Overdue";
        return "Unpaid";
    }

    private static string ToUiStatus(BillStatus status, DateTime dueDateUtc)
    {
        if (status == BillStatus.Paid) return "Paid";
        if (dueDateUtc < DateTime.UtcNow) return "Overdue";
        return "Unpaid";
    }

    
   
}