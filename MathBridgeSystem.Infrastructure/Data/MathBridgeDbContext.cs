using MathBridge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MathBridge.Infrastructure.Data
{
    public class MathBridgeDbContext : DbContext
    {
        public MathBridgeDbContext(DbContextOptions<MathBridgeDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<WalletTransaction> WalletTransactions { get; set; }
        public DbSet<SePayTransaction> SePayTransactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // User configuration
            modelBuilder.Entity<User>()
                .HasKey(u => u.UserId);

            modelBuilder.Entity<User>()
                .Property(u => u.UserId)
                .HasColumnName("user_id")
                .HasDefaultValueSql("newid()");

            modelBuilder.Entity<User>()
                .Property(u => u.FullName)
                .HasColumnName("full_name")
                .HasMaxLength(255)
                .IsRequired();

            modelBuilder.Entity<User>()
                .Property(u => u.Email)
                .HasColumnName("email")
                .HasMaxLength(255)
                .IsRequired();

            modelBuilder.Entity<User>()
                .Property(u => u.PasswordHash)
                .HasColumnName("password_hash")
                .HasMaxLength(255)
                .IsRequired();

            modelBuilder.Entity<User>()
                .Property(u => u.PhoneNumber)
                .HasColumnName("phone_number")
                .HasMaxLength(20)
                .IsRequired();

            modelBuilder.Entity<User>()
                .Property(u => u.Gender)
                .HasColumnName("gender")
                .HasMaxLength(10)
                .IsRequired();

            modelBuilder.Entity<User>()
                .Property(u => u.WalletBalance)
                .HasColumnName("wallet_balance")
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0.00m);

            modelBuilder.Entity<User>()
                .Property(u => u.CreatedDate)
                .HasColumnName("created_date")
                .HasDefaultValueSql("getutcdate()");

            modelBuilder.Entity<User>()
                .Property(u => u.LastActive)
                .HasColumnName("last_active")
                .HasDefaultValueSql("getutcdate()");

            modelBuilder.Entity<User>()
                .Property(u => u.Status)
                .HasColumnName("status")
                .HasMaxLength(20)
                .HasDefaultValue("active");

            modelBuilder.Entity<User>()
                .Property(u => u.RoleId)
                .HasColumnName("role_id");

            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany()
                .HasForeignKey(u => u.RoleId)
                .HasConstraintName("fk_users_roles");

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique()
                .HasDatabaseName("ix_users_email");

            modelBuilder.Entity<User>()
                .HasIndex(u => u.RoleId)
                .HasDatabaseName("ix_users_role_id");

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Status)
                .HasDatabaseName("ix_users_status");

            // Role configuration
            modelBuilder.Entity<Role>()
                .HasKey(r => r.RoleId);

            modelBuilder.Entity<Role>()
                .Property(r => r.RoleId)
                .HasColumnName("role_id");

            modelBuilder.Entity<Role>()
                .Property(r => r.RoleName)
                .HasColumnName("role_name")
                .HasMaxLength(50)
                .IsRequired();

            modelBuilder.Entity<Role>()
                .Property(r => r.Description)
                .HasColumnName("description")
                .HasMaxLength(255);

            modelBuilder.Entity<Role>()
                .HasIndex(r => r.RoleName)
                .IsUnique()
                .HasDatabaseName("ix_roles_name");

            // WalletTransaction configuration
            modelBuilder.Entity<WalletTransaction>()
                .HasKey(wt => wt.TransactionId);

            modelBuilder.Entity<WalletTransaction>()
                .Property(wt => wt.TransactionId)
                .HasColumnName("transaction_id")
                .HasDefaultValueSql("newid()");

            modelBuilder.Entity<WalletTransaction>()
                .Property(wt => wt.ParentId)
                .HasColumnName("parent_id");

            modelBuilder.Entity<WalletTransaction>()
                .Property(wt => wt.ContractId)
                .HasColumnName("contract_id");

            modelBuilder.Entity<WalletTransaction>()
                .Property(wt => wt.Amount)
                .HasColumnName("amount")
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            modelBuilder.Entity<WalletTransaction>()
                .Property(wt => wt.TransactionType)
                .HasColumnName("transaction_type")
                .HasMaxLength(20)
                .IsRequired();

            modelBuilder.Entity<WalletTransaction>()
                .Property(wt => wt.Description)
                .HasColumnName("description")
                .HasMaxLength(500);

            modelBuilder.Entity<WalletTransaction>()
                .Property(wt => wt.TransactionDate)
                .HasColumnName("transaction_date")
                .HasDefaultValueSql("getutcdate()");

            modelBuilder.Entity<WalletTransaction>()
                .Property(wt => wt.Status)
                .HasColumnName("status")
                .HasMaxLength(20)
                .IsRequired();

            modelBuilder.Entity<WalletTransaction>()
                .Property(wt => wt.PaymentMethod)
                .HasColumnName("payment_method")
                .HasMaxLength(50);

            modelBuilder.Entity<WalletTransaction>()
                .Property(wt => wt.PaymentGatewayReference)
                .HasColumnName("payment_gateway_reference")
                .HasMaxLength(100);

            modelBuilder.Entity<WalletTransaction>()
                .HasOne(wt => wt.Parent)
                .WithMany(u => u.WalletTransactions)
                .HasForeignKey(wt => wt.ParentId)
                .HasConstraintName("fk_wallet_transactions_parent");

            modelBuilder.Entity<WalletTransaction>()
                .HasIndex(wt => wt.ParentId)
                .HasDatabaseName("ix_wallet_transactions_parent_id");

            modelBuilder.Entity<WalletTransaction>()
                .HasIndex(wt => wt.ContractId)
                .HasDatabaseName("ix_wallet_transactions_contract_id");

            modelBuilder.Entity<WalletTransaction>()
                .HasIndex(wt => wt.Status)
                .HasDatabaseName("ix_wallet_transactions_status");

            // SePayTransaction configuration
            modelBuilder.Entity<SePayTransaction>()
                .HasKey(st => st.SePayTransactionId);

            modelBuilder.Entity<SePayTransaction>()
                .Property(st => st.SePayTransactionId)
                .HasColumnName("sepay_transaction_id")
                .HasDefaultValueSql("newid()");

            modelBuilder.Entity<SePayTransaction>()
                .Property(st => st.WalletTransactionId)
                .HasColumnName("wallet_transaction_id")
                .IsRequired();

            modelBuilder.Entity<SePayTransaction>()
                .Property(st => st.Gateway)
                .HasColumnName("gateway")
                .HasMaxLength(50)
                .IsRequired();

            modelBuilder.Entity<SePayTransaction>()
                .Property(st => st.TransactionDate)
                .HasColumnName("transaction_date")
                .IsRequired();

            modelBuilder.Entity<SePayTransaction>()
                .Property(st => st.AccountNumber)
                .HasColumnName("account_number")
                .HasMaxLength(50)
                .IsRequired();

            modelBuilder.Entity<SePayTransaction>()
                .Property(st => st.SubAccount)
                .HasColumnName("sub_account")
                .HasMaxLength(50);

            modelBuilder.Entity<SePayTransaction>()
                .Property(st => st.TransferType)
                .HasColumnName("transfer_type")
                .HasMaxLength(10)
                .IsRequired();

            modelBuilder.Entity<SePayTransaction>()
                .Property(st => st.TransferAmount)
                .HasColumnName("transfer_amount")
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            modelBuilder.Entity<SePayTransaction>()
                .Property(st => st.Accumulated)
                .HasColumnName("accumulated")
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            modelBuilder.Entity<SePayTransaction>()
                .Property(st => st.Code)
                .HasColumnName("code")
                .HasMaxLength(100)
                .IsRequired();

            modelBuilder.Entity<SePayTransaction>()
                .Property(st => st.Content)
                .HasColumnName("content")
                .HasMaxLength(500)
                .IsRequired();

            modelBuilder.Entity<SePayTransaction>()
                .Property(st => st.ReferenceNumber)
                .HasColumnName("reference_number")
                .HasMaxLength(100)
                .IsRequired();

            modelBuilder.Entity<SePayTransaction>()
                .Property(st => st.Description)
                .HasColumnName("description")
                .HasMaxLength(1000);

            modelBuilder.Entity<SePayTransaction>()
                .Property(st => st.OrderReference)
                .HasColumnName("order_reference")
                .HasMaxLength(50);

            modelBuilder.Entity<SePayTransaction>()
                .Property(st => st.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("getutcdate()");

            // SePayTransaction relationships
            modelBuilder.Entity<SePayTransaction>()
                .HasOne(st => st.WalletTransaction)
                .WithMany()
                .HasForeignKey(st => st.WalletTransactionId)
                .HasConstraintName("fk_sepay_transactions_wallet_transaction")
                .OnDelete(DeleteBehavior.Restrict);

            // SePayTransaction indexes
            modelBuilder.Entity<SePayTransaction>()
                .HasIndex(st => st.WalletTransactionId)
                .HasDatabaseName("ix_sepay_transactions_wallet_transaction_id");

            modelBuilder.Entity<SePayTransaction>()
                .HasIndex(st => st.Code)
                .IsUnique()
                .HasDatabaseName("ix_sepay_transactions_code");

            modelBuilder.Entity<SePayTransaction>()
                .HasIndex(st => st.OrderReference)
                .HasDatabaseName("ix_sepay_transactions_order_reference");

            modelBuilder.Entity<SePayTransaction>()
                .HasIndex(st => st.TransactionDate)
                .HasDatabaseName("ix_sepay_transactions_date");
        }
    }
}