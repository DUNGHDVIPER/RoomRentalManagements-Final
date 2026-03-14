using DAL.Entities.Common;
using DAL.Entities.Property;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DAL.Data.Configurations.Property;

public class RoomConfig : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> b)
    {
        b.ToTable("Rooms");

        b.HasKey(x => x.RoomId);

        b.Property(x => x.RoomId)
            .HasColumnName("RoomId");

        b.Property(x => x.FloorId)
            .IsRequired();

        b.Property(x => x.RoomCode)
            .IsRequired()
            .HasMaxLength(50);

        b.HasIndex(x => x.RoomCode)
            .IsUnique();

        b.Property(x => x.RoomName)
            .HasMaxLength(100);

        b.Property(x => x.AreaM2)
            .HasColumnType("decimal(10,2)");

        b.Property(x => x.MaxOccupants)
            .HasDefaultValue(2);

        b.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        b.Property(x => x.CurrentBasePrice)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0);

        b.Property(x => x.Description)
            .HasMaxLength(500);

        b.Property(x => x.CreatedAt)
            .HasDefaultValueSql("SYSDATETIME()");

        b.HasOne(x => x.Floor)
            .WithMany(x => x.Rooms)
            .HasForeignKey(x => x.FloorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}