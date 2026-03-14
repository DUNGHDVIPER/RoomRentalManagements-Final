using DAL.Entities.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DAL.Data.Configurations;

public class ExtraFeeConfig : IEntityTypeConfiguration<ExtraFee>
{
    public void Configure(EntityTypeBuilder<ExtraFee> b)
    {
        b.ToTable("ExtraFees");
        b.HasKey(x => x.Id);

        b.Property(x => x.Name).HasMaxLength(120).IsRequired();
        b.Property(x => x.DefaultAmount).HasPrecision(18, 2);

        b.HasIndex(x => x.Name).IsUnique();
    }
}
