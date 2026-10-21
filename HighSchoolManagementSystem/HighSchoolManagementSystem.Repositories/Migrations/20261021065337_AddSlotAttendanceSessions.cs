using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HighSchoolManagementSystem.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddSlotAttendanceSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Stop before changing legacy data if its meaning cannot be preserved safely.
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM attendance_records
                    WHERE status NOT IN ('Present', 'Absent', 'Late', 'Excused')
                       OR attendance_date <> CONVERT(date, attendance_date))
                    THROW 51000, 'Attendance migration: review invalid status or non-midnight legacy dates.', 1;
                IF EXISTS (SELECT 1 FROM attendance_records
                    GROUP BY student_id, timetable_entry_id, CONVERT(date, attendance_date) HAVING COUNT(*) > 1)
                    THROW 51000, 'Attendance migration: duplicate student/lesson/date. Resolve duplicates without deleting history.', 1;
                IF EXISTS (SELECT 1 FROM attendance_records r
                    JOIN timetable_entries e ON e.id = r.timetable_entry_id
                    JOIN teaching_assignments a ON a.id = e.teaching_assignment_id
                    JOIN semesters sem ON sem.id = a.semester_id
                    WHERE r.recorded_by <> a.teacher_id
                       OR r.attendance_date < CONVERT(date, sem.start_date)
                       OR r.attendance_date > CONVERT(date, sem.end_date))
                    THROW 51000, 'Attendance migration: legacy teacher or semester differs from current schedule. Review history first.', 1;
                IF EXISTS (SELECT 1 FROM (
                    SELECT DISTINCT r.timetable_entry_id, CONVERT(date, r.attendance_date) AS lesson_date, a.class_id, e.time_slot_id
                    FROM attendance_records r JOIN timetable_entries e ON e.id = r.timetable_entry_id
                    JOIN teaching_assignments a ON a.id = e.teaching_assignment_id
                ) lessons GROUP BY class_id, lesson_date, time_slot_id HAVING COUNT(*) > 1)
                    THROW 51000, 'Attendance migration: multiple lessons occupy the same class/date/slot.', 1;
                IF EXISTS (SELECT 1 FROM attendance_records r
                    JOIN timetable_entries e ON e.id = r.timetable_entry_id
                    JOIN teaching_assignments a ON a.id = e.teaching_assignment_id
                    JOIN time_slots ts ON ts.id = e.time_slot_id
                    WHERE e.day_of_week <> ((DATEDIFF(day, '19000101', CONVERT(date, r.attendance_date)) % 7 + 7) % 7 + 2)
                       OR NOT EXISTS (SELECT 1 FROM student_classes sc WHERE sc.student_id = r.student_id AND sc.class_id = a.class_id
                            AND sc.joined_at <= DATEADD(hour, -7, DATEADD(second, DATEDIFF(second, CAST('00:00:00' AS time), ts.start_time), CAST(CONVERT(date, r.attendance_date) AS datetime2)))
                            AND (sc.left_at IS NULL OR sc.left_at > DATEADD(hour, -7, DATEADD(second, DATEDIFF(second, CAST('00:00:00' AS time), ts.start_time), CAST(CONVERT(date, r.attendance_date) AS datetime2))))))
                    THROW 51000, 'Attendance migration: legacy weekday or class membership differs from schedule. Review history first.', 1;
                """);
            migrationBuilder.DropIndex(
                name: "IX_attendance_records_student_id",
                table: "attendance_records");

            migrationBuilder.AlterColumn<int>(
                name: "recorded_by",
                table: "attendance_records",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<DateTime>(
                name: "attendance_date",
                table: "attendance_records",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<int>(
                name: "attendance_session_id",
                table: "attendance_records",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "recorded_at",
                table: "attendance_records",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "attendance_records",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "attendance_sessions",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    timetable_entry_id = table.Column<int>(type: "int", nullable: false),
                    attendance_date = table.Column<DateTime>(type: "date", nullable: false),
                    class_id = table.Column<int>(type: "int", nullable: false),
                    subject_id = table.Column<int>(type: "int", nullable: false),
                    semester_id = table.Column<int>(type: "int", nullable: false),
                    assigned_teacher_id = table.Column<int>(type: "int", nullable: false),
                    time_slot_id = table.Column<int>(type: "int", nullable: false),
                    room_id = table.Column<int>(type: "int", nullable: false),
                    slot_code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    school_session = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    period_no = table.Column<int>(type: "int", nullable: false),
                    start_time = table.Column<TimeSpan>(type: "time", nullable: false),
                    end_time = table.Column<TimeSpan>(type: "time", nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    created_by_user_id = table.Column<int>(type: "int", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    submitted_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attendance_sessions", x => x.id);
                    table.UniqueConstraint("AK_attendance_sessions_id_timetable_entry_id_attendance_date", x => new { x.id, x.timetable_entry_id, x.attendance_date });
                    table.CheckConstraint("CK_AttendanceSession_Status", "[status] IN ('Open', 'Submitted', 'Cancelled')");
                    table.ForeignKey(
                        name: "FK_attendance_sessions_classes_class_id",
                        column: x => x.class_id,
                        principalTable: "classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_attendance_sessions_rooms_room_id",
                        column: x => x.room_id,
                        principalTable: "rooms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_attendance_sessions_semesters_semester_id",
                        column: x => x.semester_id,
                        principalTable: "semesters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_attendance_sessions_subjects_subject_id",
                        column: x => x.subject_id,
                        principalTable: "subjects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_attendance_sessions_teachers_assigned_teacher_id",
                        column: x => x.assigned_teacher_id,
                        principalTable: "teachers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_attendance_sessions_time_slots_time_slot_id",
                        column: x => x.time_slot_id,
                        principalTable: "time_slots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_attendance_sessions_timetable_entries_timetable_entry_id",
                        column: x => x.timetable_entry_id,
                        principalTable: "timetable_entries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_attendance_sessions_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "attendance_audit_logs",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    attendance_session_id = table.Column<int>(type: "int", nullable: false),
                    actor_user_id = table.Column<int>(type: "int", nullable: false),
                    action = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    details = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    occurred_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attendance_audit_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_attendance_audit_logs_attendance_sessions_attendance_session_id",
                        column: x => x.attendance_session_id,
                        principalTable: "attendance_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_attendance_audit_logs_users_actor_user_id",
                        column: x => x.actor_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql("""
                INSERT INTO attendance_sessions
                    (timetable_entry_id, attendance_date, class_id, subject_id, semester_id, assigned_teacher_id,
                     time_slot_id, room_id, slot_code, school_session, period_no, start_time, end_time, status)
                SELECT r.timetable_entry_id, r.attendance_date, a.class_id, a.subject_id, a.semester_id, a.teacher_id,
                       e.time_slot_id, e.room_id, ts.slot_code, ts.session, ts.period_no, ts.start_time, ts.end_time, 'Submitted'
                FROM (SELECT DISTINCT timetable_entry_id, attendance_date FROM attendance_records) r
                JOIN timetable_entries e ON e.id = r.timetable_entry_id
                JOIN teaching_assignments a ON a.id = e.teaching_assignment_id
                JOIN time_slots ts ON ts.id = e.time_slot_id;
                UPDATE r SET attendance_session_id = s.id
                FROM attendance_records r JOIN attendance_sessions s
                    ON s.timetable_entry_id = r.timetable_entry_id AND s.attendance_date = r.attendance_date;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_attendance_records_attendance_session_id_student_id",
                table: "attendance_records",
                columns: new[] { "attendance_session_id", "student_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_attendance_records_attendance_session_id_timetable_entry_id_attendance_date",
                table: "attendance_records",
                columns: new[] { "attendance_session_id", "timetable_entry_id", "attendance_date" });

            migrationBuilder.CreateIndex(
                name: "IX_attendance_records_student_id_attendance_date",
                table: "attendance_records",
                columns: new[] { "student_id", "attendance_date" });

            migrationBuilder.CreateIndex(
                name: "IX_attendance_records_student_id_timetable_entry_id_attendance_date",
                table: "attendance_records",
                columns: new[] { "student_id", "timetable_entry_id", "attendance_date" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_AttendanceRecord_Status",
                table: "attendance_records",
                sql: "([status] = 'NotMarked' AND [recorded_by] IS NULL) OR ([status] IN ('Present', 'Absent', 'Late', 'Excused') AND [recorded_by] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_audit_logs_actor_user_id",
                table: "attendance_audit_logs",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_audit_logs_attendance_session_id_occurred_at",
                table: "attendance_audit_logs",
                columns: new[] { "attendance_session_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "IX_attendance_sessions_assigned_teacher_id_attendance_date",
                table: "attendance_sessions",
                columns: new[] { "assigned_teacher_id", "attendance_date" });

            migrationBuilder.CreateIndex(
                name: "IX_attendance_sessions_class_id_attendance_date_time_slot_id",
                table: "attendance_sessions",
                columns: new[] { "class_id", "attendance_date", "time_slot_id" },
                unique: true,
                filter: "[status] <> 'Cancelled'");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_sessions_created_by_user_id",
                table: "attendance_sessions",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_sessions_room_id",
                table: "attendance_sessions",
                column: "room_id");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_sessions_semester_id_attendance_date",
                table: "attendance_sessions",
                columns: new[] { "semester_id", "attendance_date" });

            migrationBuilder.CreateIndex(
                name: "IX_attendance_sessions_subject_id",
                table: "attendance_sessions",
                column: "subject_id");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_sessions_time_slot_id",
                table: "attendance_sessions",
                column: "time_slot_id");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_sessions_timetable_entry_id_attendance_date",
                table: "attendance_sessions",
                columns: new[] { "timetable_entry_id", "attendance_date" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_attendance_records_attendance_sessions_attendance_session_id_timetable_entry_id_attendance_date",
                table: "attendance_records",
                columns: new[] { "attendance_session_id", "timetable_entry_id", "attendance_date" },
                principalTable: "attendance_sessions",
                principalColumns: new[] { "id", "timetable_entry_id", "attendance_date" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM attendance_audit_logs)
                    OR EXISTS (SELECT 1 FROM attendance_records WHERE recorded_by IS NULL)
                    THROW 51000, 'Attendance rollback would lose new attendance history. Restore a verified backup instead.', 1;
                """);
            migrationBuilder.DropForeignKey(
                name: "FK_attendance_records_attendance_sessions_attendance_session_id_timetable_entry_id_attendance_date",
                table: "attendance_records");

            migrationBuilder.DropTable(
                name: "attendance_audit_logs");

            migrationBuilder.DropTable(
                name: "attendance_sessions");

            migrationBuilder.DropIndex(
                name: "IX_attendance_records_attendance_session_id_student_id",
                table: "attendance_records");

            migrationBuilder.DropIndex(
                name: "IX_attendance_records_attendance_session_id_timetable_entry_id_attendance_date",
                table: "attendance_records");

            migrationBuilder.DropIndex(
                name: "IX_attendance_records_student_id_attendance_date",
                table: "attendance_records");

            migrationBuilder.DropIndex(
                name: "IX_attendance_records_student_id_timetable_entry_id_attendance_date",
                table: "attendance_records");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AttendanceRecord_Status",
                table: "attendance_records");

            migrationBuilder.DropColumn(
                name: "attendance_session_id",
                table: "attendance_records");

            migrationBuilder.DropColumn(
                name: "recorded_at",
                table: "attendance_records");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "attendance_records");

            migrationBuilder.AlterColumn<int>(
                name: "recorded_by",
                table: "attendance_records",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "attendance_date",
                table: "attendance_records",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "date");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_records_student_id",
                table: "attendance_records",
                column: "student_id");
        }
    }
}
