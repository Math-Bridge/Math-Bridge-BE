using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MathBridgeSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSePayTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    role_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    role_name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.role_id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "newid()"),
                    full_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    phone_number = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    gender = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    wallet_balance = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0.00m),
                    created_date = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getutcdate()"),
                    last_active = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getutcdate()"),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "active"),
                    role_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.user_id);
                    table.ForeignKey(
                        name: "fk_users_roles",
                        column: x => x.role_id,
                        principalTable: "Roles",
                        principalColumn: "role_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WalletTransactions",
                columns: table => new
                {
                    transaction_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "newid()"),
                    parent_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    contract_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    transaction_type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    transaction_date = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getutcdate()"),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    payment_method = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    payment_gateway_reference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletTransactions", x => x.transaction_id);
                    table.ForeignKey(
                        name: "fk_wallet_transactions_parent",
                        column: x => x.parent_id,
                        principalTable: "Users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SePayTransactions",
                columns: table => new
                {
                    sepay_transaction_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "newid()"),
                    wallet_transaction_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    gateway = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    transaction_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    account_number = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    sub_account = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    transfer_type = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    transfer_amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    accumulated = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    content = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    reference_number = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    order_reference = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getutcdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SePayTransactions", x => x.sepay_transaction_id);
                    table.ForeignKey(
                        name: "fk_sepay_transactions_wallet_transaction",
                        column: x => x.wallet_transaction_id,
                        principalTable: "WalletTransactions",
                        principalColumn: "transaction_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_roles_name",
                table: "Roles",
                column: "role_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sepay_transactions_code",
                table: "SePayTransactions",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sepay_transactions_date",
                table: "SePayTransactions",
                column: "transaction_date");

            migrationBuilder.CreateIndex(
                name: "ix_sepay_transactions_order_reference",
                table: "SePayTransactions",
                column: "order_reference");

            migrationBuilder.CreateIndex(
                name: "ix_sepay_transactions_wallet_transaction_id",
                table: "SePayTransactions",
                column: "wallet_transaction_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "Users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_role_id",
                table: "Users",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_status",
                table: "Users",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_wallet_transactions_contract_id",
                table: "WalletTransactions",
                column: "contract_id");

            migrationBuilder.CreateIndex(
                name: "ix_wallet_transactions_parent_id",
                table: "WalletTransactions",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "ix_wallet_transactions_status",
                table: "WalletTransactions",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SePayTransactions");

            migrationBuilder.DropTable(
                name: "WalletTransactions");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
