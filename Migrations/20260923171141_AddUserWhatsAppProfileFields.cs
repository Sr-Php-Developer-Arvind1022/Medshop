using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medshop.Migrations
{
    /// <inheritdoc />
    public partial class AddUserWhatsAppProfileFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BusinessName",
                table: "Users",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerName",
                table: "Users",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WhatsAppApiKey",
                table: "Users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WhatsAppBaseUrl",
                table: "Users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WhatsAppTemplatesJson",
                table: "Users",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BusinessName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "OwnerName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "WhatsAppApiKey",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "WhatsAppBaseUrl",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "WhatsAppTemplatesJson",
                table: "Users");
        }
    }
}
