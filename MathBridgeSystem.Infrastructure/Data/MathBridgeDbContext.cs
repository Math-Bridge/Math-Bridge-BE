using MathBridge.Infrastructure.Data;
using MathBridge.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    // DbSets
    public virtual DbSet<Role> Roles { get; set; }
    public virtual DbSet<School> Schools { get; set; }
    public virtual DbSet<SePayTransaction> SePayTransactions { get; set; }
    public virtual DbSet<User> Users { get; set; }
    public virtual DbSet<WalletTransaction> WalletTransactions { get; set; }
    public virtual DbSet<Child> Children { get; set; }
    public virtual DbSet<MathProgram> MathPrograms { get; set; }
    public virtual DbSet<PaymentPackage> PaymentPackages { get; set; }
    public virtual DbSet<Contract> Contracts { get; set; }
    public virtual DbSet<Center> Centers { get; set; }
    public virtual DbSet<TutorCenter> TutorCenters { get; set; }
    public virtual DbSet<TutorVerification> TutorVerifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // === ROLE ENTITY ===
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

            // Seed default roles
            modelBuilder.Entity<Role>().HasData(
                new Role { RoleId = 1, RoleName = "admin", Description = "System administrator" },
                new Role { RoleId = 2, RoleName = "tutor", Description = "Math tutor" },
                new Role { RoleId = 3, RoleName = "parent", Description = "Parent/Guardian" },
                new Role { RoleId = 4, RoleName = "staff", Description = "Support staff" }
            );
        });

        // === SCHOOL ENTITY ===
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

        // === SEPAY TRANSACTION ENTITY ===
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

        // === USER ENTITY ===
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

            // One-to-one relationship with TutorVerification
            entity.HasOne(u => u.TutorVerification)
                .WithOne(tv => tv.User)
                .HasForeignKey<TutorVerification>(tv => tv.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);

            // One-to-many relationship with TutorCenters - SỬA DeleteBehavior thành Restrict để tránh circular reference
            entity.HasMany(u => u.TutorCenters)
                .WithOne(tc => tc.Tutor)
                .HasForeignKey(tc => tc.TutorId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_tutor_centers_tutor");

            // One-to-many relationship with Children (as parent)
            entity.HasMany(u => u.Children)
                .WithOne(c => c.Parent)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_children_users");

            // One-to-many relationship with WalletTransactions
            entity.HasMany(u => u.WalletTransactions)
                .WithOne(wt => wt.Parent)
                .HasForeignKey(wt => wt.ParentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_wallet_transactions_parent");

            // One-to-many relationships with Contracts
            entity.HasMany(u => u.ParentContracts)
                .WithOne(c => c.Parent)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_contracts_parent");

            entity.HasMany(u => u.MainTutorContracts)
                .WithOne(c => c.MainTutor)
                .HasForeignKey(c => c.MainTutorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_contracts_main_tutor");

            entity.HasMany(u => u.SubstituteTutor1Contracts)
                .WithOne(c => c.SubstituteTutor1)
                .HasForeignKey(c => c.SubstituteTutor1Id)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_contracts_substitute_tutor1");

            entity.HasMany(u => u.SubstituteTutor2Contracts)
                .WithOne(c => c.SubstituteTutor2)
                .HasForeignKey(c => c.SubstituteTutor2Id)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_contracts_substitute_tutor2");
        });

        // === WALLET TRANSACTION ENTITY ===
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

            entity.HasOne(d => d.Contract)
                .WithMany(c => c.WalletTransactions)
                .HasForeignKey(d => d.ContractId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_wallet_transactions_contract");
        });

        // === CHILD ENTITY ===
        modelBuilder.Entity<Child>(entity =>
        {
            entity.HasKey(e => e.ChildId).HasName("PK__children__27CA6CF476FA07F4");

            entity.ToTable("children");

            entity.Property(e => e.ChildId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("child_id");

            entity.Property(e => e.FullName)
                .HasMaxLength(255)
                .IsRequired()
                .HasColumnName("full_name");

            entity.Property(e => e.Grade)
                .HasMaxLength(50)
                .IsRequired()
                .HasColumnName("grade");

            entity.Property(e => e.DateOfBirth)
                .HasColumnType("date")
                .HasColumnName("date_of_birth");

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime")
                .IsRequired()
                .HasColumnName("created_date");

            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("active")
                .IsRequired()
                .HasColumnName("status");

            // FOREIGN KEY PROPERTIES 
            entity.Property(e => e.ParentId)
                .HasColumnName("parent_id");
            entity.Property(e => e.SchoolId)
                .HasColumnName("school_id");
            entity.Property(e => e.CenterId)
                .HasColumnName("center_id");

            // FOREIGN KEY RELATIONSHIPS
            entity.HasOne(d => d.Parent)
                .WithMany(p => p.Children)
                .HasForeignKey(d => d.ParentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_children_users");

            entity.HasOne(d => d.School)
                .WithMany()
                .HasForeignKey(d => d.SchoolId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_children_schools");

            entity.HasOne(d => d.Center)
                .WithMany(p => p.Children)
                .HasForeignKey(d => d.CenterId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_children_center");

            entity.HasIndex(e => e.ParentId, "ix_children_parent_id");
            entity.HasIndex(e => e.SchoolId, "ix_children_school_id");
            entity.HasIndex(e => e.CenterId, "ix_children_center_id");
            entity.HasIndex(e => e.Status, "ix_children_status");

            // CHECK constraint for grade
            entity.HasCheckConstraint("CK_children_grade",
                "[grade]='grade 12' OR [grade]='grade 11' OR [grade]='grade 10' OR [grade]='grade 9'");

            // CHECK constraint for status
            entity.HasCheckConstraint("CK_children_status",
                "[status]='active' OR [status]='deleted'");
        });

        // === MATH PROGRAM ENTITY ===
        modelBuilder.Entity<MathProgram>(entity =>
        {
            entity.HasKey(e => e.ProgramId).HasName("PK__math_pro__760965CC610C41A2");

            entity.ToTable("math_programs");

            entity.Property(e => e.ProgramId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("program_id");

            entity.Property(e => e.ProgramName)
                .HasMaxLength(100)
                .IsRequired()
                .HasColumnName("program_name");

            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");

            entity.HasIndex(e => e.ProgramName, "ix_math_programs_program_name");

            // CHECK constraint for program name
            entity.HasCheckConstraint("CK_math_programs_program_name",
                "[program_name]='vietnam advanced math' OR " +
                "[program_name]='sat math' OR " +
                "[program_name]='ap calculus' OR " +
                "[program_name]='ib sl' OR " +
                "[program_name]='ib hl' OR " +
                "[program_name]='as/a level' OR " +
                "[program_name]='cambridge igcse'");
        });

        // === PAYMENT PACKAGE ENTITY ===
        modelBuilder.Entity<PaymentPackage>(entity =>
        {
            entity.HasKey(e => e.PackageId).HasName("PK__payment_packages__760965CC610C41A2");

            entity.ToTable("payment_packages");

            entity.Property(e => e.PackageId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("package_id");

            entity.Property(e => e.PackageName)
                .HasMaxLength(100)
                .IsRequired()
                .HasColumnName("package_name");

            entity.Property(e => e.Grade)
                .HasMaxLength(50)
                .IsRequired()
                .HasColumnName("grade");

            entity.Property(e => e.Price)
                .HasColumnType("decimal(18, 2)")
                .IsRequired()
                .HasColumnName("price");

            entity.Property(e => e.SessionCount)
                .IsRequired()
                .HasColumnName("session_count");

            entity.Property(e => e.SessionsPerWeek)
                .IsRequired()
                .HasColumnName("sessions_per_week");

            entity.Property(e => e.MaxReschedule)
                .HasDefaultValueSql("((0))")
                .IsRequired()
                .HasColumnName("max_reschedule");

            entity.Property(e => e.DurationDays)
                .IsRequired()
                .HasColumnName("duration_days");

            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime")
                .IsRequired()
                .HasColumnName("created_date");

            entity.Property(e => e.UpdatedDate)
                .HasColumnType("datetime")
                .HasColumnName("updated_date");

            // FOREIGN KEY PROPERTY 
            entity.Property(e => e.ProgramId)
                .HasColumnName("program_id");

            // FOREIGN KEY RELATIONSHIP
            entity.HasOne(d => d.Program)
                .WithMany(p => p.PaymentPackages)
                .HasForeignKey(d => d.ProgramId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_payment_packages_programs");

            entity.HasIndex(e => e.ProgramId, "ix_payment_packages_program_id");

            // CHECK constraint for grade
            entity.HasCheckConstraint("CK_payment_packages_grade",
                "[grade]='grade 12' OR [grade]='grade 11' OR [grade]='grade 10' OR [grade]='grade 9'");
        });

        // === CONTRACT ENTITY ===
        modelBuilder.Entity<Contract>(entity =>
        {
            entity.HasKey(e => e.ContractId).HasName("PK__contracts__760965CC610C41A2");

            entity.ToTable("contracts");

            entity.Property(e => e.ContractId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("contract_id");

            entity.Property(e => e.StartDate)
                .HasColumnType("date")
                .IsRequired()
                .HasColumnName("start_date");

            entity.Property(e => e.EndDate)
                .HasColumnType("date")
                .IsRequired()
                .HasColumnName("end_date");

            entity.Property(e => e.TimeSlot)
                .HasMaxLength(50)
                .IsRequired()
                .HasColumnName("time_slot");

            entity.Property(e => e.IsOnline)
                .IsRequired()
                .HasDefaultValue(false)
                .HasColumnName("is_online");

            entity.Property(e => e.OfflineAddress)
                .HasMaxLength(500)
                .HasColumnName("offline_address");

            entity.Property(e => e.OfflineLatitude)
                .HasColumnType("decimal(9, 6)")
                .HasColumnName("offline_latitude");

            entity.Property(e => e.OfflineLongitude)
                .HasColumnType("decimal(9, 6)")
                .HasColumnName("offline_longitude");

            entity.Property(e => e.VideoCallPlatform)
                .HasMaxLength(50)
                .HasColumnName("video_call_platform");

            entity.Property(e => e.MaxDistanceKm)
                .HasColumnType("decimal(5, 2)")
                .HasDefaultValue(7.00m)
                .IsRequired()
                .HasColumnName("max_distance_km");

            entity.Property(e => e.RescheduleCount)
                .HasDefaultValue(0)
                .IsRequired()
                .HasColumnName("reschedule_count");

            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("pending")
                .IsRequired()
                .HasColumnName("status");

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime")
                .IsRequired()
                .HasColumnName("created_date");

            entity.Property(e => e.UpdatedDate)
                .HasColumnType("datetime")
                .HasColumnName("updated_date");

            // FOREIGN KEY PROPERTIES
            entity.Property(e => e.ParentId)
                .HasColumnName("parent_id");
            entity.Property(e => e.ChildId)
                .HasColumnName("child_id");
            entity.Property(e => e.CenterId)
                .HasColumnName("center_id");
            entity.Property(e => e.PackageId)
                .HasColumnName("package_id");
            entity.Property(e => e.MainTutorId)
                .HasColumnName("main_tutor_id");
            entity.Property(e => e.SubstituteTutor1Id)
                .HasColumnName("substitute_tutor1_id");
            entity.Property(e => e.SubstituteTutor2Id)
                .HasColumnName("substitute_tutor2_id");

            // CHECK CONSTRAINT FOR TUTOR DISTINCT
            entity.HasCheckConstraint("chk_tutor_distinct",
                "[main_tutor_id] <> [substitute_tutor1_id] AND " +
                "[main_tutor_id] <> [substitute_tutor2_id] AND " +
                "([substitute_tutor1_id] <> [substitute_tutor2_id] OR [substitute_tutor1_id] IS NULL OR [substitute_tutor2_id] IS NULL)");

            // CHECK constraint for status
            entity.HasCheckConstraint("CK_contracts_status",
                "[status]='cancelled' OR [status]='completed' OR [status]='active' OR [status]='pending'");

            // CHECK constraint for time_slot
            entity.HasCheckConstraint("CK_contracts_time_slot",
                "[time_slot]='sun_evening' OR [time_slot]='sat_evening' OR [time_slot]='fri_evening' OR " +
                "[time_slot]='thu_evening' OR [time_slot]='wed_evening' OR [time_slot]='tue_evening' OR [time_slot]='mon_evening'");

            // CHECK constraint for video_call_platform
            entity.HasCheckConstraint("CK_contracts_video_call_platform",
                "[video_call_platform] IS NULL OR [video_call_platform]='google_meet' OR [video_call_platform]='zoom'");

            // FOREIGN KEY RELATIONSHIPS
            entity.HasOne(d => d.Child)
                .WithMany(p => p.Contracts)
                .HasForeignKey(d => d.ChildId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_contracts_child");

            entity.HasOne(d => d.MainTutor)
                .WithMany(u => u.MainTutorContracts)
                .HasForeignKey(d => d.MainTutorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_contracts_main_tutor");

            entity.HasOne(d => d.Parent)
                .WithMany(u => u.ParentContracts)
                .HasForeignKey(d => d.ParentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_contracts_parent");

            entity.HasOne(d => d.Package)
                .WithMany(p => p.Contracts)
                .HasForeignKey(d => d.PackageId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_contracts_package");

            entity.HasOne(d => d.Center)
                .WithMany(p => p.Contracts)
                .HasForeignKey(d => d.CenterId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_contracts_center");

            entity.HasOne(d => d.SubstituteTutor1)
                .WithMany(u => u.SubstituteTutor1Contracts)
                .HasForeignKey(d => d.SubstituteTutor1Id)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_contracts_substitute_tutor1");

            entity.HasOne(d => d.SubstituteTutor2)
                .WithMany(u => u.SubstituteTutor2Contracts)
                .HasForeignKey(d => d.SubstituteTutor2Id)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_contracts_substitute_tutor2");

            entity.HasIndex(e => e.ChildId, "ix_contracts_child_id");
            entity.HasIndex(e => e.MainTutorId, "ix_contracts_main_tutor_id");
            entity.HasIndex(e => e.PackageId, "ix_contracts_package_id");
            entity.HasIndex(e => e.ParentId, "ix_contracts_parent_id");
            entity.HasIndex(e => e.CenterId, "ix_contracts_center_id");
        });

        // === CENTER ENTITY ===
        modelBuilder.Entity<Center>(entity =>
        {
            entity.HasKey(e => e.CenterId).HasName("PK__centers__27CA6CF476FA07F4");

            entity.ToTable("centers");

            entity.HasIndex(e => e.City, "IX_Centers_City");
            entity.HasIndex(e => new { e.Latitude, e.Longitude }, "IX_Centers_Location");
            entity.HasIndex(e => e.Name, "ix_centers_name");

            entity.Property(e => e.CenterId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("center_id");

            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .IsRequired()
                .HasColumnName("name");

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
                .IsRequired()
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

            entity.Property(e => e.PlaceName)
                .HasMaxLength(255)
                .HasColumnName("place_name");

            entity.Property(e => e.TutorCount)
                .HasDefaultValue(0)
                .HasColumnName("tutor_count");

            entity.Property(e => e.UpdatedDate)
                .HasColumnType("datetime")
                .HasColumnName("updated_date");

            // Relationships
            entity.HasMany(c => c.Children)
                .WithOne(child => child.Center)
                .HasForeignKey(child => child.CenterId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_children_center");

            entity.HasMany(c => c.Contracts)
                .WithOne(contract => contract.Center)
                .HasForeignKey(contract => contract.CenterId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_contracts_center");

            entity.HasMany(c => c.TutorCenters)
                .WithOne(tc => tc.Center)
                .HasForeignKey(tc => tc.CenterId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_tutor_centers_center");
        });

        // === TUTOR_CENTER ENTITY 
        modelBuilder.Entity<TutorCenter>(entity =>
        {
            entity.HasKey(e => e.TutorCenterId).HasName("PK__tutor_centers__760965CC610C41A2");

            entity.ToTable("tutor_centers");

            // Unique constraint cho combination tutor_id và center_id
            entity.HasIndex(e => new { e.TutorId, e.CenterId }, "uq_tutor_center").IsUnique();
            entity.HasIndex(e => e.CenterId, "ix_tutor_centers_center_id");
            entity.HasIndex(e => e.TutorId, "ix_tutor_centers_tutor_id");

            // Property mappings - QUAN TRỌNG: THÊM MAPPING CHO TUTORID VÀ CENTERID
            entity.Property(e => e.TutorCenterId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("tutor_center_id");

            entity.Property(e => e.TutorId)
                .HasColumnName("tutor_id")
                .IsRequired();

            entity.Property(e => e.CenterId)
                .HasColumnName("center_id")
                .IsRequired();

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime")
                .IsRequired()
                .HasColumnName("created_date");

            // FOREIGN KEY RELATIONSHIPS
            entity.HasOne(d => d.Tutor)
                .WithMany(u => u.TutorCenters)
                .HasForeignKey(d => d.TutorId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_tutor_centers_tutor");

            entity.HasOne(d => d.Center)
                .WithMany(p => p.TutorCenters)
                .HasForeignKey(d => d.CenterId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_tutor_centers_center");
        });

        // === TUTOR VERIFICATION ENTITY ===
        modelBuilder.Entity<TutorVerification>(entity =>
        {
            entity.HasKey(e => e.VerificationId).HasName("PK__tutor_verifications__760965CC610C41A2");

            entity.ToTable("tutor_verifications");

            entity.HasIndex(e => e.VerificationStatus, "ix_tutor_verifications_verification_status");

            entity.Property(e => e.VerificationId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("verification_id");

            entity.Property(e => e.UserId)
                .HasColumnName("user_id");

            entity.Property(e => e.University)
                .HasMaxLength(255)
                .IsRequired()
                .HasColumnName("university");

            entity.Property(e => e.Major)
                .HasMaxLength(255)
                .IsRequired()
                .HasColumnName("major");

            entity.Property(e => e.HourlyRate)
                .HasColumnType("decimal(18, 2)")
                .HasDefaultValue(0.00m)
                .HasColumnName("hourly_rate");

            entity.Property(e => e.Bio)
                .HasColumnType("nvarchar(max)")
                .HasColumnName("bio");

            entity.Property(e => e.VerificationStatus)
                .HasMaxLength(20)
                .HasDefaultValue("pending")
                .HasColumnName("verification_status");

            entity.Property(e => e.VerificationDate)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime")
                .HasColumnName("verification_date");

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime")
                .IsRequired()
                .HasColumnName("created_date");

            // FOREIGN KEY RELATIONSHIP - One-to-One with User
            entity.HasOne(d => d.User)
                .WithOne(u => u.TutorVerification)
                .HasForeignKey<TutorVerification>(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_tutor_verifications_users");

            // CHECK constraint for verification_status
            entity.HasCheckConstraint("CK_tutor_verifications_status",
                "[verification_status]='rejected' OR [verification_status]='approved' OR [verification_status]='pending'");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}