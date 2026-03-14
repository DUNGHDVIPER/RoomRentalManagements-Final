using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Entities.Motel;

// -------------------------
// A) AUTH + RBAC + AUDIT
// -------------------------
public class User
{
    [Key] public int UserId { get; set; }
    [MaxLength(150)] public string Email { get; set; } = null!;
    [MaxLength(300)] public string? PasswordHash { get; set; }
    [MaxLength(150)] public string FullName { get; set; } = null;
    [MaxLength(30)] public string? Phone { get; set; }
    [MaxLength(500)] public string? AvatarUrl { get; set; }
    public bool IsLocked { get; set; }
    [MaxLength(255)] public string? LockReason { get; set; }
    [MaxLength(30)] public string? LoginProvider { get; set; }
    [MaxLength(200)] public string? ProviderUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

public class Role
{
    [Key] public int RoleId { get; set; }
    [MaxLength(50)] public string RoleName { get; set; } = null!;
    [MaxLength(200)] public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

public class UserRole
{
    public int UserId { get; set; }
    public User User { get; set; }

    public int RoleId { get; set; }
    public Role Role { get; set; }
}

/*public class AuditLog
{
[Key]
    public long AuditLogId { get; set; }

    public int? ActorUserId { get; set; }                 // nullable (system jobs)

    [MaxLength(100)]
    public string Action { get; set; } = null!;           // e.g. "CreateContract"

    [MaxLength(100)]
    public string EntityType { get; set; } = null!;       // e.g. "Contract"

    [MaxLength(50)]
    public string EntityId { get; set; } = null!;         // e.g. "123"

    public DateTime CreatedAt { get; set; }               // UTC recommended

    [MaxLength(500)]
    public string? Note { get; set; }                     // short note

    public string? OldValueJson { get; set; }             // nvarchar(max)
    public string? NewValueJson { get; set; }             // nvarchar(max)

    // optional navigation if you have User entity
    public User? ActorUser { get; set; }
}*/
