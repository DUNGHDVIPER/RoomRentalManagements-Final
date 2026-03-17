using DAL.Entities.Property;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DAL.Data.Configurations;

public class RoomPriceHistoryConfig : IEntityTypeConfiguration<RoomPriceHistory>
{
    public void Configure(EntityTypeBuilder<RoomPriceHistory> b)
    {
        b.ToTable("RoomPriceHistories");
        b.HasKey(x => x.Id);

        b.Property(x => x.Price).HasPrecision(18, 2);
        b.HasIndex(x => new { x.RoomId, x.FromDate });
    }
}
