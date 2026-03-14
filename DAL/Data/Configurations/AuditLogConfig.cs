using DAL.Entities.System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DAL.Data.Configurations;

public class AuditLogConfig : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("AuditLogs");
        b.HasKey(x => x.Id);

        b.Property(x => x.ActorUserId).HasMaxLength(450);
        b.Property(x => x.Action).HasMaxLength(50).IsRequired();
        b.Property(x => x.EntityType).HasMaxLength(80).IsRequired();
        b.Property(x => x.EntityId).HasMaxLength(120);
        b.Property(x => x.Note).HasMaxLength(2000);

        b.HasIndex(x => new { x.ActorUserId, x.Action, x.CreatedAt });
    }
}
