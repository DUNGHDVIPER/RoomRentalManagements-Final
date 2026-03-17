using DAL.Entities.Property;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DAL.Data.Configurations;

public class RoomConfig : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> b)
    {
        b.ToTable("Rooms");
        b.HasKey(x => x.Id);

        b.Property(x => x.RoomNo).HasMaxLength(20).IsRequired();
        b.Property(x => x.Name).HasMaxLength(100);
        b.Property(x => x.BasePrice).HasPrecision(18, 2);

        b.HasIndex(x => new { x.FloorId, x.RoomNo }).IsUnique();

        b.HasMany(x => x.RoomImages)
         .WithOne(x => x.Room)
         .HasForeignKey(x => x.RoomId)
         .OnDelete(DeleteBehavior.Cascade);

        b.HasMany(x => x.RoomPriceHistories)
         .WithOne(x => x.Room)
         .HasForeignKey(x => x.RoomId)
         .OnDelete(DeleteBehavior.Cascade);
    }
}
