using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FairShare.PaymentSimulator.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CardTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Last4Digits = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    Brand = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ExpiryMonth = table.Column<int>(type: "integer", nullable: false),
                    ExpiryYear = table.Column<int>(type: "integer", nullable: false),
                    HolderName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Outcome = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Charges",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CardTokenId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FailureCode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EventId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    WebhookAttempts = table.Column<int>(type: "integer", nullable: false),
                    NextWebhookAttemptAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    WebhookDeliveredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastWebhookError = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Charges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Charges_CardTokens_CardTokenId",
                        column: x => x.CardTokenId,
                        principalTable: "CardTokens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CardTokens_Token",
                table: "CardTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Charges_CardTokenId",
                table: "Charges",
                column: "CardTokenId");

            migrationBuilder.CreateIndex(
                name: "IX_Charges_IdempotencyKey",
                table: "Charges",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Charges_Status_CreatedAt",
                table: "Charges",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Charges_WebhookDeliveredAt_NextWebhookAttemptAt",
                table: "Charges",
                columns: new[] { "WebhookDeliveredAt", "NextWebhookAttemptAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Charges");

            migrationBuilder.DropTable(
                name: "CardTokens");
        }
    }
}
