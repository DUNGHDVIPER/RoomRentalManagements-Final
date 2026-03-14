using BLL.DTOs.Billing;
using BLL.Services.Interfaces;
using DAL.Data;
using DAL.Entities.Billing;
using DAL.Entities.Common;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services;

public class BillingService : IBillingService
{
    private readonly AppDbContext _db;
    private readonly IUtilityService _utility;

    public BillingService(AppDbContext db, IUtilityService utility)
    {
        _db = db;
        _utility = utility;
    }

    public async Task<List<BillDto>> GetBillsAsync(string? q, string? status, string? month, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        await _db.Bills
            .Where(b => b.Status == BillStatus.Issued && b.DueDate < now)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(b => b.Status, BillStatus.Overdue)
                .SetProperty(b => b.UpdatedAt, now), ct);

        var query = _db.Bills
            .AsNoTracking()
            .Include(b => b.Contract)
                .ThenInclude(c => c.Room)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var kw = q.Trim();
            query = query.Where(b =>
                b.Contract.Room.RoomCode.Contains(kw));
        }

        if (!string.IsNullOrWhiteSpace(month) && TryParseMonth(month, out var period))
        {
            query = query.Where(b => b.Period == period);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var s = NormalizeUiStatus(status);
            query = s switch
            {
                "Paid" => query.Where(b => b.Status == BillStatus.Paid),
                "Overdue" => query.Where(b => b.Status == BillStatus.Overdue),
                _ => query.Where(b => b.Status == BillStatus.Issued)
            };
        }

        return await query
            .OrderByDescending(b => b.Period)
            .ThenBy(b => b.Contract.Room.RoomCode)
            .Select(b => new BillDto
            {
                Id = b.Id,
                ContractId = (int)b.ContractId,
                Period = b.Period,
                IssuedAt = b.IssuedAt,
                DueDate = b.DueDate,
                Status = b.Status.ToString(),
                TotalAmount = b.TotalAmount,
                RoomName = b.Contract.Room.RoomCode,
                Items = new List<BillItemDto>(),
                Payments = new List<PaymentDto>()
            })
            .ToListAsync(ct);
    }

    public async Task<BillDto?> GetBillAsync(int id, CancellationToken ct)
    {
        var b = await _db.Bills
            .AsNoTracking()
            .Include(x => x.Contract)
                .ThenInclude(c => c.Room)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (b == null) return null;

        var items = await _db.BillItems
            .AsNoTracking()
            .Where(i => i.BillId == id)
            .OrderBy(i => i.ExtraFeeId.HasValue)
            .ThenBy(i => i.Name)
            .Select(i => new BillItemDto
            {
                Id = i.Id,
                BillId = i.BillId,
                Name = i.Name,
                Amount = i.Amount,
                ExtraFeeId = i.ExtraFeeId
            })
            .ToListAsync(ct);

        var payments = await _db.Payments
            .AsNoTracking()
            .Where(p => p.BillId == id)
            .OrderByDescending(p => p.PaidAt ?? p.CreatedAt)
            .Select(p => new PaymentDto
            {
                Id = p.Id,
                BillId = p.BillId,
                Amount = p.Amount,
                Method = p.Method,
                Status = p.Status.ToString(),
                PaidAt = p.PaidAt,
                TransactionRef = p.TransactionRef
            })
            .ToListAsync(ct);

        return new BillDto
        {
            Id = b.Id,
            ContractId = (int)b.ContractId,
            Period = b.Period,
            IssuedAt = b.IssuedAt,
            DueDate = b.DueDate,
            Status = b.Status.ToString(),
            TotalAmount = b.TotalAmount,
            RoomName = b.Contract.Room.RoomCode,
            Items = items,
            Payments = payments
        };
    }

    public async Task<List<SelectListItem>> GetActiveRoomOptionsAsync(CancellationToken ct)
    {
        var rooms = await _db.Contracts
            .AsNoTracking()
            .Where(c => c.Status == "Active")
            .Include(c => c.Room)
            .Select(c => c.Room)
            .Distinct()
            .OrderBy(r => r.RoomCode)
            .ToListAsync(ct);

        return rooms.Select(r => new SelectListItem
        {
            Value = r.RoomCode,
            Text = r.RoomCode
        }).ToList();
    }

    public async Task<List<ExtraFee>> GetActiveExtraFeesAsync(CancellationToken ct)
    {
        return await _db.ExtraFees
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);
    }

    public async Task<(bool Ok, string? Error)> CreateBillAsync(string roomNo, string month, string uiStatus, decimal total, CancellationToken ct)
    {
        if (!TryParseMonth(month, out var period))
            return (false, "Month phải theo định dạng yyyy-MM (vd: 2026-02).");

        if (string.IsNullOrWhiteSpace(roomNo))
            return (false, "Vui lòng chọn phòng.");

        if (!IsUiStatusValid(uiStatus))
            return (false, "Status chỉ nhận: Unpaid, Paid, Overdue.");

        var roomKey = roomNo.Trim();

        var contract = await _db.Contracts
            .Include(c => c.Room)
            .Where(c => c.Status == "Active")
            .OrderByDescending(c => c.ContractId)
            .FirstOrDefaultAsync(c => c.Room.RoomCode == roomKey, ct);

        if (contract == null)
            return (false, "Không tìm thấy phòng hoặc chưa có hợp đồng active cho phòng này.");

        var exists = await _db.Bills
            .AnyAsync(b => b.ContractId == contract.ContractId && b.Period == period, ct);

        if (exists)
            return (false, "Bill của phòng này trong tháng này đã tồn tại.");

        var now = DateTime.UtcNow;

        var bill = new Bill
        {
            ContractId = contract.ContractId,
            Period = period,
            IssuedAt = now,
            DueDate = now.AddDays(7),
            Status = FromUiStatus(uiStatus),
            TotalAmount = total,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.Bills.Add(bill);
        await _db.SaveChangesAsync(ct);

        return (true, null);
    }

    public async Task UpdateBillStatusAsync(int billId, int status, CancellationToken ct = default)
    {
        var bill = await _db.Bills.FirstOrDefaultAsync(x => x.Id == billId, ct);
        if (bill == null) return;

        bill.Status = (BillStatus)status;
        bill.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<(bool Ok, string? Error)> UpdateBillAsync(int id, string month, string uiStatus, decimal total, CancellationToken ct)
    {
        if (!TryParseMonth(month, out var period))
            return (false, "Month phải theo định dạng yyyy-MM (vd: 2026-02).");

        if (!IsUiStatusValid(uiStatus))
            return (false, "Status chỉ nhận: Unpaid, Paid, Overdue.");

        var bill = await _db.Bills.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (bill == null) return (false, "Bill không tồn tại.");

        bill.Period = period;
        bill.TotalAmount = total;
        bill.Status = FromUiStatus(uiStatus);
        bill.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> DeleteBillAsync(int id, CancellationToken ct)
    {
        var bill = await _db.Bills.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (bill == null) return (false, "Bill không tồn tại.");

        _db.BillItems.RemoveRange(_db.BillItems.Where(x => x.BillId == id));
        _db.Payments.RemoveRange(_db.Payments.Where(x => x.BillId == id));
        _db.Bills.Remove(bill);

        await _db.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> RecordPaymentAsync(RecordPaymentDto dto, CancellationToken ct)
    {
        var bill = await _db.Bills.FirstOrDefaultAsync(b => b.Id == dto.BillId, ct);
        if (bill == null) return (false, "Bill không tồn tại.");

        if (dto.Amount <= 0) return (false, "Số tiền phải lớn hơn 0.");

        var now = DateTime.UtcNow;

        var payment = new Payment
        {
            BillId = dto.BillId,
            Amount = dto.Amount,
            Method = dto.Method,
            Status = PaymentStatus.Completed,
            PaidAt = dto.PaidAt ?? now,
            TransactionRef = dto.TransactionRef,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync(ct);

        var totalPaid = await _db.Payments
            .Where(p => p.BillId == dto.BillId && p.Status == PaymentStatus.Completed)
            .SumAsync(p => (decimal?)p.Amount, ct) ?? 0m;

        if (totalPaid >= bill.TotalAmount)
            bill.Status = BillStatus.Paid;
        else if (bill.DueDate < now)
            bill.Status = BillStatus.Overdue;
        else
            bill.Status = BillStatus.Issued;

        bill.UpdatedAt = now;
        await _db.SaveChangesAsync(ct);

        return (true, null);
    }

    public async Task<(bool Ok, string? Error, int Created, int Skipped)> GenerateBillsAsync(
        GenerateBillsRequestDto req,
        List<int> extraFeeIds,
        bool includeRent,
        bool includeUtilities,
        CancellationToken ct)
    {
        var period = req.Period;
        if (period <= 0) return (false, "Period không hợp lệ (YYYYMM).", 0, 0);

        var y = period / 100;
        var m = period % 100;
        if (m < 1 || m > 12) return (false, "Period không hợp lệ (YYYYMM).", 0, 0);

        if (req.DueDate == default) return (false, "Vui lòng chọn Due Date.", 0, 0);

        var firstDay = new DateTime(y, m, 1);
        var lastDay = new DateTime(y, m, DateTime.DaysInMonth(y, m));
        if (req.DueDate < firstDay || req.DueDate > lastDay)
            return (false, $"Due Date phải nằm trong tháng {y:D4}-{m:D2}.", 0, 0);

        var contractRows = await _db.Contracts
            .AsNoTracking()
            .Where(c => c.Status == "Active")
            .Select(c => new
            {
                c.ContractId,
                c.RoomId,
                Rent = c.BaseRent
            })
            .ToListAsync(ct);

        if (req.FloorId.HasValue)
        {
            var allowedRoomIds = await _db.Rooms
                .AsNoTracking()
                .Where(r => r.FloorId == req.FloorId.Value)
                .Select(r => r.RoomId)
                .ToListAsync(ct);

            contractRows = contractRows
                .Where(c => allowedRoomIds.Contains(c.RoomId))
                .ToList();
        }

        if (req.BlockId.HasValue)
        {
            var allowedRoomIds = await _db.Rooms
                .AsNoTracking()
                .Where(r => r.Floor.BlockId == req.BlockId.Value)
                .Select(r => r.RoomId)
                .ToListAsync(ct);

            contractRows = contractRows
                .Where(c => allowedRoomIds.Contains(c.RoomId))
                .ToList();
        }

        var existingContractIds = (await _db.Bills
                .AsNoTracking()
                .Where(b => b.Period == period)
                .Select(b => b.ContractId)
                .ToListAsync(ct))
            .ToHashSet();

        var fees = extraFeeIds is { Count: > 0 }
            ? await _db.ExtraFees
                .AsNoTracking()
                .Where(x => x.IsActive && extraFeeIds.Contains(x.Id))
                .ToListAsync(ct)
            : new List<ExtraFee>();

        var now = DateTime.UtcNow;
        var created = 0;
        var skipped = 0;

        foreach (var c in contractRows)
        {
            if (existingContractIds.Contains(c.ContractId))
            {
                skipped++;
                continue;
            }

            var bill = new Bill
            {
                ContractId = c.ContractId,
                Period = period,
                DueDate = req.DueDate,
                Status = BillStatus.Issued,
                IssuedAt = now,
                CreatedAt = now,
                UpdatedAt = now,
                TotalAmount = 0m
            };

            _db.Bills.Add(bill);
            await _db.SaveChangesAsync(ct);

            if (includeRent)
            {
                _db.BillItems.Add(new BillItem
                {
                    BillId = bill.Id,
                    Name = "Rent",
                    Amount = c.Rent
                });
            }

            if (includeUtilities)
            {
                try
                {
                    var charges = await _utility.CalculateChargesAsync(c.RoomId, period, ct);

                    if (charges.ElectricAmount > 0)
                    {
                        _db.BillItems.Add(new BillItem
                        {
                            BillId = bill.Id,
                            Name = "Electric",
                            Amount = charges.ElectricAmount
                        });
                    }

                    if (charges.WaterAmount > 0)
                    {
                        _db.BillItems.Add(new BillItem
                        {
                            BillId = bill.Id,
                            Name = "Water",
                            Amount = charges.WaterAmount
                        });
                    }
                }
                catch
                {
                }
            }

            foreach (var fee in fees)
            {
                _db.BillItems.Add(new BillItem
                {
                    BillId = bill.Id,
                    Name = fee.Name,
                    Amount = fee.DefaultAmount,
                    ExtraFeeId = fee.Id
                });
            }

            await _db.SaveChangesAsync(ct);

            bill.TotalAmount = await _db.BillItems
                .Where(x => x.BillId == bill.Id)
                .SumAsync(x => x.Amount, ct);

            bill.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            created++;
        }

        return (true, null, created, skipped);
    }

    private static bool TryParseMonth(string? month, out int period)
    {
        period = 0;
        if (string.IsNullOrWhiteSpace(month)) return false;

        if (!DateTime.TryParseExact(
                month.Trim(),
                "yyyy-MM",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out var dt))
            return false;

        period = dt.Year * 100 + dt.Month;
        return true;
    }

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

    private static BillStatus FromUiStatus(string ui) => NormalizeUiStatus(ui) switch
    {
        "Paid" => BillStatus.Paid,
        "Overdue" => BillStatus.Overdue,
        _ => BillStatus.Issued
    };
}