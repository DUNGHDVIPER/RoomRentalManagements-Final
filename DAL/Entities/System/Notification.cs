using DAL.Entities.Common;
using DAL.Entities.Contracts;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Entities.System;

public class Notification : AuditableEntity<long>
{
    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;

    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }

    public SourceType SourceType { get; set; }

    public int? ContractId { get; set; }
    public Contract? Contract { get; set; }

    public string? ReceiverUserId { get; set; }

    [ForeignKey("ReceiverUserId")]
    public IdentityUser? ReceiverUser { get; set; }
}