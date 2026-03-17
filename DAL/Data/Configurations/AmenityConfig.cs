using DAL.Entities.Property;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DAL.Data.Configurations;

public class AmenityConfig : IEntityTypeConfiguration<Amenity>
{
    public void Configure(EntityTypeBuilder<Amenity> b)
    {
        b.ToTable("Amenities");

        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("AmenityId"); // ✅ map về cột cũ

        b.Property(x => x.AmenityName)
            .HasMaxLength(100)
            .IsRequired();

        b.HasIndex(x => x.AmenityName).IsUnique(false);
    }
}