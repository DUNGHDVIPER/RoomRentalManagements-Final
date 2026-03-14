using DAL.Entities.Property;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DAL.Data.Configurations.Property;

public class FloorConfig : IEntityTypeConfiguration<Floor>
{
    public void Configure(EntityTypeBuilder<Floor> b)
    {
        b.ToTable("Floors");

        b.HasKey(x => x.Id);
        b.Property(x => x.Id)
            .HasColumnName("FloorId");

        b.Property(x => x.BlockId)
            .IsRequired();

        b.Property(x => x.FloorName)
            .IsRequired()
            .HasMaxLength(50);

        b.HasOne(x => x.Block)
            .WithMany(x => x.Floors)
            .HasForeignKey(x => x.BlockId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Property(x => x.CreatedAt)
            .HasDefaultValueSql("SYSDATETIME()");
    }
}