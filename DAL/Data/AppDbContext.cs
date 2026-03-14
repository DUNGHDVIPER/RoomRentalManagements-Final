using DAL.Entities.Billing;
using DAL.Entities.Common;
using DAL.Entities.Contracts;
using DAL.Entities.Motel;
using DAL.Entities.System;
using DAL.Entities.Tenanting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

using AmenityEntity = DAL.Entities.Property.Amenity;
using BlockEntity = DAL.Entities.Property.Block;
using FloorEntity = DAL.Entities.Property.Floor;
using RoomAmenityEntity = DAL.Entities.Property.RoomAmenity;
using RoomEntity = DAL.Entities.Property.Room;
using RoomImageEntity = DAL.Entities.Property.RoomImage;
using RoomPricingHistoryEntity = DAL.Entities.Property.RoomPricingHistory;

namespace DAL.Data;

public class AppDbContext : IdentityDbContext<IdentityUser, IdentityRole, string>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // =========================
    // SYSTEM
    // =========================
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationRecipient> NotificationRecipients => Set<NotificationRecipient>();

    // =========================
    // PROPERTY
    // =========================
    public DbSet<BlockEntity> Blocks => Set<BlockEntity>();
    public DbSet<FloorEntity> Floors => Set<FloorEntity>();
    public DbSet<RoomEntity> Rooms => Set<RoomEntity>();
    public DbSet<AmenityEntity> Amenities => Set<AmenityEntity>();
    public DbSet<RoomAmenityEntity> RoomAmenities => Set<RoomAmenityEntity>();
    public DbSet<RoomImageEntity> RoomImages => Set<RoomImageEntity>();
    public DbSet<RoomPricingHistoryEntity> RoomPricingHistories => Set<RoomPricingHistoryEntity>();

    // =========================
    // TENANTING
    // =========================
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantIdDoc> TenantIdDocs => Set<TenantIdDoc>();
    public DbSet<StayHistory> StayHistories => Set<StayHistory>();
    public DbSet<RoomResident> RoomResidents => Set<RoomResident>();

    // =========================
    // CONTRACTS
    // =========================
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<ContractAttachment> ContractAttachments => Set<ContractAttachment>();
    public DbSet<ContractVersion> ContractVersions => Set<ContractVersion>();
    public DbSet<Deposit> Deposits => Set<Deposit>();
    public DbSet<ContractReminder> ContractReminders => Set<ContractReminder>();
    public DbSet<ContractReminderLog> ContractReminderLogs => Set<ContractReminderLog>();

    // =========================
    // BILLING
    // =========================
    public DbSet<Bill> Bills => Set<Bill>();
    public DbSet<BillItem> BillItems => Set<BillItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<BillStatusHistory> BillStatusHistories => Set<BillStatusHistory>();
    public DbSet<UtilityPrice> UtilityPrices => Set<UtilityPrice>();
    public DbSet<UtilityReading> UtilityReadings => Set<UtilityReading>();
    public DbSet<ExtraFee> ExtraFees => Set<ExtraFee>();

    // =========================
    // MAINTENANCE
    // =========================
    public DbSet<DAL.Entities.Maintenance.Ticket> Tickets => Set<DAL.Entities.Maintenance.Ticket>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Dùng chung schema dbo cho toàn bộ app
        modelBuilder.HasDefaultSchema("dbo");

        // Load entity configuration automatically
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // =========================
        // PROPERTY
        // =========================
        modelBuilder.Entity<BlockEntity>(e =>
        {
            e.ToTable("Blocks", "dbo");
            e.HasKey(x => x.Id);

            e.Property(x => x.BlockName)
                .HasMaxLength(100)
                .IsRequired();

            e.Property(x => x.Address)
                .HasMaxLength(300);

            e.Property(x => x.Note)
                .HasMaxLength(1000);

            e.Property(x => x.Status)
                .HasMaxLength(30)
                .HasDefaultValue("Active");
        });

        modelBuilder.Entity<FloorEntity>(e =>
        {
            e.ToTable("Floors", "dbo");
            e.HasKey(x => x.Id);

            e.Property(x => x.FloorName)
                .HasMaxLength(50)
                .IsRequired();

            e.HasOne(x => x.Block)
                .WithMany(x => x.Floors)
                .HasForeignKey(x => x.BlockId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RoomEntity>(e =>
        {
            e.ToTable("Rooms", "dbo");
            e.HasKey(x => x.RoomId);

            e.Property(x => x.RoomCode)
                .HasMaxLength(20)
                .IsRequired();

            e.Property(x => x.RoomName)
                .HasMaxLength(100);

            e.Property(x => x.AreaM2)
                .HasPrecision(18, 2);

            e.Property(x => x.CurrentBasePrice)
                .HasPrecision(18, 2);

            e.Property(x => x.Description)
                .HasMaxLength(1000);

            e.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(30);

            e.HasOne(x => x.Floor)
                .WithMany(x => x.Rooms)
                .HasForeignKey(x => x.FloorId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AmenityEntity>(e =>
        {
            e.ToTable("Amenities", "dbo");
        });

        modelBuilder.Entity<RoomAmenityEntity>(e =>
        {
            e.ToTable("RoomAmenities", "dbo");
            e.HasKey(x => new { x.RoomId, x.AmenityId });

            e.HasOne(x => x.Room)
                .WithMany(x => x.RoomAmenities)
                .HasForeignKey(x => x.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Amenity)
                .WithMany(x => x.RoomAmenities)
                .HasForeignKey(x => x.AmenityId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RoomImageEntity>(e =>
        {
            e.ToTable("RoomImages", "dbo");
        });

        modelBuilder.Entity<RoomPricingHistoryEntity>(e =>
        {
            e.ToTable("RoomPriceHistories", "dbo");
        });

        modelBuilder.Entity<AmenityEntity>(e =>
        {
            e.ToTable("Amenities", "dbo");
        });

        modelBuilder.Entity<RoomAmenityEntity>(e =>
        {
            e.ToTable("RoomAmenities", "dbo");
            e.HasKey(x => new { x.RoomId, x.AmenityId });

            e.HasOne(x => x.Room)
                .WithMany(x => x.RoomAmenities)
                .HasForeignKey(x => x.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Amenity)
                .WithMany(x => x.RoomAmenities)
                .HasForeignKey(x => x.AmenityId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RoomImageEntity>(e =>
        {
            e.ToTable("RoomImages", "dbo");
        });

        modelBuilder.Entity<RoomPricingHistoryEntity>(e =>
        {
            e.ToTable("RoomPriceHistories", "dbo");
        });

        // =========================
        // CONTRACT FIX
        // =========================
        modelBuilder.Entity<Contract>(e =>
        {
            e.Ignore(x => x.Id);
            e.HasKey(x => x.ContractId);
            e.Property(x => x.ContractId).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<ContractVersion>(e =>
        {
            e.HasKey(x => x.VersionId);
            e.Property(x => x.VersionId).ValueGeneratedOnAdd();
        });

        // =========================
        // AUDIT LOG FIX
        // =========================
        modelBuilder.Entity<AuditLog>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.OldValueJson).HasColumnType("nvarchar(max)");
            e.Property(x => x.NewValueJson).HasColumnType("nvarchar(max)");
        });

        // =========================
        // USER ROLE RELATION
        // =========================
        modelBuilder.Entity<UserRole>(e =>
        {
            e.HasKey(x => new { x.UserId, x.RoleId });

            e.HasOne(x => x.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(x => x.UserId);

            e.HasOne(x => x.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(x => x.RoleId);
        });

        // =========================
        // UTILITY READING PRECISION
        // =========================
        modelBuilder.Entity<UtilityReading>(e =>
        {
            e.Property(x => x.ElectricKwh).HasPrecision(18, 3);
            e.Property(x => x.WaterM3).HasPrecision(18, 3);
        });

        modelBuilder.Entity<Deposit>(e =>
        {
            e.HasKey(x => x.DepositId);

            e.Property(x => x.Amount)
                .HasPrecision(18, 2);

            e.HasOne(x => x.Contract)
                .WithMany(x => x.Deposits)
                .HasForeignKey(x => x.ContractId)
                .OnDelete(DeleteBehavior.Cascade);
        });

    }
}