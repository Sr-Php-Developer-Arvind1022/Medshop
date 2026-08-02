using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medshop.Migrations
{
    /// <inheritdoc />
    public partial class AlignProductSellInventoryNaming : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductSellInventories_Products_ProductId",
                table: "ProductSellInventories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProductSellInventories",
                table: "ProductSellInventories");

            migrationBuilder.RenameTable(
                name: "ProductSellInventories",
                newName: "product_sell_inventory");

            migrationBuilder.RenameColumn(
                name: "ProductId",
                table: "product_sell_inventory",
                newName: "product_id");

            migrationBuilder.RenameIndex(
                name: "IX_ProductSellInventories_ProductId",
                table: "product_sell_inventory",
                newName: "IX_product_sell_inventory_product_id");

            migrationBuilder.RenameIndex(
                name: "IX_ProductSellInventories_LoginId",
                table: "product_sell_inventory",
                newName: "IX_product_sell_inventory_LoginId");

            migrationBuilder.RenameIndex(
                name: "IX_ProductSellInventories_Id",
                table: "product_sell_inventory",
                newName: "IX_product_sell_inventory_Id");

            migrationBuilder.RenameIndex(
                name: "IX_ProductSellInventories_CreatedAt",
                table: "product_sell_inventory",
                newName: "IX_product_sell_inventory_CreatedAt");

            migrationBuilder.AddPrimaryKey(
                name: "PK_product_sell_inventory",
                table: "product_sell_inventory",
                column: "product_sell_inventory_id_pk");

            migrationBuilder.AddForeignKey(
                name: "FK_product_sell_inventory_Products_product_id",
                table: "product_sell_inventory",
                column: "product_id",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_product_sell_inventory_Products_product_id",
                table: "product_sell_inventory");

            migrationBuilder.DropPrimaryKey(
                name: "PK_product_sell_inventory",
                table: "product_sell_inventory");

            migrationBuilder.RenameTable(
                name: "product_sell_inventory",
                newName: "ProductSellInventories");

            migrationBuilder.RenameColumn(
                name: "product_id",
                table: "ProductSellInventories",
                newName: "ProductId");

            migrationBuilder.RenameIndex(
                name: "IX_product_sell_inventory_product_id",
                table: "ProductSellInventories",
                newName: "IX_ProductSellInventories_ProductId");

            migrationBuilder.RenameIndex(
                name: "IX_product_sell_inventory_LoginId",
                table: "ProductSellInventories",
                newName: "IX_ProductSellInventories_LoginId");

            migrationBuilder.RenameIndex(
                name: "IX_product_sell_inventory_Id",
                table: "ProductSellInventories",
                newName: "IX_ProductSellInventories_Id");

            migrationBuilder.RenameIndex(
                name: "IX_product_sell_inventory_CreatedAt",
                table: "ProductSellInventories",
                newName: "IX_ProductSellInventories_CreatedAt");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProductSellInventories",
                table: "ProductSellInventories",
                column: "product_sell_inventory_id_pk");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductSellInventories_Products_ProductId",
                table: "ProductSellInventories",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
