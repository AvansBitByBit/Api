using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BitByBitTrashAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddTemperatureToTrashPickup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Temperature",
                table: "LitterModels",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Temperature",
                table: "LitterModels");
        }
    }
}
