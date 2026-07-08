using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DukkanPilot.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SalesRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BusinessId = table.Column<int>(type: "int", nullable: true),
                    RequestedPlanId = table.Column<int>(type: "int", nullable: true),
                    CurrentPlanId = table.Column<int>(type: "int", nullable: true),
                    Source = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    RequestType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ContactName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    BusinessName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    RequestedPlanName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CurrentPlanName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AdminNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    LastContactedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    PrivacyNoticeAcknowledged = table.Column<bool>(type: "bit", nullable: false),
                    KvkkNoticeAcknowledged = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesRequests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalesRequests_BusinessId_CreatedAtUtc",
                table: "SalesRequests",
                columns: new[] { "BusinessId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesRequests_Email_CreatedAtUtc",
                table: "SalesRequests",
                columns: new[] { "Email", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesRequests_Priority",
                table: "SalesRequests",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_SalesRequests_RequestedPlanId",
                table: "SalesRequests",
                column: "RequestedPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesRequests_RequestType",
                table: "SalesRequests",
                column: "RequestType");

            migrationBuilder.CreateIndex(
                name: "IX_SalesRequests_Source",
                table: "SalesRequests",
                column: "Source");

            migrationBuilder.CreateIndex(
                name: "IX_SalesRequests_Status_CreatedAtUtc",
                table: "SalesRequests",
                columns: new[] { "Status", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SalesRequests");
        }
    }
}
