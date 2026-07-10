using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DukkanPilot.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTableServiceMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_BusinessId",
                table: "Orders");

            migrationBuilder.AddColumn<int>(
                name: "BusinessTableId",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServiceType",
                table: "Orders",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TableLabelSnapshot",
                table: "Orders",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BusinessTables",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusinessId = table.Column<int>(type: "int", nullable: false),
                    TableLabel = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    PublicCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessTables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessTables_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_BusinessId_BusinessTableId",
                table: "Orders",
                columns: new[] { "BusinessId", "BusinessTableId" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_BusinessId_CreatedAt",
                table: "Orders",
                columns: new[] { "BusinessId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_BusinessId_ServiceType",
                table: "Orders",
                columns: new[] { "BusinessId", "ServiceType" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_BusinessTableId",
                table: "Orders",
                column: "BusinessTableId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessTables_BusinessId_IsActive_DisplayOrder",
                table: "BusinessTables",
                columns: new[] { "BusinessId", "IsActive", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessTables_BusinessId_PublicCode",
                table: "BusinessTables",
                columns: new[] { "BusinessId", "PublicCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessTables_BusinessId_TableLabel",
                table: "BusinessTables",
                columns: new[] { "BusinessId", "TableLabel" });

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_BusinessTables_BusinessTableId",
                table: "Orders",
                column: "BusinessTableId",
                principalTable: "BusinessTables",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_BusinessTables_BusinessTableId",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "BusinessTables");

            migrationBuilder.DropIndex(
                name: "IX_Orders_BusinessId_BusinessTableId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_BusinessId_CreatedAt",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_BusinessId_ServiceType",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_BusinessTableId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "BusinessTableId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ServiceType",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TableLabelSnapshot",
                table: "Orders");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_BusinessId",
                table: "Orders",
                column: "BusinessId");
        }
    }
}
