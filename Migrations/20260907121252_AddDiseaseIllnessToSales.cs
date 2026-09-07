using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medshop.Migrations
{
    /// <inheritdoc />
    public partial class AddDiseaseIllnessToSales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "disease_illness",
                table: "sales",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "disease_illness",
                table: "sales");
        }
    }
}
