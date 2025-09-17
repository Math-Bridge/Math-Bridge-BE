using System;
using System.Collections.Generic;
using MathBridge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MathBridge.Infrastructure.Data;

public partial class MathBridgeDbContext : DbContext
{
    public MathBridgeDbContext()
    {
    }

    public MathBridgeDbContext(DbContextOptions<MathBridgeDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<School> Schools { get; set; }

    public virtual DbSet<SePayTransaction> SePayTransactions { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<WalletTransaction> WalletTransactions { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=sql.vibe88.tech;Database=mathbridge;User Id=sa;Password=Phineas160404;MultipleActiveResultSets=true;TrustServerCertificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__roles__760965CC610C41A2");

            entity.ToTable("roles");

            entity.HasIndex(e => e.RoleName, "UQ__roles__783254B1FA804F1D").IsUnique();

            entity.HasIndex(e => e.RoleName, "ix_roles_name");

            entity.Property(e => e.RoleId)
                .ValueGeneratedNever()
                .HasColumnName("role_id");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasColumnName("description");
            entity.Property(e => e.RoleName)
                .HasMaxLength(50)
                .HasColumnName("role_name");
        });

        modelBuilder.Entity<School>(entity =>
        {
            entity.HasKey(e => e.SchoolId).HasName("PK__schools__27CA6CF476FA07F4");

            entity.ToTable("schools");

            entity.HasIndex(e => e.City, "IX_Schools_City");

            entity.HasIndex(e => new { e.Latitude, e.Longitude }, "IX_Schools_Location");

            entity.HasIndex(e => e.Name, "ix_schools_name");

            entity.Property(e => e.SchoolId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("school_id");
            entity.Property(e => e.City)
                .HasMaxLength(100)
                .HasColumnName("city");
            entity.Property(e => e.CountryCode)
                .HasMaxLength(2)
                .HasDefaultValue("VN")
                .HasColumnName("country_code");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_date");
            entity.Property(e => e.District)
                .HasMaxLength(100)
                .HasColumnName("district");
            entity.Property(e => e.FormattedAddress)
                .HasMaxLength(500)
                .HasColumnName("formatted_address");
            entity.Property(e => e.GooglePlaceId)
                .HasMaxLength(255)
                .HasColumnName("google_place_id");
            entity.Property(e => e.Latitude).HasColumnName("latitude");
            entity.Property(e => e.LocationUpdatedDate)
                .HasColumnType("datetime")
                .HasColumnName("location_updated_date");
            entity.Property(e => e.Longitude).HasColumnName("longitude");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.PlaceName)
                .HasMaxLength(255)
                .HasColumnName("place_name");
            entity.Property(e => e.UpdatedDate)
                .HasColumnType("datetime")
                .HasColumnName("updated_date");
        });

        modelBuilder.Entity<SePayTransaction>(entity =>
        {
            entity.HasIndex(e => e.Code, "ix_sepay_transactions_code").IsUnique();

            entity.HasIndex(e => e.TransactionDate, "ix_sepay_transactions_date");

            entity.HasIndex(e => e.OrderReference, "ix_sepay_transactions_order_reference");

            entity.HasIndex(e => e.WalletTransactionId, "ix_sepay_transactions_wallet_transaction_id");

            entity.Property(e => e.SepayTransactionId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("sepay_transaction_id");
            entity.Property(e => e.AccountNumber)
                .HasMaxLength(50)
                .HasColumnName("account_number");
            entity.Property(e => e.Accumulated)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("accumulated");
            entity.Property(e => e.Code)
                .HasMaxLength(100)
                .HasColumnName("code");
            entity.Property(e => e.Content)
                .HasMaxLength(500)
                .HasColumnName("content");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasMaxLength(1000)
                .HasColumnName("description");
            entity.Property(e => e.Gateway)
                .HasMaxLength(50)
                .HasColumnName("gateway");
            entity.Property(e => e.OrderReference)
                .HasMaxLength(50)
                .HasColumnName("order_reference");
            entity.Property(e => e.ReferenceNumber)
                .HasMaxLength(100)
                .HasColumnName("reference_number");
            entity.Property(e => e.SubAccount)
                .HasMaxLength(50)
                .HasColumnName("sub_account");
            entity.Property(e => e.TransactionDate).HasColumnName("transaction_date");
            entity.Property(e => e.TransferAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("transfer_amount");
            entity.Property(e => e.TransferType)
                .HasMaxLength(10)
                .HasColumnName("transfer_type");
            entity.Property(e => e.WalletTransactionId).HasColumnName("wallet_transaction_id");

            entity.HasOne(d => d.WalletTransaction).WithMany(p => p.SePayTransactions)
                .HasForeignKey(d => d.WalletTransactionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_sepay_transactions_wallet_transaction");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__users__B9BE370F99768E01");

            entity.ToTable("users");

            entity.HasIndex(e => new { e.City, e.District }, "IX_users_city_district").HasFilter("([city] IS NOT NULL)");

            entity.HasIndex(e => new { e.Latitude, e.Longitude }, "IX_users_location_coordinates");

            entity.HasIndex(e => e.Email, "UQ__users__AB6E616455322907").IsUnique();

            entity.HasIndex(e => e.Email, "ix_users_email");

            entity.HasIndex(e => e.RoleId, "ix_users_role_id");

            entity.HasIndex(e => e.Status, "ix_users_status");

            entity.Property(e => e.UserId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("user_id");
            entity.Property(e => e.City)
                .HasMaxLength(100)
                .HasColumnName("city");
            entity.Property(e => e.CountryCode)
                .HasMaxLength(2)
                .HasDefaultValue("VN")
                .HasColumnName("country_code");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_date");
            entity.Property(e => e.District)
                .HasMaxLength(100)
                .HasColumnName("district");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.FormattedAddress)
                .HasMaxLength(500)
                .HasComment("Complete formatted address from Google Places API")
                .HasColumnName("formatted_address");
            entity.Property(e => e.FullName)
                .HasMaxLength(255)
                .HasColumnName("full_name");
            entity.Property(e => e.Gender)
                .HasMaxLength(10)
                .HasColumnName("gender");
            entity.Property(e => e.GooglePlaceId)
                .HasMaxLength(255)
                .HasComment("Google Places API place identifier for location accuracy")
                .HasColumnName("google_place_id");
            entity.Property(e => e.LastActive)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime")
                .HasColumnName("last_active");
            entity.Property(e => e.Latitude)
                .HasComment("GPS latitude coordinate for distance calculations")
                .HasColumnName("latitude");
            entity.Property(e => e.LocationUpdatedDate)
                .HasColumnType("datetime")
                .HasColumnName("location_updated_date");
            entity.Property(e => e.Longitude)
                .HasComment("GPS longitude coordinate for distance calculations")
                .HasColumnName("longitude");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(20)
                .HasColumnName("phone_number");
            entity.Property(e => e.PlaceName)
                .HasMaxLength(200)
                .HasColumnName("place_name");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("active")
                .HasColumnName("status");
            entity.Property(e => e.WalletBalance)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("wallet_balance");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_users_roles");
        });

        modelBuilder.Entity<WalletTransaction>(entity =>
        {
            entity.HasKey(e => e.TransactionId).HasName("PK__wallet_t__85C600AF29459AA2");

            entity.HasIndex(e => e.ContractId, "ix_wallet_transactions_contract_id");

            entity.HasIndex(e => e.ParentId, "ix_wallet_transactions_parent_id");

            entity.HasIndex(e => e.Status, "ix_wallet_transactions_status");

            entity.Property(e => e.TransactionId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("transaction_id");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("amount");
            entity.Property(e => e.ContractId).HasColumnName("contract_id");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");
            entity.Property(e => e.ParentId).HasColumnName("parent_id");
            entity.Property(e => e.PaymentGatewayReference)
                .HasMaxLength(100)
                .HasColumnName("payment_gateway_reference");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(50)
                .HasColumnName("payment_method");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.TransactionDate)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime")
                .HasColumnName("transaction_date");
            entity.Property(e => e.TransactionType)
                .HasMaxLength(20)
                .HasColumnName("transaction_type");

            entity.HasOne(d => d.Parent).WithMany(p => p.WalletTransactions)
                .HasForeignKey(d => d.ParentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_wallet_transactions_parent");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
