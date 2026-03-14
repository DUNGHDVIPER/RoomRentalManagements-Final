using DAL.Entities.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DAL.Data.Configurations;

public class UtilityReadingConfig : IEntityTypeConfiguration<UtilityReading>
{
    public void Configure(EntityTypeBuilder<UtilityReading> b)
    {
        b.ToTable("UtilityReadings");
        b.HasKey(x => x.Id);

        b.HasIndex(x => new { x.RoomId, x.Period }).IsUnique();

        b.HasOne(x => x.Room)
         .WithMany()
         .HasForeignKey(x => x.RoomId)
         .OnDelete(DeleteBehavior.Restrict);
    }
}
