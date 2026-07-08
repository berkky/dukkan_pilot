using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DukkanPilot.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCampaignDiscountFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DiscountType",
                table: "Campaigns",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountValue",
                table: "Campaigns",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsAutoApply",
                table: "Campaigns",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPublicVisible",
                table: "Campaigns",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaximumDiscountAmount",
                table: "Campaigns",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinimumOrderAmount",
                table: "Campaigns",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "Campaigns",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscountType",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "DiscountValue",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "IsAutoApply",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "IsPublicVisible",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "MaximumDiscountAmount",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "MinimumOrderAmount",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Campaigns");
        }
    }
}
