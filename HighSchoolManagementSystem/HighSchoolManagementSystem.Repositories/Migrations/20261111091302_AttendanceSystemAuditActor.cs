using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HighSchoolManagementSystem.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AttendanceSystemAuditActor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "actor_user_id",
                table: "attendance_audit_logs",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Preserve system audit entries; never replace their actor with a fictitious user.
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM attendance_audit_logs WHERE actor_user_id IS NULL)
                    THROW 51000, 'Cannot roll back while system attendance audit entries exist. Preserve and review these entries first.', 1;
                """);
            migrationBuilder.AlterColumn<int>(
                name: "actor_user_id",
                table: "attendance_audit_logs",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
