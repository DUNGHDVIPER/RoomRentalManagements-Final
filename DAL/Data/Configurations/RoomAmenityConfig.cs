using DAL.Entities.Property;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DAL.Data.Configurations;

public class RoomAmenityConfig : IEntityTypeConfiguration<RoomAmenity>
{
    public void Configure(EntityTypeBuilder<RoomAmenity> b)
    {
        b.ToTable("RoomAmenities");
        b.HasKey(x => new { x.RoomId, x.AmenityId });

        b.HasOne(x => x.Room)
            .WithMany(r => r.RoomAmenities)
            .HasForeignKey(x => x.RoomId);

        b.HasOne(x => x.Amenity)
            .WithMany(a => a.RoomAmenities)
            .HasForeignKey(x => x.AmenityId);
    }
}