using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Medshop.Migrations
{
    /// <inheritdoc />
    public partial class AddProductSellModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_Products_Id",
                table: "Products",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "ProductSellInventories",
                columns: table => new
                {
                    product_sell_inventory_id_pk = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LoginId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    PurchasePrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    SellingPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalPurchaseAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalSellingAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductSellInventories", x => x.product_sell_inventory_id_pk);
                    table.ForeignKey(
                        name: "FK_ProductSellInventories_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductSellInventories_CreatedAt",
                table: "ProductSellInventories",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSellInventories_Id",
                table: "ProductSellInventories",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductSellInventories_LoginId",
                table: "ProductSellInventories",
                column: "LoginId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSellInventories_ProductId",
                table: "ProductSellInventories",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductSellInventories");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Products_Id",
                table: "Products");
        }
    }
}
