using DAL.Entities.Tenanting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DAL.Data.Configurations;

public class StayHistoryConfig : IEntityTypeConfiguration<StayHistory>
{
    public void Configure(EntityTypeBuilder<StayHistory> b)
    {
        b.ToTable("StayHistories");
        b.HasKey(x => x.Id);

        b.HasOne(x => x.Room)
         .WithMany()
         .HasForeignKey(x => x.RoomId)
         .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new { x.RoomId, x.CheckInAt });
    }
}
