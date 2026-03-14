using DAL.Entities.Property;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DAL.Data.Configurations.Property;

public class RoomAmenityConfig : IEntityTypeConfiguration<RoomAmenity>
{
    public void Configure(EntityTypeBuilder<RoomAmenity> b)
    {
        b.ToTable("RoomAmenities");

        b.HasKey(x => new { x.RoomId, x.AmenityId });

        b.HasOne(x => x.Room)
            .WithMany(x => x.RoomAmenities)
            .HasForeignKey(x => x.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Amenity)
            .WithMany(x => x.RoomAmenities)
            .HasForeignKey(x => x.AmenityId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}