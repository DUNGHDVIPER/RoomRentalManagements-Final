using DAL.Entities.Property;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DAL.Data.Configurations.Property;

public class AmenityConfig : IEntityTypeConfiguration<Amenity>
{
    public void Configure(EntityTypeBuilder<Amenity> b)
    {
        b.ToTable("Amenities");

        b.HasKey(x => x.Id);
        b.Property(x => x.Id)
            .HasColumnName("AmenityId");

        b.Property(x => x.AmenityName)
            .IsRequired()
            .HasMaxLength(100);

        b.HasIndex(x => x.AmenityName)
            .IsUnique();

        b.Property(x => x.CreatedAt)
            .HasDefaultValueSql("SYSDATETIME()");
    }
}