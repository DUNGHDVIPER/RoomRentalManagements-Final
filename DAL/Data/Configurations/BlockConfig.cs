using DAL.Entities.Property;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DAL.Data.Configurations.Property;

public class BlockConfig : IEntityTypeConfiguration<Block>
{
    public void Configure(EntityTypeBuilder<Block> b)
    {
        b.ToTable("Blocks");

        b.HasKey(x => x.Id);
        b.Property(x => x.Id)
            .HasColumnName("BlockId");

        b.Property(x => x.BlockName)
            .IsRequired()
            .HasMaxLength(100);

        b.Property(x => x.Address)
            .HasMaxLength(255);

        b.Property(x => x.Note)
            .HasMaxLength(255);

        b.Property(x => x.CreatedAt)
            .HasDefaultValueSql("SYSDATETIME()");
    }
}