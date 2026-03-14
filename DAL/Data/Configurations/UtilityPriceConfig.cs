using DAL.Entities.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DAL.Data.Configurations;

public class UtilityPriceConfig : IEntityTypeConfiguration<UtilityPrice>
{
    public void Configure(EntityTypeBuilder<UtilityPrice> b)
    {
        b.ToTable("UtilityPrices");
        b.HasKey(x => x.Id);

        b.Property(x => x.ElectricPerKwh).HasPrecision(18, 2);
        b.Property(x => x.WaterPerM3).HasPrecision(18, 2);

        b.HasIndex(x => x.EffectiveFrom).IsUnique();
    }
}
