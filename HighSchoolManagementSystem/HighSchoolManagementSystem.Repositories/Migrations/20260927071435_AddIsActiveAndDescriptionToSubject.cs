using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HighSchoolManagementSystem.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddIsActiveAndDescriptionToSubject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "subjects",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "subjects",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "description",
                table: "subjects");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "subjects");
        }
    }
}
