using DAL.Entities.Common;
using DAL.Entities.Tenanting;
using System.ComponentModel.DataAnnotations;

namespace DAL.Entities.System;

public class NotificationRecipient : AuditableEntity<long>
{
    public long NotificationId { get; set; }
    public int TenantId { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }

    public Notification Notification { get; set; } = null!;
}