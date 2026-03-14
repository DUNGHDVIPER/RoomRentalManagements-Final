using DAL.Entities.Tenanting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DAL.Data.Configurations;

public class TenantIdDocConfig : IEntityTypeConfiguration<TenantIdDoc>
{
    public void Configure(EntityTypeBuilder<TenantIdDoc> b)
    {
        b.ToTable("TenantIdDocs");
        b.HasKey(x => x.Id);

        b.Property(x => x.DocType).HasMaxLength(30).IsRequired();
        b.Property(x => x.DocNumber).HasMaxLength(50).IsRequired();
        b.Property(x => x.ImageUrl).HasMaxLength(500);

        b.HasIndex(x => new { x.TenantId, x.DocType, x.DocNumber })
         .HasDatabaseName("IX_TenantIdDocs_Tenant_Doc");
    }
}
