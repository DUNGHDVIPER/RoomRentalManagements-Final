using DAL.Entities.Common;
using System.ComponentModel.DataAnnotations;

namespace DAL.Entities.System;

public class AuditLog : AuditableEntity<long>
{
    public int? ActorUserId { get; set; }

    [MaxLength(100)]
    public string Action { get; set; } = null!;

    [MaxLength(100)]
    public string EntityType { get; set; } = null!;

    [MaxLength(50)]
    public string EntityId { get; set; } = null!;

    [MaxLength(500)]
    public string? Note { get; set; }

    public string? OldValueJson { get; set; }
    public string? NewValueJson { get; set; }
}