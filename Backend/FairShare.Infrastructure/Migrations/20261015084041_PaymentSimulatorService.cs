using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FairShare.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PaymentSimulatorService : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "Payments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderChargeId",
                table: "Payments",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                table: "Payments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Payments",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateTable(
                name: "ProcessedWebhookEvent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ChargeId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedWebhookEvent", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ProviderChargeId",
                table: "Payments",
                column: "ProviderChargeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedWebhookEvent_EventId",
                table: "ProcessedWebhookEvent",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedWebhookEvent_PaymentId",
                table: "ProcessedWebhookEvent",
                column: "PaymentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProcessedWebhookEvent");

            migrationBuilder.DropIndex(
                name: "IX_Payments_ProviderChargeId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ProviderChargeId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Payments");
        }
    }
}
