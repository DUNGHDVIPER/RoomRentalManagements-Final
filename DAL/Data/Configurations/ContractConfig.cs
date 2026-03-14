using DAL.Entities.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DAL.Data.Configurations;

public class ContractConfig : IEntityTypeConfiguration<Contract>
{
    public void Configure(EntityTypeBuilder<Contract> b)
    {
        b.ToTable("Contracts");
        /*b.HasKey(x => x.Id);
*/
        b.Property(x => x.ContractId)
               .ValueGeneratedOnAdd();  // ✅ identity

        b.HasIndex(x => x.RoomId)
            .HasDatabaseName("UX_Contracts_Room_ActiveOnly")
            .IsUnique()
            .HasFilter("[Status] = N'Active'");

        b.HasIndex(x => new { x.TenantId, x.StartDate });
    }
}