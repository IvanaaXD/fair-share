using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FairShare.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class GroupExpenseReceiptImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BankAccountNumber",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MustConfirmEmail",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "SettlementTransactions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptImageUrl",
                table: "GroupExpenses",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BankAccountNumber",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MustConfirmEmail",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "SettlementTransactions");

            migrationBuilder.DropColumn(
                name: "ReceiptImageUrl",
                table: "GroupExpenses");
        }
    }
}
