using DAL.Entities.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DAL.Data.Configurations;

public class ContractReminderConfig : IEntityTypeConfiguration<ContractReminder>
{
    public void Configure(EntityTypeBuilder<ContractReminder> b)
    {
        b.ToTable("ContractReminders");
        b.HasKey(x => x.Id);

        b.Property(x => x.Type).HasMaxLength(30).IsRequired();

        b.HasOne(x => x.Contract)
            .WithMany() // hoặc .WithMany(x => x.Reminders) nếu Contract có navigation
            .HasForeignKey(x => x.ContractId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => new { x.ContractId, x.RemindAt });
    }
}