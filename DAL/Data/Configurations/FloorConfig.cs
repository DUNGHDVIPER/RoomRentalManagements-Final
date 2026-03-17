using DAL.Entities.Property;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DAL.Data.Configurations;

public class FloorConfig : IEntityTypeConfiguration<Floor>
{
    public void Configure(EntityTypeBuilder<Floor> b)
    {
        b.ToTable("Floors");
        b.HasKey(x => x.Id);

        b.Property(x => x.Name).HasMaxLength(50).IsRequired();

        b.HasMany(x => x.Rooms)
         .WithOne(x => x.Floor)
         .HasForeignKey(x => x.FloorId)
         .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => new { x.BlockId, x.Level }).HasDatabaseName("IX_Floors_Block_Level");
    }
}
