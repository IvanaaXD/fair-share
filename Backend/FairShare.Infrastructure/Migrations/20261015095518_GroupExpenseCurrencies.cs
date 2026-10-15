using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FairShare.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class GroupExpenseCurrencies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRate",
                table: "GroupExpenses",
                type: "numeric(18,8)",
                precision: 18,
                scale: 8,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExchangeRateDate",
                table: "GroupExpenses",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalAmount",
                table: "GroupExpenses",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalCurrency",
                table: "GroupExpenses",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExchangeRate",
                table: "GroupExpenses");

            migrationBuilder.DropColumn(
                name: "ExchangeRateDate",
                table: "GroupExpenses");

            migrationBuilder.DropColumn(
                name: "OriginalAmount",
                table: "GroupExpenses");

            migrationBuilder.DropColumn(
                name: "OriginalCurrency",
                table: "GroupExpenses");
        }
    }
}
