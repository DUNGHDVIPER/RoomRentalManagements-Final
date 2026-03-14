using System.Text.Json;
using BLL.Services.Interfaces;
using DAL.Data;
using DAL.Entities.System;

namespace BLL.Services;

public class AuditService : IAuditService
{
    private readonly AppDbContext _db;

    public AuditService(AppDbContext db)
    {
        _db = db;
    }

    public async Task LogAsync(
        int? actorUserId,
        string action,
        string entityType,
        string entityId,
        string? note = null,
        object? oldValue = null,
        object? newValue = null,
        CancellationToken ct = default)
    {
        var oldJson = oldValue == null ? null : JsonSerializer.Serialize(oldValue);
        var newJson = newValue == null ? null : JsonSerializer.Serialize(newValue);

        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actorUserId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Note = note,
            OldValueJson = oldJson,
            NewValueJson = newJson,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);
    }
}