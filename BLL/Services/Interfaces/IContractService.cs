using BLL.Common;
using BLL.DTOs.Common;
using BLL.DTOs.Contract;

namespace BLL.Services.Interfaces;

public interface IContractService
{
    Task<ContractDto> CreateAsync(CreateContractDto dto, int? actorUserId = null, CancellationToken ct = default);
    Task ActivateAsync(int contractId, int? actorUserId = null, CancellationToken ct = default);
    Task<ContractDto> RenewAsync(RenewContractDto dto, int? actorUserId = null, CancellationToken ct = default);
    Task TerminateAsync(TerminateContractDto dto, int? actorUserId = null, CancellationToken ct = default);
    Task ForceExpireAsync(int contractId, int? actorUserId = null, CancellationToken ct = default);
    Task<PagedResultDto<ContractDto>> GetContractsAsync(PagedRequestDto req, CancellationToken ct = default);

    Task<ContractDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ContractDto> GetByIdAsync(long contractId, CancellationToken ct = default);

    Task UpdateDepositAsync(UpdateDepositDto dto, int? actorUserId = null, CancellationToken ct = default);
    Task UpdateDepositAsync(int contractId, decimal newDeposit, int? actorUserId = null, CancellationToken ct = default);

    Task AddAttachmentStubAsync(int contractId, string fileName, string url, int? actorUserId = null, CancellationToken ct = default);

    Task CreateVersionSnapshotAsync(int contractId, string changedByUserId, CancellationToken ct = default);
    Task<List<ContractVersionItemDto>> GetVersionsAsync(int contractId, CancellationToken ct = default);

    Task CreateReminderAsync(int contractId, DateTime remindAt, string type, CancellationToken ct = default);


    Task<byte[]> ExportPdfStubAsync(int contractId, int? actorUserId = null, CancellationToken ct = default);

    Task<int> ScanAndSendExpiryRemindersAsync(
        int daysBeforeEnd = 7,
        string remindType = "Expiry_7d",
        int? actorUserId = null,
        CancellationToken ct = default);

    Task<int?> GetActiveContractIdByUserIdAsync(string userId);

}
