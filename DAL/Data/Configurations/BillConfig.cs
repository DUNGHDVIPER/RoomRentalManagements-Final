using DAL.Entities.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DAL.Data.Configurations;

public class BillConfig : IEntityTypeConfiguration<Bill>
{
    public void Configure(EntityTypeBuilder<Bill> b)
    {
        b.ToTable("Bills");
        b.HasKey(x => x.Id);

        b.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");

        b.HasOne(x => x.Contract)
            .WithMany(c => c.Bills)
            .HasForeignKey(x => x.ContractId)
            .HasPrincipalKey(c => c.ContractId) // ✅ quan trọng
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => new { x.ContractId, x.Period }).IsUnique();
    }
}