using DAL.Entities.Property;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DAL.Data.Configurations.Property;

public class RoomPricingHistoryConfig : IEntityTypeConfiguration<RoomPricingHistory>
{
    public void Configure(EntityTypeBuilder<RoomPricingHistory> b)
    {
        b.ToTable("RoomPricingHistory");

        b.HasKey(x => x.PriceId);

        b.Property(x => x.PriceId)
            .HasColumnName("PriceId");

        b.Property(x => x.RoomId)
            .IsRequired();

        b.Property(x => x.OldPrice)
            .HasColumnType("decimal(18,2)");

        b.Property(x => x.NewPrice)
            .HasColumnType("decimal(18,2)");

        b.Property(x => x.ChangedAt)
            .HasDefaultValueSql("SYSDATETIME()");

        b.Property(x => x.Note)
            .HasMaxLength(255);

        b.HasOne(x => x.Room)
            .WithMany(x => x.RoomPricingHistories)
            .HasForeignKey(x => x.RoomId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}