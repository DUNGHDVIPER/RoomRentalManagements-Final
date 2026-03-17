using BLL.Common;
using BLL.DTOs.Common;
using BLL.DTOs.Contract;
using BLL.Services.Interfaces;
using DAL.Data;
using DAL.Entities.Contracts;
using DAL.Entities.Tenanting;
using DAL.Entities.System;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text.Json;

namespace BLL.Services;

public class ContractService : IContractService
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;

    public ContractService(AppDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    private const string OneActivePerRoomIndexName = "UX_Contracts_Room_ActiveOnly";

    private static bool IsOneActivePerRoomViolation(DbUpdateException ex)
    {
        var msg = ex.InnerException?.Message ?? ex.Message;
        return msg.Contains(OneActivePerRoomIndexName, StringComparison.OrdinalIgnoreCase);
    }


    // =========================
    // Centralized status constants + compare helper
    // =========================
    private static class ContractStatus
    {
        public const string Pending = "Pending";
        public const string Active = "Active";
        public const string Expired = "Expired";
        public const string Terminated = "Terminated";
        public const string Renewed = "Renewed";

        public static bool Is(string? s, string expected)
            => string.Equals(s?.Trim(), expected, StringComparison.OrdinalIgnoreCase);
    }

    // ============================================================
    // VALIDATION HELPERS (shared for MVC + Razor Pages)
    // ============================================================

    // Validate value >= 0
    private static void EnsureNonNegative(decimal value, string fieldName)
    {
        if (value < 0)
            throw new ValidationException($"{fieldName} must be >= 0.");
    }

    // Validate StartDate < EndDate (date part)
    private static void EnsureDateRange(DateTime start, DateTime end, string startName, string endName)
    {
        if (start.Date >= end.Date)
            throw new ValidationException($"{startName} must be earlier than {endName}.");
    }

    // Validate Room exists

    private async Task EnsureRoomExistsAsync(int roomId, CancellationToken ct)
    {

        var exists = await _db.Rooms
            .AsNoTracking()
            .AnyAsync(r => r.Id == roomId, ct); // ✅ Room PK là Id (dbo.Rooms.Id)

        if (!exists)
            throw new InvalidOperationException("Room not found.");
        var conn = _db.Database.GetDbConnection().ConnectionString;
        Console.WriteLine("DB = " + conn);

    }

    // Validate Tenant exists
    private async Task EnsureTenantExistsAsync(int tenantId, CancellationToken ct)
    {
        var exists = await _db.Tenants
            .AsNoTracking()
            .AnyAsync(t => t.Id == tenantId, ct); // ✅ dbo.Tenants.Id

        if (!exists)
            throw new InvalidOperationException("Tenant not found.");
    }

    // Ensure contract is ACTIVE
    private static void EnsureActive(Contract c, string actionName)
    {
        if (!ContractStatus.Is(c.Status, ContractStatus.Active))
            throw new InvalidOperationException(
                $"Only ACTIVE contracts can be {actionName}. Current status = '{c.Status}'.");
    }

    // (Optional) Ensure contract is renewable (Active/Expired/Terminated)
    // Nếu bạn muốn renew cả Expired/Terminated -> dùng hàm này thay EnsureActive ở RenewAsync.
    private static void EnsureRenewable(Contract c)
    {
        if (ContractStatus.Is(c.Status, ContractStatus.Pending))
            throw new InvalidOperationException("Cannot renew a PENDING contract. Please activate it first.");

        if (ContractStatus.Is(c.Status, ContractStatus.Renewed))
            throw new InvalidOperationException("Cannot renew because this contract has already been renewed.");

        if (!(ContractStatus.Is(c.Status, ContractStatus.Active)
              || ContractStatus.Is(c.Status, ContractStatus.Expired)
              || ContractStatus.Is(c.Status, ContractStatus.Terminated)))
        {
            throw new InvalidOperationException($"Cannot renew contract. Current status = '{c.Status}'.");
        }
    }

    // Ensure not in blocked statuses
    private static void EnsureNotInStatuses(string? status, params string[] blocked)
    {
        foreach (var s in blocked)
        {
            if (ContractStatus.Is(status, s))
                throw new InvalidOperationException(
                    $"Operation not allowed because contract status is '{status}'.");
        }
    }

    // ============================================================
    // 1) CREATE
    // Requirements:
    // - RoomId exists
    // - TenantId exists
    // - StartDate < EndDate
    // - Rent >= 0
    // - Deposit >= 0
    // - If ActivateNow=true: room must not already have ACTIVE contract
    // ============================================================
    public async Task<ContractDto> CreateAsync(CreateContractDto dto, int? actorUserId = null, CancellationToken ct = default)
    {
        if (dto is null) throw new ArgumentNullException(nameof(dto));

        // Input validation
        EnsureDateRange(dto.StartDate, dto.EndDate, "StartDate", "EndDate");
        EnsureNonNegative(dto.Rent, nameof(dto.Rent));
        EnsureNonNegative(dto.Deposit, nameof(dto.Deposit));

#if DEBUG
        System.Diagnostics.Debug.WriteLine(
            $"[CreateAsync] Provider={_db.Database.ProviderName}, RoomId={dto.RoomId}, TenantId={dto.TenantId}, ActivateNow={dto.ActivateNow}");
#endif
        // Existence validation
        await EnsureRoomExistsAsync(dto.RoomId, ct);
        await EnsureTenantExistsAsync(dto.TenantId, ct);

        // Business rule: ActivateNow => must not have another ACTIVE contract in same room
        if (dto.ActivateNow)
        {
            var activeUpper = ContractStatus.Active.ToUpper();
            var hasActive = await _db.Contracts.AsNoTracking()
                .AnyAsync(c => c.RoomId == dto.RoomId
                           && c.Status != null
                           && c.Status.ToUpper() == activeUpper, ct);
            if (hasActive)
                throw new InvalidOperationException(
                    $"Cannot activate contract: Room {dto.RoomId} already has an ACTIVE contract. " +
                    $"Please renew/terminate the current active contract first.");
        }

        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        try
        {
            var status = dto.ActivateNow ? ContractStatus.Active : ContractStatus.Pending;
            var code = await GenerateContractCodeAsync(ct);

            var contract = new Contract
            {
                RoomId = dto.RoomId,
                TenantId = dto.TenantId,
                ContractCode = code,

                StartDate = dto.StartDate.Date,
                EndDate = dto.EndDate.Date,

                BaseRent = dto.Rent,
                DepositAmount = dto.Deposit,

                PaymentCycle = "Monthly",
                Status = status,

                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = actorUserId
            };

            _db.Contracts.Add(contract);

            // Create initial deposit history row if Deposit > 0
            if (dto.Deposit > 0)
            {
                _db.Deposits.Add(new Deposit
                {
                    Contract = contract,
                    Amount = dto.Deposit,
                    Type = "Hold",
                    Note = "Initial deposit",
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync(ct);

            // Version snapshot
            await CreateVersionSnapshotInternalAsync(contract.ContractId, actorUserId, "Create contract", ct);

            // Audit
            await _audit.LogAsync(
                actorUserId,
                action: "CreateContract",
                entityType: "Contract",
                entityId: contract.ContractId.ToString(),
                note: "Create contract",
                oldValue: null,
                newValue: new
                {
                    contract.ContractId,
                    contract.ContractCode,
                    contract.RoomId,
                    contract.TenantId,
                    contract.StartDate,
                    contract.EndDate,
                    contract.BaseRent,
                    contract.DepositAmount,
                    contract.PaymentCycle,
                    contract.Status
                },
                ct: ct);

            await tx.CommitAsync(ct);
            return MapToDto(contract);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation() && IsOneActivePerRoomViolation(ex))
        {
            await tx.RollbackAsync(ct);
            throw new InvalidOperationException(
                "Cannot activate contract because this room already has an ACTIVE contract (DB constraint). " +
                "Please renew/terminate the existing active contract and try again.");
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
    // ============================================================
    // 1.2) ACTIVATE
    // Requirements:
    // - Contract exists
    // - Only PENDING contracts can be activated
    // - Must not violate one-active-contract-per-room
    // ============================================================
    public async Task ActivateAsync(int contractId, int? actorUserId = null, CancellationToken ct = default)
    {
        if (contractId <= 0) throw new InvalidOperationException("Invalid contract id.");

        var id = (long)contractId;

        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        try
        {
            var c = await _db.Contracts
                .FirstOrDefaultAsync(x => x.ContractId == id, ct);

            if (c == null)
                throw new InvalidOperationException("Contract not found.");

            // Idempotent behavior: if already ACTIVE, do nothing.
            if (ContractStatus.Is(c.Status, ContractStatus.Active))
            {
                await tx.CommitAsync(ct);
                return;
            }

            if (!ContractStatus.Is(c.Status, ContractStatus.Pending))
                throw new InvalidOperationException($"Only PENDING contracts can be activated. Current status = '{c.Status}'.");

            // Must not exist another ACTIVE contract in same room
            var activeUpper = ContractStatus.Active.ToUpper();

            var hasOtherActive = await _db.Contracts.AsNoTracking()
                .AnyAsync(x => x.RoomId == c.RoomId
                              && x.ContractId != c.ContractId
                              && x.Status != null
                              && x.Status.ToUpper() == activeUpper, ct);

            if (hasOtherActive)
                throw new InvalidOperationException("Cannot activate: this room already has an ACTIVE contract.");

            await CreateVersionSnapshotInternalAsync(c.ContractId, actorUserId, "Before activate", ct);

            var oldAudit = new { c.Status, c.Note };

            c.Status = ContractStatus.Active;
            c.Note = AppendNote(c.Note, $"Activated at {DateTime.UtcNow:yyyy-MM-dd HH:mm} (UTC).");

            await _db.SaveChangesAsync(ct);

            await CreateVersionSnapshotInternalAsync(c.ContractId, actorUserId, "After activate", ct);

            await _audit.LogAsync(
                actorUserId,
                action: "ActivateContract",
                entityType: "Contract",
                entityId: c.ContractId.ToString(),
                note: "Activate contract",
                oldValue: oldAudit,
                newValue: new { c.Status, c.Note },
                ct: ct);

            await tx.CommitAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation() && IsOneActivePerRoomViolation(ex))
        {
            await tx.RollbackAsync(ct);
            throw new InvalidOperationException(
                "Cannot activate contract because this room already has an ACTIVE contract (DB constraint). " +
                "Please renew/terminate the existing active contract and try again.");
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    // ============================================================
    // 2) RENEW
    // Requirements:
    // - Contract exists
    // - Contract must be ACTIVE (the requirement asked)
    // - NewStartDate < NewEndDate
    // - NewRent >= 0
    // - NewDeposit >= 0
    // - Must not violate one-active-contract-per-room
    // ============================================================
    public async Task<ContractDto> RenewAsync(RenewContractDto dto, int? actorUserId = null, CancellationToken ct = default)
    {
        if (dto is null) throw new ArgumentNullException(nameof(dto));

        // Input validation
        EnsureDateRange(dto.NewStartDate, dto.NewEndDate, "NewStartDate", "NewEndDate");
        EnsureNonNegative(dto.NewRent, nameof(dto.NewRent));
        EnsureNonNegative(dto.NewDeposit, nameof(dto.NewDeposit));

        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        try
        {
            var old = await _db.Contracts
                .FirstOrDefaultAsync(c => c.ContractId == (long)dto.ContractId, ct);

            if (old == null)
                throw new InvalidOperationException("Contract not found.");

            // Requirement says: contract must be ACTIVE
            EnsureActive(old, "renew");

            // If you want allow renew from Expired/Terminated too:
            // EnsureRenewable(old);

            // Rule: new start must be >= old.EndDate or old.EndDate+1 based on flag
            var minStart = dto.RequireStartNextDay ? old.EndDate.Date.AddDays(1) : old.EndDate.Date;
            if (dto.NewStartDate.Date < minStart)
                throw new ValidationException($"NewStartDate must be >= {minStart:yyyy-MM-dd}.");

            // Must not exist another ACTIVE contract in same room
            var activeUpper = ContractStatus.Active.ToUpper();

            var hasOtherActive = await _db.Contracts.AsNoTracking()
                .AnyAsync(c => c.RoomId == old.RoomId
                           && c.ContractId != old.ContractId
                           && c.Status != null
                           && c.Status.ToUpper() == activeUpper, ct);

            if (hasOtherActive)
                throw new InvalidOperationException("Renew failed: Room already has another ACTIVE contract.");

            // Snapshot before
            await CreateVersionSnapshotInternalAsync(old.ContractId, actorUserId, "Before renew (old)", ct);

            var oldAudit = new
            {
                old.ContractId,
                old.ContractCode,
                old.Status,
                old.StartDate,
                old.EndDate,
                old.BaseRent,
                old.DepositAmount,
                old.Note
            };

            // Old -> Renewed
            old.Status = ContractStatus.Renewed;
            old.Note = AppendNote(old.Note,
                $"Renewed -> new period {dto.NewStartDate:yyyy-MM-dd} to {dto.NewEndDate:yyyy-MM-dd}. Reason: {dto.Reason}");

            // New contract -> Active
            var newCode = await GenerateContractCodeAsync(ct);

            var renewed = new Contract
            {
                RoomId = old.RoomId,
                TenantId = old.TenantId,
                ContractCode = newCode,

                StartDate = dto.NewStartDate.Date,
                EndDate = dto.NewEndDate.Date,

                BaseRent = dto.NewRent,
                DepositAmount = dto.NewDeposit,

                PaymentCycle = old.PaymentCycle,
                Status = ContractStatus.Active,

                Note = $"Renew from {old.ContractCode}. Reason: {dto.Reason}",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = actorUserId
            };

            _db.Contracts.Add(renewed);

            await _db.SaveChangesAsync(ct);

            // Snapshot after
            await CreateVersionSnapshotInternalAsync(old.ContractId, actorUserId, "After renew (old renewed)", ct);
            await CreateVersionSnapshotInternalAsync(renewed.ContractId, actorUserId, "After renew (new created)", ct);

            // Audit
            await _audit.LogAsync(
                actorUserId,
                action: "RenewContract",
                entityType: "Contract",
                entityId: old.ContractId.ToString(),
                note: $"NewContractId={renewed.ContractId}",
                oldValue: oldAudit,
                newValue: new
                {
                    renewed.ContractId,
                    renewed.ContractCode,
                    renewed.Status,
                    renewed.StartDate,
                    renewed.EndDate,
                    renewed.BaseRent,
                    renewed.DepositAmount
                },
                ct: ct);

            await tx.CommitAsync(ct);
            return MapToDto(renewed);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation() && IsOneActivePerRoomViolation(ex))
        {
            await tx.RollbackAsync(ct);
            throw new InvalidOperationException(
                "Renew failed because the room already has another ACTIVE contract (DB constraint).");
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    // ============================================================
    // 3) TERMINATE
    // Requirements:
    // - Contract exists
    // - Contract not Terminated/Expired/Renewed
    // - TerminateDate within [StartDate, EndDate]
    // ============================================================
    public async Task TerminateAsync(TerminateContractDto dto, int? actorUserId = null, CancellationToken ct = default)
    {
        if (dto is null) throw new ArgumentNullException(nameof(dto));

        var terminateDate = dto.TerminateDate.Date;

        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        try
        {
            var c = await _db.Contracts
                .FirstOrDefaultAsync(x => x.ContractId == (long)dto.ContractId, ct);

            if (c == null)
                throw new InvalidOperationException("Contract not found.");

            // Block invalid statuses
            EnsureNotInStatuses(c.Status, ContractStatus.Terminated, ContractStatus.Expired, ContractStatus.Renewed);

            // Validate terminate date range
            if (terminateDate < c.StartDate.Date || terminateDate > c.EndDate.Date)
                throw new ValidationException("TerminateDate must be within [StartDate, EndDate].");

            await CreateVersionSnapshotInternalAsync(c.ContractId, actorUserId, "Before terminate", ct);

            var oldAudit = new { c.Status, c.EndDate, c.Note };
            var endDateBefore = c.EndDate.Date;

            c.Status = ContractStatus.Terminated;
            c.EndDate = terminateDate;
            c.Note = AppendNote(c.Note, $"Terminated at {terminateDate:yyyy-MM-dd}. Reason: {dto.Reason}");

            // Checkout residents
            var residents = await _db.RoomResidents
                .Where(r => r.RoomId == c.RoomId && r.TenantId == c.TenantId && r.IsActive)
                .ToListAsync(ct);

            foreach (var r in residents)
            {
                r.CheckOutDate = terminateDate;
                r.IsActive = false;
            }

            await _db.SaveChangesAsync(ct);

            await CreateVersionSnapshotInternalAsync(c.ContractId, actorUserId, "After terminate", ct);

            await _audit.LogAsync(
                actorUserId,
                action: "TerminateContract",
                entityType: "Contract",
                entityId: c.ContractId.ToString(),
                note: dto.Reason,
                oldValue: new { oldAudit.Status, EndDate = endDateBefore, oldAudit.Note },
                newValue: new { c.Status, c.EndDate, c.Note, TerminatedAt = terminateDate },
                ct: ct);

            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
    public async Task ForceExpireAsync(int contractId, int? actorUserId = null, CancellationToken ct = default)
    {
        if (contractId <= 0)
            throw new InvalidOperationException("Invalid contract id.");

        var id = (long)contractId;
        var today = DateTime.Today;

        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        try
        {
            var c = await _db.Contracts
                .FirstOrDefaultAsync(x => x.ContractId == id, ct);

            if (c == null)
                throw new InvalidOperationException("Contract not found.");

            // Idempotent
            if (ContractStatus.Is(c.Status, ContractStatus.Expired))
            {
                await tx.CommitAsync(ct);
                return;
            }

            if (!ContractStatus.Is(c.Status, ContractStatus.Active))
                throw new InvalidOperationException($"Only ACTIVE contracts can be force-expired. Current status = '{c.Status}'.");

            if (c.EndDate.Date >= today)
                throw new InvalidOperationException("Cannot force-expire: EndDate has not passed yet.");

            var oldAudit = new { c.Status, c.EndDate, c.Note };

            c.Status = ContractStatus.Expired;
            c.Note = AppendNote(c.Note, $"Force expired by admin at {DateTime.UtcNow:yyyy-MM-dd HH:mm} (UTC).");

            await _db.SaveChangesAsync(ct);

            await CreateVersionSnapshotInternalAsync(c.ContractId, actorUserId, "Force expire contract", ct);

            await _audit.LogAsync(
                actorUserId,
                action: "ForceExpireContract",
                entityType: "Contract",
                entityId: c.ContractId.ToString(),
                note: "Admin force expire",
                oldValue: oldAudit,
                newValue: new { c.Status, c.EndDate, c.Note },
                ct: ct);

            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    // ============================================================
    // 4) GET LIST + GET BY ID
    // ============================================================
    public async Task<PagedResultDto<ContractDto>> GetContractsAsync(PagedRequestDto req, CancellationToken ct = default)
    {
        var page = req.PageNumber < 1 ? 1 : req.PageNumber;
        var pageSize = req.PageSize < 1 ? 12 : req.PageSize;

        var q = _db.Contracts.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(req.Keyword))
        {
            var kw = req.Keyword.Trim();
            q = q.Where(x =>
                (x.ContractCode ?? "").Contains(kw) ||
                x.RoomId.ToString().Contains(kw) ||
                x.TenantId.ToString().Contains(kw));
        }

        var total = await q.CountAsync(ct);

        var items = await q
            .OrderByDescending(c => c.ContractId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new ContractDto
            {
                Id = (int)c.ContractId,
                RoomId = c.RoomId,
                TenantId = c.TenantId,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                Rent = c.BaseRent,
                Deposit = c.DepositAmount,
                Status = StatusToInt(c.Status),
                IsActive = c.Status == ContractStatus.Active,
                ContractCode = c.ContractCode
            })
            .ToListAsync(ct);

        return new PagedResultDto<ContractDto>(items, total, page, pageSize);
    }

    /*    public async Task<ContractDto> GetByIdAsync(int id, CancellationToken ct = default)
        {

        }*/
    public async Task<ContractDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var contractId = (long)id;

        var c = await _db.Contracts.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ContractId == contractId, ct);

        if (c == null) throw new InvalidOperationException("Contract not found.");
        return MapToDto(c);
    }
    public Task<ContractDto> GetByIdAsync(long contractId, CancellationToken ct = default)
    {
        if (contractId <= 0 || contractId > int.MaxValue)
            throw new InvalidOperationException("Invalid contract id.");

        return GetByIdAsync((int)contractId, ct);
    }
    // ============================================================
    // 5) UPDATE DEPOSIT
    // Requirement: newDeposit >= 0
    // ============================================================
    public async Task UpdateDepositAsync(int contractId, decimal newDeposit, int? actorUserId = null, CancellationToken ct = default)
    {
        EnsureNonNegative(newDeposit, nameof(newDeposit));

        var id = (long)contractId;

        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        try
        {
            var contract = await _db.Contracts.FirstOrDefaultAsync(c => c.ContractId == id, ct);
            if (contract == null) throw new InvalidOperationException("Contract not found.");

            await CreateVersionSnapshotInternalAsync(contract.ContractId, actorUserId, "Before deposit update", ct);

            var oldDeposit = contract.DepositAmount;
            contract.DepositAmount = newDeposit;

            if (newDeposit != oldDeposit)
            {
                _db.Deposits.Add(new Deposit
                {
                    ContractId = contract.ContractId,
                    Amount = newDeposit - oldDeposit,
                    Type = "Hold",
                    Note = $"Deposit changed {oldDeposit} -> {newDeposit}",
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync(ct);

            await CreateVersionSnapshotInternalAsync(contract.ContractId, actorUserId, "After deposit update", ct);

            await _audit.LogAsync(
                actorUserId,
                action: "UpdateDeposit",
                entityType: "Contract",
                entityId: contract.ContractId.ToString(),
                note: "Update deposit amount",
                oldValue: new { DepositAmount = oldDeposit },
                newValue: new { DepositAmount = contract.DepositAmount },
                ct: ct);

            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task UpdateDepositAsync(UpdateDepositDto dto, int? actorUserId = null, CancellationToken ct = default)
    {
        if (dto is null) throw new ArgumentNullException(nameof(dto));

        // Minimal requirement you asked: deposit >= 0
        EnsureNonNegative(dto.DepositAmount, nameof(dto.DepositAmount));

        dto.DepositStatus = (dto.DepositStatus ?? "Unpaid").Trim();

        static bool IsOneOf(string s, params string[] vals)
            => vals.Any(v => string.Equals(s, v, StringComparison.OrdinalIgnoreCase));

        if (!IsOneOf(dto.DepositStatus, "Unpaid", "Paid", "Refunded", "Forfeit"))
            throw new InvalidOperationException("DepositStatus invalid. Use Unpaid/Paid/Refunded/Forfeit.");

        if (string.Equals(dto.DepositStatus, "Paid", StringComparison.OrdinalIgnoreCase))
        {
            if (dto.PaidAt == null) throw new InvalidOperationException("PaidAt is required when DepositStatus=Paid.");
            if ((dto.PaidAmount ?? 0) <= 0) throw new InvalidOperationException("PaidAmount must be > 0 when DepositStatus=Paid.");
        }

        if (string.Equals(dto.DepositStatus, "Unpaid", StringComparison.OrdinalIgnoreCase))
        {
            if (dto.PaidAt != null) throw new InvalidOperationException("PaidAt must be null when DepositStatus=Unpaid.");
        }

        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        try
        {
            var c = await _db.Contracts.FirstOrDefaultAsync(x => x.ContractId == (long)dto.ContractId, ct);
            if (c == null) throw new InvalidOperationException("Contract not found.");

            await CreateVersionSnapshotInternalAsync(c.ContractId, actorUserId, "Before deposit update", ct);

            var oldAudit = new
            {
                c.DepositAmount,
                c.DepositStatus,
                c.DepositPaidAt,
                c.DepositPaidAmount
            };

            var oldDepositAmount = c.DepositAmount;
            var oldStatus = c.DepositStatus;
            var oldPaidAmount = c.DepositPaidAmount;

            c.DepositAmount = dto.DepositAmount;
            c.DepositStatus = dto.DepositStatus;
            c.DepositPaidAt = dto.PaidAt?.ToUniversalTime();
            c.DepositPaidAmount = dto.PaidAmount;

            if (string.Equals(c.DepositStatus, "Unpaid", StringComparison.OrdinalIgnoreCase))
            {
                c.DepositPaidAt = null;
                c.DepositPaidAmount = null;
            }

            var delta = c.DepositAmount - oldDepositAmount;
            if (delta != 0)
            {
                _db.Deposits.Add(new Deposit
                {
                    ContractId = c.ContractId,
                    Amount = delta,
                    Type = "Hold",
                    Note = $"DepositAmount changed {oldDepositAmount} -> {c.DepositAmount}. {dto.Note}",
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (!string.Equals(oldStatus, c.DepositStatus, StringComparison.OrdinalIgnoreCase))
            {
                var t = c.DepositStatus switch
                {
                    "Paid" => "Paid",
                    "Refunded" => "Refund",
                    "Forfeit" => "Forfeit",
                    _ => "Hold"
                };

                var amount = (c.DepositPaidAmount ?? 0);
                if (string.Equals(c.DepositStatus, "Paid", StringComparison.OrdinalIgnoreCase) && amount <= 0)
                    amount = c.DepositAmount;

                _db.Deposits.Add(new Deposit
                {
                    ContractId = c.ContractId,
                    Amount = amount,
                    Type = t,
                    Note = $"DepositStatus {oldStatus} -> {c.DepositStatus}. {dto.Note}",
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                if (string.Equals(c.DepositStatus, "Paid", StringComparison.OrdinalIgnoreCase)
                    && (oldPaidAmount ?? 0) != (c.DepositPaidAmount ?? 0))
                {
                    _db.Deposits.Add(new Deposit
                    {
                        ContractId = c.ContractId,
                        Amount = (c.DepositPaidAmount ?? 0) - (oldPaidAmount ?? 0),
                        Type = "Paid",
                        Note = $"PaidAmount changed {(oldPaidAmount ?? 0)} -> {(c.DepositPaidAmount ?? 0)}. {dto.Note}",
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            await _db.SaveChangesAsync(ct);

            await CreateVersionSnapshotInternalAsync(c.ContractId, actorUserId, "After deposit update", ct);

            await _audit.LogAsync(
                actorUserId,
                action: "UpdateDeposit",
                entityType: "Contract",
                entityId: c.ContractId.ToString(),
                note: dto.Note,
                oldValue: oldAudit,
                newValue: new
                {
                    c.DepositAmount,
                    c.DepositStatus,
                    c.DepositPaidAt,
                    c.DepositPaidAmount
                },
                ct: ct);

            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    // ============================================================
    // 6) ATTACHMENT UPLOAD (metadata)
    // ============================================================
    public async Task AddAttachmentStubAsync(int contractId, string fileName, string url, int? actorUserId = null, CancellationToken ct = default)
    {
        var id = (int)contractId;

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        try
        {
            var contractExists = await _db.Contracts.AsNoTracking().AnyAsync(c => c.ContractId == id, ct);
            if (!contractExists) throw new InvalidOperationException("Contract not found.");

            _db.ContractAttachments.Add(new ContractAttachment
            {
                ContractId = id,
                FileName = fileName,
                FileUrl = url,
                UploadedAt = DateTime.UtcNow,
                UploadedByUserId = actorUserId
            });

            await _db.SaveChangesAsync(ct);

            await CreateVersionSnapshotInternalAsync(id, actorUserId, $"Add attachment: {fileName}", ct);

            await _audit.LogAsync(
                actorUserId,
                action: "UploadAttachment",
                entityType: "Contract",
                entityId: id.ToString(),
                note: fileName,
                oldValue: null,
                newValue: new { FileName = fileName, Url = url },
                ct: ct);

            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    // ============================================================
    // 7) VERSIONING
    // ============================================================
    public Task CreateVersionSnapshotAsync(int contractId, string changedByUserId, CancellationToken ct = default)
        => CreateVersionSnapshotInternalAsync((int)contractId, TryParseUserId(changedByUserId), "Manual snapshot", ct);

    public async Task<List<ContractVersionItemDto>> GetVersionsAsync(int contractId, CancellationToken ct = default)
    {
        var id = (long)contractId;

        var exists = await _db.Contracts.AsNoTracking().AnyAsync(x => x.ContractId == id, ct);
        if (!exists) throw new InvalidOperationException("Contract not found.");

        return await _db.ContractVersions.AsNoTracking()
            .Where(v => v.ContractId == id)
            .OrderByDescending(v => v.VersionNumber)
            .Select(v => new ContractVersionItemDto
            {
                VersionId = v.VersionId,
                VersionNumber = v.VersionNumber,
                ChangedAt = v.ChangedAt,
                ChangedByUserId = v.ChangedByUserId,
                ChangeNote = v.ChangeNote,
                SnapshotJson = v.SnapshotJson
            })
            .ToListAsync(ct);
    }

    private async Task CreateVersionSnapshotInternalAsync(int contractId, int? changedByUserId, string? changeNote, CancellationToken ct)
    {
        var contract = await _db.Contracts.AsNoTracking()
            .Include(c => c.Attachments)
            .Include(c => c.Deposits)
            .FirstOrDefaultAsync(c => c.ContractId == contractId, ct);

        if (contract == null) return;

        var currentMaxVersion = await _db.ContractVersions
            .Where(v => v.ContractId == contractId)
            .MaxAsync(v => (int?)v.VersionNumber, ct) ?? 0;

        var snapshotObj = new
        {
            contract.ContractId,
            contract.ContractCode,
            contract.RoomId,
            contract.TenantId,
            contract.StartDate,
            contract.EndDate,
            contract.BaseRent,
            contract.DepositAmount,
            contract.DepositStatus,
            contract.DepositPaidAt,
            contract.DepositPaidAmount,
            contract.PaymentCycle,
            contract.Status,
            contract.Note,
            Attachments = contract.Attachments.Select(a => new
            {
                a.AttachmentId,
                a.FileName,
                a.FileUrl,
                a.UploadedAt,
                a.UploadedByUserId
            }),
            Deposits = contract.Deposits.Select(d => new
            {
                d.DepositId,
                d.Amount,
                d.Type,
                d.Note,
                d.CreatedAt
            })
        };

        var json = JsonSerializer.Serialize(snapshotObj, new JsonSerializerOptions { WriteIndented = true });

        _db.ContractVersions.Add(new ContractVersion
        {
            ContractId = contractId,
            VersionNumber = currentMaxVersion + 1,
            ChangedAt = DateTime.UtcNow,
            ChangedByUserId = changedByUserId,
            ChangeNote = changeNote,
            SnapshotJson = json
        });

        await _db.SaveChangesAsync(ct);
    }
    // ============================================================
    // 8) REMINDERS
    // ============================================================
    public async Task CreateReminderAsync(int contractId, DateTime remindAt, string type, CancellationToken ct = default)
    {
        var id = (long)contractId;

        var contract = await _db.Contracts.AsNoTracking()
            .FirstOrDefaultAsync(c => c.ContractId == id, ct);

        if (contract == null)
            throw new InvalidOperationException("Contract not found.");

        var notif = new DAL.Entities.System.Notification
        {
            Title = $"Contract reminder ({type})",
            Content = $"Contract {contract.ContractCode} reminder at {remindAt:yyyy-MM-dd HH:mm}.",
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            ContractId = contract.ContractId
        };

        _db.Notifications.Add(notif);
        await _db.SaveChangesAsync(ct);
    }

    // ============================================================
    // 9) EXPORT PDF
    // ============================================================
    public async Task<byte[]> ExportPdfStubAsync(int contractId, int? actorUserId = null, CancellationToken ct = default)
    {
        var id = (long)contractId;

        var c = await _db.Contracts.AsNoTracking()
            .Include(x => x.Room)
            .Include(x => x.Tenant)
            .FirstOrDefaultAsync(x => x.ContractId == id, ct);

        if (c == null) throw new InvalidOperationException("Contract not found.");

        var bytes = BLL.Pdf.ContractPdfExporter.Export(c);

        await _audit.LogAsync(
            actorUserId,
            action: "ExportContractPdf",
            entityType: "Contract",
            entityId: c.ContractId.ToString(),
            note: c.ContractCode,
            oldValue: null,
            newValue: new { c.ContractId, c.ContractCode, SizeBytes = bytes.Length },
            ct: ct);

        return bytes;
    }

    // ============================================================
    // 10) EXPIRY JOB (demo)
    // ============================================================
    public async Task<int> ScanAndSendExpiryRemindersAsync(
int daysBeforeEnd = 7,
string remindType = "Expiry_7d",
int? actorUserId = null,
CancellationToken ct = default)
    {
        var today = DateTime.Today;
        var target = today.AddDays(daysBeforeEnd);

        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        try
        {
            var contracts = await _db.Contracts.AsNoTracking()
                .Where(c => c.Status == ContractStatus.Active && c.EndDate.Date == target.Date)
                .Select(c => new { c.ContractId, c.ContractCode, c.TenantId })
                .ToListAsync(ct);

            var sent = 0;

            foreach (var c in contracts)
            {
                var already = await _db.ContractReminderLogs.AsNoTracking()
                    .AnyAsync(x =>
                        x.ContractId == c.ContractId &&
                        x.RemindType == remindType &&
                        x.RemindAtDate == today, ct);

                if (already) continue;

                var title = $"Contract Expiry Reminder ({daysBeforeEnd} days)";
                var content = $"Contract {c.ContractCode} will expire on {target:yyyy-MM-dd}. Please review / renew / terminate.";
                var createdAt = DateTime.UtcNow;

                var pTitle = new Microsoft.Data.SqlClient.SqlParameter("@title", title);
                var pContent = new Microsoft.Data.SqlClient.SqlParameter("@content", content);
                var pCreatedAt = new Microsoft.Data.SqlClient.SqlParameter("@createdAt", createdAt);

                var notifId = await _db.Database.SqlQueryRaw<long>(@"
INSERT INTO [Notifications] ([UserId],[Title],[Content],[IsRead],[ReadAt],[CreatedAt])
OUTPUT INSERTED.[Id]
VALUES (NULL, @title, @content, 0, NULL, @createdAt);
", pTitle, pContent, pCreatedAt).SingleAsync(ct);

                var pNotifId = new Microsoft.Data.SqlClient.SqlParameter("@nid", notifId);
                var pTenantId = new Microsoft.Data.SqlClient.SqlParameter("@tid", c.TenantId);

                await _db.Database.ExecuteSqlRawAsync(@"
INSERT INTO [NotificationRecipients] ([NotificationId],[TenantId],[IsRead],[ReadAt])
VALUES (@nid, @tid, 0, NULL);
", pNotifId, pTenantId);

                _db.ContractReminderLogs.Add(new ContractReminderLog
                {
                    ContractId = c.ContractId,
                    RemindType = remindType,
                    RemindAtDate = today,
                    CreatedAt = DateTime.UtcNow
                });

                sent++;
            }

            if (sent > 0)
                await _db.SaveChangesAsync(ct);

            await _audit.LogAsync(
                actorUserId,
                action: "RunExpiryReminderJob",
                entityType: "Contract",
                entityId: "0",
                note: "Scan expiry reminders",
                oldValue: null,
                newValue: new
                {
                    daysBeforeEnd,
                    remindType,
                    targetEndDate = target.ToString("yyyy-MM-dd"),
                    sent
                },
                ct: ct);

            await tx.CommitAsync(ct);
            return sent;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    // ============================================================
    // Mapping helpers
    // ============================================================
    private static ContractDto MapToDto(Contract c) => new()
    {
        Id = (int)c.ContractId,
        RoomId = c.RoomId,
        TenantId = c.TenantId,
        StartDate = c.StartDate,
        EndDate = c.EndDate,
        Rent = c.BaseRent,
        Deposit = c.DepositAmount,
        Status = StatusToInt(c.Status),
        IsActive = c.Status == ContractStatus.Active,

        ContractCode = c.ContractCode,
        DepositStatus = c.DepositStatus ?? "Unpaid",
        DepositPaidAt = c.DepositPaidAt,
        DepositPaidAmount = c.DepositPaidAmount
    };

    private static int StatusToInt(string? status) => status switch
    {
        ContractStatus.Pending => 0,
        ContractStatus.Active => 1,
        ContractStatus.Expired => 2,
        ContractStatus.Terminated => 3,
        ContractStatus.Renewed => 4,
        _ => 99
    };

    private static string AppendNote(string? old, string add)
        => string.IsNullOrWhiteSpace(old) ? add : $"{old}\n- {add}";

    private async Task<string> GenerateContractCodeAsync(CancellationToken ct)
    {
        var prefix = $"CT-{DateTime.UtcNow:yyyyMM}-";
        var last = await _db.Contracts.AsNoTracking()
            .Where(c => c.ContractCode.StartsWith(prefix))
            .OrderByDescending(c => c.ContractCode)
            .Select(c => c.ContractCode)
            .FirstOrDefaultAsync(ct);

        var nextNum = 1;
        if (!string.IsNullOrWhiteSpace(last))
        {
            var tail = last.Replace(prefix, "");
            if (int.TryParse(tail, out var n)) nextNum = n + 1;
        }

        return $"{prefix}{nextNum:0000}";
    }

    private static int? TryParseUserId(string? s)
        => int.TryParse(s, out var id) ? id : null;

    public sealed class WhoRow
    {
        public string ServerName { get; set; } = "";
        public string DbName { get; set; } = "";
        public string LoginName { get; set; } = "";
    }

    public async Task<int?> GetActiveContractIdByUserIdAsync(string userId)
    {
        return await _db.Contracts
            .Where(c => c.Tenant.UserId == userId && c.Status == "Active")
            .Select(c => (int?)c.ContractId)
            .FirstOrDefaultAsync();
    }

}
