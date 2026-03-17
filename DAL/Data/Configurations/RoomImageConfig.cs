using DAL.Entities.Property;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DAL.Data.Configurations;

public class RoomImageConfig : IEntityTypeConfiguration<RoomImage>
{
    public void Configure(EntityTypeBuilder<RoomImage> b)
    {
        b.ToTable("RoomImages");
        b.HasKey(x => x.Id);

        b.Property(x => x.Url).HasMaxLength(500).IsRequired();
        b.HasIndex(x => new { x.RoomId, x.SortOrder });
    }
}
