using DAL.Entities.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DAL.Data.Configurations;

public class BillItemConfig : IEntityTypeConfiguration<BillItem>
{
    public void Configure(EntityTypeBuilder<BillItem> b)
    {
        b.ToTable("BillItems");
        b.HasKey(x => x.Id);

        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.Amount).HasPrecision(18, 2);

        b.HasOne(x => x.ExtraFee)
         .WithMany()
         .HasForeignKey(x => x.ExtraFeeId)
         .OnDelete(DeleteBehavior.SetNull);
    }
}
