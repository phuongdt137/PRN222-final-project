using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HighSchoolManagementSystem.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class LeaveRequestReviewedByUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_leave_requests_teachers_reviewed_by",
                table: "leave_requests");

            // reviewed_by trước đây trỏ Teacher.Id — xóa giá trị không còn hợp lệ trước khi gắn FK users
            migrationBuilder.Sql(
                """
                UPDATE leave_requests
                SET reviewed_by = NULL
                WHERE reviewed_by IS NOT NULL
                  AND reviewed_by NOT IN (SELECT id FROM users)
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_leave_requests_users_reviewed_by",
                table: "leave_requests",
                column: "reviewed_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_leave_requests_users_reviewed_by",
                table: "leave_requests");

            migrationBuilder.AddForeignKey(
                name: "FK_leave_requests_teachers_reviewed_by",
                table: "leave_requests",
                column: "reviewed_by",
                principalTable: "teachers",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
