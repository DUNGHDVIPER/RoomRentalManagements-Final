using DAL.Entities.Tenanting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DAL.Data.Configurations;

public class TenantConfig : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> b)
    {
        b.ToTable("Tenants");
        b.HasKey(x => x.Id);

        b.Property(x => x.FullName).HasMaxLength(150).IsRequired();
        b.Property(x => x.Phone).HasMaxLength(30);
        b.Property(x => x.Email).HasMaxLength(150);

        b.HasMany(x => x.IdDocs)
         .WithOne(x => x.Tenant)
         .HasForeignKey(x => x.TenantId)
         .OnDelete(DeleteBehavior.Cascade);

        b.HasMany(x => x.StayHistories)
         .WithOne(x => x.Tenant)
         .HasForeignKey(x => x.TenantId)
         .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => x.UserId).HasDatabaseName("IX_Tenants_IdentityUserId");
    }
}
