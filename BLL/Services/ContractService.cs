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

    private Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> action,
        CancellationToken ct = default,
        IsolationLevel isolationLevel = IsolationLevel.Serializable)
    {
        var strategy = _db.Database.CreateExecutionStrategy();

        return strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(isolationLevel, ct);
            try
            {
                await action(ct);
                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        });
    }

    private Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> action,
        CancellationToken ct = default,
        IsolationLevel isolationLevel = IsolationLevel.Serializable)
    {
        var strategy = _db.Database.CreateExecutionStrategy();

        return strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(isolationLevel, ct);
            try
            {
                var result = await action(ct);
                await tx.CommitAsync(ct);
                return result;
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        });
    }

    private const string OneActivePerRoomIndexName = "UX_Contracts_Room_ActiveOnly";

    private static bool IsOneActivePerRoomViolation(DbUpdateException ex)
    {
        var msg = ex.InnerException?.Message ?? ex.Message;
        return msg.Contains(OneActivePerRoomIndexName, StringComparison.OrdinalIgnoreCase);
    }

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

    private static void EnsureNonNegative(decimal value, string fieldName)
    {
        if (value < 0)
            throw new ValidationException($"{fieldName} must be >= 0.");
    }

    private static void EnsureDateRange(DateTime start, DateTime end, string startName, string endName)
    {
        if (start.Date >= end.Date)
            throw new ValidationException($"{startName} must be earlier than {endName}.");
    }

    private async Task EnsureRoomExistsAsync(int roomId, CancellationToken ct)
    {
        var exists = await _db.Rooms
            .AsNoTracking()
            .AnyAsync(r => r.RoomId == roomId, ct);

        if (!exists)
            throw new InvalidOperationException("Room not found.");
    }

    private async Task EnsureTenantExistsAsync(int tenantId, CancellationToken ct)
    {
        var exists = await _db.Tenants
            .AsNoTracking()
            .AnyAsync(t => t.Id == tenantId, ct);

        if (!exists)
            throw new InvalidOperationException("Tenant not found.");
    }

    private static void EnsureActive(Contract c, string actionName)
    {
        if (!ContractStatus.Is(c.Status, ContractStatus.Active))
            throw new InvalidOperationException(
                $"Only ACTIVE contracts can be {actionName}. Current status = '{c.Status}'.");
    }

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

    private static void EnsureNotInStatuses(string? status, params string[] blocked)
    {
        foreach (var s in blocked)
        {
            if (ContractStatus.Is(status, s))
                throw new InvalidOperationException(
                    $"Operation not allowed because contract status is '{status}'.");
        }
    }

    public async Task<ContractDto> CreateAsync(CreateContractDto dto, int? actorUserId = null, CancellationToken ct = default)
    {
        if (dto is null) throw new ArgumentNullException(nameof(dto));

        EnsureDateRange(dto.StartDate, dto.EndDate, "StartDate", "EndDate");
        EnsureNonNegative(dto.Rent, nameof(dto.Rent));
        EnsureNonNegative(dto.Deposit, nameof(dto.Deposit));

        await EnsureRoomExistsAsync(dto.RoomId, ct);
        await EnsureTenantExistsAsync(dto.TenantId, ct);

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

        try
        {
            return await ExecuteInTransactionAsync(async innerCt =>
            {
                var status = dto.ActivateNow ? ContractStatus.Active : ContractStatus.Pending;
                var code = await GenerateContractCodeAsync(innerCt);

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

                await _db.SaveChangesAsync(innerCt);

                await CreateVersionSnapshotInternalAsync(contract.ContractId, actorUserId, "Create contract", innerCt);

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
                    ct: innerCt);

                return MapToDto(contract);
            }, ct);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation() && IsOneActivePerRoomViolation(ex))
        {
            throw new InvalidOperationException(
                "Cannot activate contract because this room already has an ACTIVE contract (DB constraint). " +
                "Please renew/terminate the existing active contract and try again.");
        }
    }

    public async Task ActivateAsync(int contractId, int? actorUserId = null, CancellationToken ct = default)
    {
        if (contractId <= 0) throw new InvalidOperationException("Invalid contract id.");

        var id = (long)contractId;

        try
        {
            await ExecuteInTransactionAsync(async innerCt =>
            {
                var c = await _db.Contracts.FirstOrDefaultAsync(x => x.ContractId == id, innerCt);

                if (c == null)
                    throw new InvalidOperationException("Contract not found.");

                if (ContractStatus.Is(c.Status, ContractStatus.Active))
                    return;

                if (!ContractStatus.Is(c.Status, ContractStatus.Pending))
                    throw new InvalidOperationException($"Only PENDING contracts can be activated. Current status = '{c.Status}'.");

                var activeUpper = ContractStatus.Active.ToUpper();

                var hasOtherActive = await _db.Contracts.AsNoTracking()
                    .AnyAsync(x => x.RoomId == c.RoomId
                                  && x.ContractId != c.ContractId
                                  && x.Status != null
                                  && x.Status.ToUpper() == activeUpper, innerCt);

                if (hasOtherActive)
                    throw new InvalidOperationException("Cannot activate: this room already has an ACTIVE contract.");

                await CreateVersionSnapshotInternalAsync(c.ContractId, actorUserId, "Before activate", innerCt);

                var oldAudit = new { c.Status, c.Note };

                c.Status = ContractStatus.Active;
                c.Note = AppendNote(c.Note, $"Activated at {DateTime.UtcNow:yyyy-MM-dd HH:mm} (UTC).");

                await _db.SaveChangesAsync(innerCt);

                await CreateVersionSnapshotInternalAsync(c.ContractId, actorUserId, "After activate", innerCt);

                await _audit.LogAsync(
                    actorUserId,
                    action: "ActivateContract",
                    entityType: "Contract",
                    entityId: c.ContractId.ToString(),
                    note: "Activate contract",
                    oldValue: oldAudit,
                    newValue: new { c.Status, c.Note },
                    ct: innerCt);
            }, ct);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation() && IsOneActivePerRoomViolation(ex))
        {
            throw new InvalidOperationException(
                "Cannot activate contract because this room already has an ACTIVE contract (DB constraint). " +
                "Please renew/terminate the existing active contract and try again.");
        }
    }

    public async Task<ContractDto> RenewAsync(RenewContractDto dto, int? actorUserId = null, CancellationToken ct = default)
    {
        if (dto is null) throw new ArgumentNullException(nameof(dto));

        EnsureDateRange(dto.NewStartDate, dto.NewEndDate, "NewStartDate", "NewEndDate");
        EnsureNonNegative(dto.NewRent, nameof(dto.NewRent));
        EnsureNonNegative(dto.NewDeposit, nameof(dto.NewDeposit));

        try
        {
            return await ExecuteInTransactionAsync(async innerCt =>
            {
                var old = await _db.Contracts
                    .FirstOrDefaultAsync(c => c.ContractId == (long)dto.ContractId, innerCt);

                if (old == null)
                    throw new InvalidOperationException("Contract not found.");

                EnsureActive(old, "renew");

                var minStart = dto.RequireStartNextDay ? old.EndDate.Date.AddDays(1) : old.EndDate.Date;
                if (dto.NewStartDate.Date < minStart)
                    throw new ValidationException($"NewStartDate must be >= {minStart:yyyy-MM-dd}.");

                var activeUpper = ContractStatus.Active.ToUpper();

                var hasOtherActive = await _db.Contracts.AsNoTracking()
                    .AnyAsync(c => c.RoomId == old.RoomId
                               && c.ContractId != old.ContractId
                               && c.Status != null
                               && c.Status.ToUpper() == activeUpper, innerCt);

                if (hasOtherActive)
                    throw new InvalidOperationException("Renew failed: Room already has another ACTIVE contract.");

                await CreateVersionSnapshotInternalAsync(old.ContractId, actorUserId, "Before renew (old)", innerCt);

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

                old.Status = ContractStatus.Renewed;
                old.Note = AppendNote(old.Note,
                    $"Renewed -> new period {dto.NewStartDate:yyyy-MM-dd} to {dto.NewEndDate:yyyy-MM-dd}. Reason: {dto.Reason}");

                var newCode = await GenerateContractCodeAsync(innerCt);

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

                await _db.SaveChangesAsync(innerCt);

                await CreateVersionSnapshotInternalAsync(old.ContractId, actorUserId, "After renew (old renewed)", innerCt);
                await CreateVersionSnapshotInternalAsync(renewed.ContractId, actorUserId, "After renew (new created)", innerCt);

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
                    ct: innerCt);

                return MapToDto(renewed);
            }, ct);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation() && IsOneActivePerRoomViolation(ex))
        {
            throw new InvalidOperationException(
                "Renew failed because the room already has another ACTIVE contract (DB constraint).");
        }
    }

    public async Task TerminateAsync(TerminateContractDto dto, int? actorUserId = null, CancellationToken ct = default)
    {
        if (dto is null) throw new ArgumentNullException(nameof(dto));

        var terminateDate = dto.TerminateDate.Date;

        await ExecuteInTransactionAsync(async innerCt =>
        {
            var c = await _db.Contracts
                .FirstOrDefaultAsync(x => x.ContractId == (long)dto.ContractId, innerCt);

            if (c == null)
                throw new InvalidOperationException("Contract not found.");

            EnsureNotInStatuses(c.Status, ContractStatus.Terminated, ContractStatus.Expired, ContractStatus.Renewed);

            if (terminateDate < c.StartDate.Date || terminateDate > c.EndDate.Date)
                throw new ValidationException("TerminateDate must be within [StartDate, EndDate].");

            await CreateVersionSnapshotInternalAsync(c.ContractId, actorUserId, "Before terminate", innerCt);

            var oldAudit = new { c.Status, c.EndDate, c.Note };
            var endDateBefore = c.EndDate.Date;

            c.Status = ContractStatus.Terminated;
            c.EndDate = terminateDate;
            c.Note = AppendNote(c.Note, $"Terminated at {terminateDate:yyyy-MM-dd}. Reason: {dto.Reason}");

            var residents = await _db.RoomResidents
                .Where(r => r.RoomId == c.RoomId && r.TenantId == c.TenantId && r.IsActive)
                .ToListAsync(innerCt);

            foreach (var r in residents)
            {
                r.CheckOutDate = terminateDate;
                r.IsActive = false;
            }

            await _db.SaveChangesAsync(innerCt);

            await CreateVersionSnapshotInternalAsync(c.ContractId, actorUserId, "After terminate", innerCt);

            await _audit.LogAsync(
                actorUserId,
                action: "TerminateContract",
                entityType: "Contract",
                entityId: c.ContractId.ToString(),
                note: dto.Reason,
                oldValue: new { oldAudit.Status, EndDate = endDateBefore, oldAudit.Note },
                newValue: new { c.Status, c.EndDate, c.Note, TerminatedAt = terminateDate },
                ct: innerCt);
        }, ct);
    }
    public async Task ForceExpireAsync(int contractId, int? actorUserId = null, CancellationToken ct = default)
    {
        if (contractId <= 0)
            throw new InvalidOperationException("Invalid contract id.");

        var id = (long)contractId;
        var today = DateTime.Today;

        await ExecuteInTransactionAsync(async innerCt =>
        {
            var c = await _db.Contracts
                .FirstOrDefaultAsync(x => x.ContractId == id, innerCt);

            if (c == null)
                throw new InvalidOperationException("Contract not found.");

            if (ContractStatus.Is(c.Status, ContractStatus.Expired))
                return;

            if (!ContractStatus.Is(c.Status, ContractStatus.Active))
                throw new InvalidOperationException($"Only ACTIVE contracts can be force-expired. Current status = '{c.Status}'.");

            if (c.EndDate.Date >= today)
                throw new InvalidOperationException("Cannot force-expire: EndDate has not passed yet.");

            var oldAudit = new { c.Status, c.EndDate, c.Note };

            c.Status = ContractStatus.Expired;
            c.Note = AppendNote(c.Note, $"Force expired by admin at {DateTime.UtcNow:yyyy-MM-dd HH:mm} (UTC).");

            await _db.SaveChangesAsync(innerCt);

            await CreateVersionSnapshotInternalAsync(c.ContractId, actorUserId, "Force expire contract", innerCt);

            await _audit.LogAsync(
                actorUserId,
                action: "ForceExpireContract",
                entityType: "Contract",
                entityId: c.ContractId.ToString(),
                note: "Admin force expire",
                oldValue: oldAudit,
                newValue: new { c.Status, c.EndDate, c.Note },
                ct: innerCt);
        }, ct);
    }

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
                Status = c.Status,
                IsActive = c.Status == ContractStatus.Active,
                ContractCode = c.ContractCode
            })
            .ToListAsync(ct);

        return new PagedResultDto<ContractDto>(items, total, page, pageSize);
    }

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

    public async Task UpdateDepositAsync(int contractId, decimal newDeposit, int? actorUserId = null, CancellationToken ct = default)
    {
        EnsureNonNegative(newDeposit, nameof(newDeposit));

        var id = (long)contractId;

        await ExecuteInTransactionAsync(async innerCt =>
        {
            var contract = await _db.Contracts.FirstOrDefaultAsync(c => c.ContractId == id, innerCt);
            if (contract == null) throw new InvalidOperationException("Contract not found.");

            await CreateVersionSnapshotInternalAsync(contract.ContractId, actorUserId, "Before deposit update", innerCt);

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

            await _db.SaveChangesAsync(innerCt);

            await CreateVersionSnapshotInternalAsync(contract.ContractId, actorUserId, "After deposit update", innerCt);

            await _audit.LogAsync(
                actorUserId,
                action: "UpdateDeposit",
                entityType: "Contract",
                entityId: contract.ContractId.ToString(),
                note: "Update deposit amount",
                oldValue: new { DepositAmount = oldDeposit },
                newValue: new { DepositAmount = contract.DepositAmount },
                ct: innerCt);
        }, ct);
    }

    public async Task UpdateDepositAsync(UpdateDepositDto dto, int? actorUserId = null, CancellationToken ct = default)
    {
        if (dto is null) throw new ArgumentNullException(nameof(dto));

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

        await ExecuteInTransactionAsync(async innerCt =>
        {
            var c = await _db.Contracts.FirstOrDefaultAsync(x => x.ContractId == (long)dto.ContractId, innerCt);
            if (c == null) throw new InvalidOperationException("Contract not found.");

            await CreateVersionSnapshotInternalAsync(c.ContractId, actorUserId, "Before deposit update", innerCt);

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

            await _db.SaveChangesAsync(innerCt);

            await CreateVersionSnapshotInternalAsync(c.ContractId, actorUserId, "After deposit update", innerCt);

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
                ct: innerCt);
        }, ct);
    }
    public async Task AddAttachmentStubAsync(int contractId, string fileName, string url, int? actorUserId = null, CancellationToken ct = default)
    {
        var id = contractId;

        await ExecuteInTransactionAsync(async innerCt =>
        {
            var contractExists = await _db.Contracts.AsNoTracking()
                .AnyAsync(c => c.ContractId == id, innerCt);

            if (!contractExists)
                throw new InvalidOperationException("Contract not found.");

            _db.ContractAttachments.Add(new ContractAttachment
            {
                ContractId = id,
                FileName = fileName,
                FileUrl = url,
                UploadedAt = DateTime.UtcNow,
                UploadedByUserId = actorUserId
            });

            await _db.SaveChangesAsync(innerCt);

            await CreateVersionSnapshotInternalAsync(id, actorUserId, $"Add attachment: {fileName}", innerCt);

            await _audit.LogAsync(
                actorUserId,
                action: "UploadAttachment",
                entityType: "Contract",
                entityId: id.ToString(),
                note: fileName,
                oldValue: null,
                newValue: new { FileName = fileName, Url = url },
                ct: innerCt);
        }, ct, IsolationLevel.ReadCommitted);
    }

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

    public async Task<int> ScanAndSendExpiryRemindersAsync(
        int daysBeforeEnd = 7,
        string remindType = "Expiry_7d",
        int? actorUserId = null,
        CancellationToken ct = default)
    {
        var today = DateTime.Today;
        var target = today.AddDays(daysBeforeEnd);

        return await ExecuteInTransactionAsync(async innerCt =>
        {
            var contracts = await _db.Contracts.AsNoTracking()
                .Where(c => c.Status == ContractStatus.Active && c.EndDate.Date == target.Date)
                .Select(c => new { c.ContractId, c.ContractCode, c.TenantId })
                .ToListAsync(innerCt);

            var sent = 0;

            foreach (var c in contracts)
            {
                var already = await _db.ContractReminderLogs.AsNoTracking()
                    .AnyAsync(x =>
                        x.ContractId == c.ContractId &&
                        x.RemindType == remindType &&
                        x.RemindAtDate == today, innerCt);

                if (already) continue;

                var notification = new DAL.Entities.System.Notification
                {
                    Title = $"Contract Expiry Reminder ({daysBeforeEnd} days)",
                    Content = $"Contract {c.ContractCode} will expire on {target:yyyy-MM-dd}. Please review / renew / terminate.",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = null,
                    ContractId = c.ContractId
                };

                _db.Notifications.Add(notification);
                await _db.SaveChangesAsync(innerCt);

                _db.NotificationRecipients.Add(new NotificationRecipient
                {
                    NotificationId = notification.Id,
                    TenantId = c.TenantId,
                    IsRead = false,
                    ReadAt = null,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = null
                });

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
                await _db.SaveChangesAsync(innerCt);

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
                ct: innerCt);

            return sent;
        }, ct);
    }
    private static ContractDto MapToDto(Contract c) => new()
    {
        Id = (int)c.ContractId,
        RoomId = c.RoomId,
        TenantId = c.TenantId,
        StartDate = c.StartDate,
        EndDate = c.EndDate,
        Rent = c.BaseRent,
        Deposit = c.DepositAmount,
        Status = c.Status,
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


    public async Task<TenantContractDetailsDto?> GetActiveContractDetailsByUserIdAsync(string userId, CancellationToken ct = default)
    {
        var contract = await _db.Contracts
            .AsNoTracking()
            .Include(c => c.Room)
            .Include(c => c.Tenant)
            .Include(c => c.Attachments)
            .Where(c => c.Tenant.UserId == userId && c.Status == ContractStatus.Active)
            .OrderByDescending(c => c.StartDate)
            .FirstOrDefaultAsync(ct);

        if (contract == null)
            return null;

        return new TenantContractDetailsDto
        {
            ContractId = contract.ContractId,
            ContractCode = contract.ContractCode ?? string.Empty,
            Status = contract.Status,
            IsActive = string.Equals(contract.Status, ContractStatus.Active, StringComparison.OrdinalIgnoreCase),
            RoomId = contract.RoomId,
            RoomCode = contract.Room?.RoomCode ?? string.Empty,
            RoomName = contract.Room?.RoomName ?? string.Empty,
            TenantId = contract.TenantId,
            TenantName = contract.Tenant?.FullName ?? string.Empty,
            TenantEmail = contract.Tenant?.Email ?? string.Empty,
            StartDate = contract.StartDate,
            EndDate = contract.EndDate,
            Rent = contract.BaseRent,
            Deposit = contract.DepositAmount,
            DepositStatus = contract.DepositStatus ?? "Unpaid",
            DepositPaidAt = contract.DepositPaidAt,
            DepositPaidAmount = contract.DepositPaidAmount,
            Note = contract.Note,
            Attachments = contract.Attachments
                .OrderByDescending(a => a.UploadedAt)
                .Select(a => new BLL.DTOs.Contract.ContractAttachmentDto
                {
                    AttachmentId = a.AttachmentId,
                    FileName = a.FileName ?? string.Empty,
                    FileUrl = a.FileUrl,
                    UploadedAt = a.UploadedAt
                })
                .ToList()
        };
    }
}