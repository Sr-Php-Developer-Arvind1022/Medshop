using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Medshop.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomersAndSalesModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "customers",
                columns: table => new
                {
                    customer_pk = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LoginId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Mobile = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customers", x => x.customer_pk);
                });

            migrationBuilder.CreateTable(
                name: "sales",
                columns: table => new
                {
                    sale_pk = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LoginId = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_fk = table.Column<long>(type: "bigint", nullable: false),
                    bill_no = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    discount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    tax = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    grand_total = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    payment_mode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    bill_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sales", x => x.sale_pk);
                    table.ForeignKey(
                        name: "FK_sales_customers_customer_fk",
                        column: x => x.customer_fk,
                        principalTable: "customers",
                        principalColumn: "customer_pk",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sale_items",
                columns: table => new
                {
                    sale_item_pk = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sale_fk = table.Column<long>(type: "bigint", nullable: false),
                    product_fk = table.Column<long>(type: "bigint", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    purchase_price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sale_items", x => x.sale_item_pk);
                    table.ForeignKey(
                        name: "FK_sale_items_Products_product_fk",
                        column: x => x.product_fk,
                        principalTable: "Products",
                        principalColumn: "product_id_pk",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sale_items_sales_sale_fk",
                        column: x => x.sale_fk,
                        principalTable: "sales",
                        principalColumn: "sale_pk",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_customers_Id",
                table: "customers",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customers_LoginId_Mobile",
                table: "customers",
                columns: new[] { "LoginId", "Mobile" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sale_items_product_fk",
                table: "sale_items",
                column: "product_fk");

            migrationBuilder.CreateIndex(
                name: "IX_sale_items_sale_fk",
                table: "sale_items",
                column: "sale_fk");

            migrationBuilder.CreateIndex(
                name: "IX_sales_bill_date",
                table: "sales",
                column: "bill_date");

            migrationBuilder.CreateIndex(
                name: "IX_sales_customer_fk",
                table: "sales",
                column: "customer_fk");

            migrationBuilder.CreateIndex(
                name: "IX_sales_Id",
                table: "sales",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sales_LoginId_bill_no",
                table: "sales",
                columns: new[] { "LoginId", "bill_no" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sale_items");

            migrationBuilder.DropTable(
                name: "sales");

            migrationBuilder.DropTable(
                name: "customers");
        }
    }
}
