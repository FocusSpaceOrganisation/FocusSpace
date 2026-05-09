using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FocusSpace.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskPriority : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Priority",
                table: "Tasks",
                type: "text",
                nullable: false,
                defaultValue: "Medium");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Tasks");
        }
    }
}
