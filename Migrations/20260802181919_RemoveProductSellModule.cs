using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Medshop.Migrations
{
    /// <inheritdoc />
    public partial class RemoveProductSellModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_sell_inventory");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Products_Id",
                table: "Products");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_Products_Id",
                table: "Products",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "product_sell_inventory",
                columns: table => new
                {
                    product_sell_inventory_id_pk = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    LoginId = table.Column<Guid>(type: "uuid", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PurchasePrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    SellingPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalPurchaseAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalSellingAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_sell_inventory", x => x.product_sell_inventory_id_pk);
                    table.ForeignKey(
                        name: "FK_product_sell_inventory_Products_product_id",
                        column: x => x.product_id,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_product_sell_inventory_CreatedAt",
                table: "product_sell_inventory",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_product_sell_inventory_Id",
                table: "product_sell_inventory",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_sell_inventory_LoginId",
                table: "product_sell_inventory",
                column: "LoginId");

            migrationBuilder.CreateIndex(
                name: "IX_product_sell_inventory_product_id",
                table: "product_sell_inventory",
                column: "product_id");
        }
    }
}
