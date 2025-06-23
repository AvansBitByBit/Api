using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BitByBitTrashAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddTempAndCoords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "LitterModels",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "LitterModels",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "LitterModels");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "LitterModels");
        }
    }
}
