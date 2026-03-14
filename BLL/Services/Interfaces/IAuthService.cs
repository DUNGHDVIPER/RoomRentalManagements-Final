using System.Threading;
using System.Threading.Tasks;

namespace BLL.Services.Interfaces;

public interface IAuditService
{
    Task LogAsync(
        int? actorUserId,
        string action,
        string entityType,
        string entityId,
        string? note = null,
        object? oldValue = null,
        object? newValue = null,
        CancellationToken ct = default);
}