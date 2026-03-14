using DAL.Entities.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DAL.Data.Configurations;

public class ContractVersionConfig : IEntityTypeConfiguration<ContractVersion>
{
    public void Configure(EntityTypeBuilder<ContractVersion> b)
    {
        b.ToTable("ContractVersions");
        b.HasKey(x => x.ContractId);

        b.Property(x => x.SnapshotJson).IsRequired();

        b.HasOne(x => x.Contract)
            .WithMany(x => x.Versions)
            .HasForeignKey(x => x.ContractId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => new { x.ContractId, x.VersionNumber }).IsUnique();
    }
}