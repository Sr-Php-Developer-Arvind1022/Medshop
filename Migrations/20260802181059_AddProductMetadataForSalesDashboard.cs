using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medshop.Migrations
{
    /// <inheritdoc />
    public partial class AddProductMetadataForSalesDashboard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "batch_no",
                table: "Products",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "expiry_date",
                table: "Products",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "manufacturer",
                table: "Products",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "batch_no",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "expiry_date",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "manufacturer",
                table: "Products");
        }
    }
}
