using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FocusSpace.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanetAndDurationToTask : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PlanetId",
                table: "Tasks",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PlannedDuration",
                table: "Tasks",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_PlanetId",
                table: "Tasks",
                column: "PlanetId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_Planets_PlanetId",
                table: "Tasks",
                column: "PlanetId",
                principalTable: "Planets",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_Planets_PlanetId",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_PlanetId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "PlanetId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "PlannedDuration",
                table: "Tasks");
        }
    }
}
