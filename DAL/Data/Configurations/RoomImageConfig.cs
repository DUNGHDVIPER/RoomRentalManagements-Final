using DAL.Entities.Property;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DAL.Data.Configurations.Property;

public class RoomImageConfig : IEntityTypeConfiguration<RoomImage>
{
    public void Configure(EntityTypeBuilder<RoomImage> b)
    {
        b.ToTable("RoomImages");

        b.HasKey(x => x.ImageId);
        b.Property(x => x.ImageId)
            .HasColumnName("ImageId");

        b.Property(x => x.RoomId)
            .IsRequired();

        b.Property(x => x.ImageUrl)
            .IsRequired()
            .HasMaxLength(500);

        b.Property(x => x.IsPrimary)
            .HasDefaultValue(false);

        b.HasOne(x => x.Room)
            .WithMany(x => x.RoomImages)
            .HasForeignKey(x => x.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Property(x => x.CreatedAt)
            .HasDefaultValueSql("SYSDATETIME()");
    }
}