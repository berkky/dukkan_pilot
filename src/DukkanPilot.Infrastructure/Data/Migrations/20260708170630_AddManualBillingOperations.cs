using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DukkanPilot.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddManualBillingOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BillingInvoices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BusinessId = table.Column<int>(type: "int", nullable: false),
                    SubscriptionPlanId = table.Column<int>(type: "int", nullable: true),
                    BusinessSubscriptionId = table.Column<int>(type: "int", nullable: true),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    IssueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PaymentStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    RelatedSalesRequestId = table.Column<int>(type: "int", nullable: true),
                    AdminNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    BusinessVisibleNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsOfficialInvoice = table.Column<bool>(type: "bit", nullable: false),
                    OfficialInvoiceReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingInvoices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BillingPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BusinessId = table.Column<int>(type: "int", nullable: false),
                    BillingInvoiceId = table.Column<int>(type: "int", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Method = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PayerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AdminNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    BusinessVisibleNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RecordedByUserId = table.Column<int>(type: "int", nullable: true),
                    RecordedByUserEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingPayments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BillingInvoices_BusinessId_CreatedAtUtc",
                table: "BillingInvoices",
                columns: new[] { "BusinessId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BillingInvoices_BusinessId_PaymentStatus",
                table: "BillingInvoices",
                columns: new[] { "BusinessId", "PaymentStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_BillingInvoices_BusinessId_Status",
                table: "BillingInvoices",
                columns: new[] { "BusinessId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_BillingInvoices_DueDate",
                table: "BillingInvoices",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_BillingInvoices_InvoiceNumber",
                table: "BillingInvoices",
                column: "InvoiceNumber");

            migrationBuilder.CreateIndex(
                name: "IX_BillingInvoices_RelatedSalesRequestId",
                table: "BillingInvoices",
                column: "RelatedSalesRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingPayments_BillingInvoiceId",
                table: "BillingPayments",
                column: "BillingInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingPayments_BusinessId_CreatedAtUtc",
                table: "BillingPayments",
                columns: new[] { "BusinessId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BillingPayments_Method",
                table: "BillingPayments",
                column: "Method");

            migrationBuilder.CreateIndex(
                name: "IX_BillingPayments_PaymentDate",
                table: "BillingPayments",
                column: "PaymentDate");

            migrationBuilder.CreateIndex(
                name: "IX_BillingPayments_Status",
                table: "BillingPayments",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BillingInvoices");

            migrationBuilder.DropTable(
                name: "BillingPayments");
        }
    }
}
