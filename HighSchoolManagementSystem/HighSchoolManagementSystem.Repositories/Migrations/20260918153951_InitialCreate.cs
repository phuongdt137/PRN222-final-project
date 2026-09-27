using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HighSchoolManagementSystem.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "academic_years",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    year_code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    start_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    end_date = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_academic_years", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "assessment_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    type_code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    type_name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    weight = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    min_entries = table.Column<int>(type: "int", nullable: false),
                    max_entries = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assessment_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "grade_levels",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    level_code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    level_name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_grade_levels", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    role_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    role_name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.role_id);
                });

            migrationBuilder.CreateTable(
                name: "rooms",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    room_code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    room_name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    building = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    room_type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    capacity = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rooms", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "subjects",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    subject_code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    subject_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    evaluation_type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subjects", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "time_slots",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    slot_code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    period_no = table.Column<int>(type: "int", nullable: false),
                    start_time = table.Column<TimeSpan>(type: "time", nullable: false),
                    end_time = table.Column<TimeSpan>(type: "time", nullable: false),
                    session = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_time_slots", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "semesters",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    semester_code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    semester_name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    start_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    end_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    academic_year_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_semesters", x => x.id);
                    table.ForeignKey(
                        name: "FK_semesters_academic_years_academic_year_id",
                        column: x => x.academic_year_id,
                        principalTable: "academic_years",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    password_hash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    phone_number = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    gender = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    create_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_of_birth = table.Column<DateTime>(type: "datetime2", nullable: true),
                    avatar_url = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    role_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                    table.ForeignKey(
                        name: "FK_users_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "role_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    sender_id = table.Column<int>(type: "int", nullable: false),
                    recipient_id = table.Column<int>(type: "int", nullable: false),
                    title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    content = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    is_read = table.Column<bool>(type: "bit", nullable: false),
                    sent_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.id);
                    table.ForeignKey(
                        name: "FK_notifications_users_recipient_id",
                        column: x => x.recipient_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_notifications_users_sender_id",
                        column: x => x.sender_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "parents",
                columns: table => new
                {
                    parent_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    occupation = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    emergency_contact = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    user_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_parents", x => x.parent_id);
                    table.ForeignKey(
                        name: "FK_parents_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "teachers",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    teacher_code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    specialization = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    max_periods_per_week = table.Column<int>(type: "int", nullable: true),
                    qualification = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    hire_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teachers", x => x.id);
                    table.ForeignKey(
                        name: "FK_teachers_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "students",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    student_code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    admission_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    address = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    parent_id = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_students", x => x.id);
                    table.ForeignKey(
                        name: "FK_students_parents_parent_id",
                        column: x => x.parent_id,
                        principalTable: "parents",
                        principalColumn: "parent_id");
                    table.ForeignKey(
                        name: "FK_students_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "classes",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    class_name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    homeroom_teacher_id = table.Column<int>(type: "int", nullable: true),
                    academic_year_id = table.Column<int>(type: "int", nullable: false),
                    grade_level_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_classes", x => x.id);
                    table.ForeignKey(
                        name: "FK_classes_academic_years_academic_year_id",
                        column: x => x.academic_year_id,
                        principalTable: "academic_years",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_classes_grade_levels_grade_level_id",
                        column: x => x.grade_level_id,
                        principalTable: "grade_levels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_classes_teachers_homeroom_teacher_id",
                        column: x => x.homeroom_teacher_id,
                        principalTable: "teachers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "conduct_evaluations",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    student_id = table.Column<int>(type: "int", nullable: false),
                    semester_id = table.Column<int>(type: "int", nullable: false),
                    homeroom_teacher_id = table.Column<int>(type: "int", nullable: false),
                    conduct_grade = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    teacher_comment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    evaluated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conduct_evaluations", x => x.id);
                    table.ForeignKey(
                        name: "FK_conduct_evaluations_semesters_semester_id",
                        column: x => x.semester_id,
                        principalTable: "semesters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_conduct_evaluations_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_conduct_evaluations_teachers_homeroom_teacher_id",
                        column: x => x.homeroom_teacher_id,
                        principalTable: "teachers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "leave_requests",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    student_id = table.Column<int>(type: "int", nullable: false),
                    request_by = table.Column<int>(type: "int", nullable: false),
                    reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    attachment = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    review_note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    start_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    end_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    requested_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    review_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    reviewed_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leave_requests", x => x.id);
                    table.ForeignKey(
                        name: "FK_leave_requests_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_leave_requests_teachers_reviewed_by",
                        column: x => x.reviewed_by,
                        principalTable: "teachers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_leave_requests_users_request_by",
                        column: x => x.request_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "semester_reports",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    student_id = table.Column<int>(type: "int", nullable: false),
                    semester_id = table.Column<int>(type: "int", nullable: false),
                    gpa = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    conduct_grade = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    academic_rank = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    class_rank = table.Column<int>(type: "int", nullable: true),
                    teacher_comments = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_semester_reports", x => x.id);
                    table.ForeignKey(
                        name: "FK_semester_reports_semesters_semester_id",
                        column: x => x.semester_id,
                        principalTable: "semesters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_semester_reports_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "semester_subject_results",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    student_id = table.Column<int>(type: "int", nullable: false),
                    subject_id = table.Column<int>(type: "int", nullable: false),
                    semester_id = table.Column<int>(type: "int", nullable: false),
                    average_score = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    is_passed = table.Column<bool>(type: "bit", nullable: false),
                    remarks = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_semester_subject_results", x => x.id);
                    table.ForeignKey(
                        name: "FK_semester_subject_results_semesters_semester_id",
                        column: x => x.semester_id,
                        principalTable: "semesters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_semester_subject_results_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_semester_subject_results_subjects_subject_id",
                        column: x => x.subject_id,
                        principalTable: "subjects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "teacher_feedbacks",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    student_id = table.Column<int>(type: "int", nullable: false),
                    teacher_id = table.Column<int>(type: "int", nullable: false),
                    semester_id = table.Column<int>(type: "int", nullable: false),
                    feedback_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    content = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teacher_feedbacks", x => x.id);
                    table.ForeignKey(
                        name: "FK_teacher_feedbacks_semesters_semester_id",
                        column: x => x.semester_id,
                        principalTable: "semesters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_teacher_feedbacks_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_teacher_feedbacks_teachers_teacher_id",
                        column: x => x.teacher_id,
                        principalTable: "teachers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "student_classes",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    student_id = table.Column<int>(type: "int", nullable: false),
                    class_id = table.Column<int>(type: "int", nullable: false),
                    joined_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    left_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_student_classes", x => x.id);
                    table.ForeignKey(
                        name: "FK_student_classes_classes_class_id",
                        column: x => x.class_id,
                        principalTable: "classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_student_classes_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "teaching_assignments",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    teacher_id = table.Column<int>(type: "int", nullable: false),
                    subject_id = table.Column<int>(type: "int", nullable: false),
                    class_id = table.Column<int>(type: "int", nullable: false),
                    semester_id = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teaching_assignments", x => x.id);
                    table.ForeignKey(
                        name: "FK_teaching_assignments_classes_class_id",
                        column: x => x.class_id,
                        principalTable: "classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_teaching_assignments_semesters_semester_id",
                        column: x => x.semester_id,
                        principalTable: "semesters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_teaching_assignments_subjects_subject_id",
                        column: x => x.subject_id,
                        principalTable: "subjects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_teaching_assignments_teachers_teacher_id",
                        column: x => x.teacher_id,
                        principalTable: "teachers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "assessments",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    teaching_assignment_id = table.Column<int>(type: "int", nullable: false),
                    assessment_type_id = table.Column<int>(type: "int", nullable: false),
                    assessment_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    conducted_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assessments", x => x.id);
                    table.ForeignKey(
                        name: "FK_assessments_assessment_types_assessment_type_id",
                        column: x => x.assessment_type_id,
                        principalTable: "assessment_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_assessments_teaching_assignments_teaching_assignment_id",
                        column: x => x.teaching_assignment_id,
                        principalTable: "teaching_assignments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "timetable_entries",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    teaching_assignment_id = table.Column<int>(type: "int", nullable: false),
                    room_id = table.Column<int>(type: "int", nullable: false),
                    time_slot_id = table.Column<int>(type: "int", nullable: false),
                    day_of_week = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_timetable_entries", x => x.id);
                    table.ForeignKey(
                        name: "FK_timetable_entries_rooms_room_id",
                        column: x => x.room_id,
                        principalTable: "rooms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_timetable_entries_teaching_assignments_teaching_assignment_id",
                        column: x => x.teaching_assignment_id,
                        principalTable: "teaching_assignments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_timetable_entries_time_slots_time_slot_id",
                        column: x => x.time_slot_id,
                        principalTable: "time_slots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "student_scores",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    student_id = table.Column<int>(type: "int", nullable: false),
                    assessment_id = table.Column<int>(type: "int", nullable: false),
                    score = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    comment = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    is_locked = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_student_scores", x => x.id);
                    table.ForeignKey(
                        name: "FK_student_scores_assessments_assessment_id",
                        column: x => x.assessment_id,
                        principalTable: "assessments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_student_scores_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "attendance_records",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    student_id = table.Column<int>(type: "int", nullable: false),
                    recorded_by = table.Column<int>(type: "int", nullable: false),
                    leave_request_id = table.Column<int>(type: "int", nullable: true),
                    timetable_entry_id = table.Column<int>(type: "int", nullable: false),
                    attendance_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    comment = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attendance_records", x => x.id);
                    table.ForeignKey(
                        name: "FK_attendance_records_leave_requests_leave_request_id",
                        column: x => x.leave_request_id,
                        principalTable: "leave_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_attendance_records_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_attendance_records_teachers_recorded_by",
                        column: x => x.recorded_by,
                        principalTable: "teachers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_attendance_records_timetable_entries_timetable_entry_id",
                        column: x => x.timetable_entry_id,
                        principalTable: "timetable_entries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "score_audit_logs",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    student_score_id = table.Column<int>(type: "int", nullable: false),
                    old_score = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    new_score = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    modified_by = table.Column<int>(type: "int", nullable: false),
                    modified_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    reason = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_score_audit_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_score_audit_logs_student_scores_student_score_id",
                        column: x => x.student_score_id,
                        principalTable: "student_scores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_score_audit_logs_users_modified_by",
                        column: x => x.modified_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_academic_years_year_code",
                table: "academic_years",
                column: "year_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_assessment_types_type_code",
                table: "assessment_types",
                column: "type_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_assessments_assessment_type_id",
                table: "assessments",
                column: "assessment_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_assessments_teaching_assignment_id",
                table: "assessments",
                column: "teaching_assignment_id");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_records_leave_request_id",
                table: "attendance_records",
                column: "leave_request_id");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_records_recorded_by",
                table: "attendance_records",
                column: "recorded_by");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_records_student_id",
                table: "attendance_records",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_records_timetable_entry_id",
                table: "attendance_records",
                column: "timetable_entry_id");

            migrationBuilder.CreateIndex(
                name: "IX_classes_academic_year_id",
                table: "classes",
                column: "academic_year_id");

            migrationBuilder.CreateIndex(
                name: "IX_classes_grade_level_id",
                table: "classes",
                column: "grade_level_id");

            migrationBuilder.CreateIndex(
                name: "IX_classes_homeroom_teacher_id",
                table: "classes",
                column: "homeroom_teacher_id");

            migrationBuilder.CreateIndex(
                name: "IX_conduct_evaluations_homeroom_teacher_id",
                table: "conduct_evaluations",
                column: "homeroom_teacher_id");

            migrationBuilder.CreateIndex(
                name: "IX_conduct_evaluations_semester_id",
                table: "conduct_evaluations",
                column: "semester_id");

            migrationBuilder.CreateIndex(
                name: "IX_conduct_evaluations_student_id",
                table: "conduct_evaluations",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_grade_levels_level_code",
                table: "grade_levels",
                column: "level_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_leave_requests_request_by",
                table: "leave_requests",
                column: "request_by");

            migrationBuilder.CreateIndex(
                name: "IX_leave_requests_reviewed_by",
                table: "leave_requests",
                column: "reviewed_by");

            migrationBuilder.CreateIndex(
                name: "IX_leave_requests_student_id",
                table: "leave_requests",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_recipient_id",
                table: "notifications",
                column: "recipient_id");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_sender_id",
                table: "notifications",
                column: "sender_id");

            migrationBuilder.CreateIndex(
                name: "IX_parents_user_id",
                table: "parents",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_roles_role_name",
                table: "roles",
                column: "role_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rooms_room_code",
                table: "rooms",
                column: "room_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_score_audit_logs_modified_by",
                table: "score_audit_logs",
                column: "modified_by");

            migrationBuilder.CreateIndex(
                name: "IX_score_audit_logs_student_score_id",
                table: "score_audit_logs",
                column: "student_score_id");

            migrationBuilder.CreateIndex(
                name: "IX_semester_reports_semester_id",
                table: "semester_reports",
                column: "semester_id");

            migrationBuilder.CreateIndex(
                name: "IX_semester_reports_student_id",
                table: "semester_reports",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_semester_subject_results_semester_id",
                table: "semester_subject_results",
                column: "semester_id");

            migrationBuilder.CreateIndex(
                name: "IX_semester_subject_results_student_id",
                table: "semester_subject_results",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_semester_subject_results_subject_id",
                table: "semester_subject_results",
                column: "subject_id");

            migrationBuilder.CreateIndex(
                name: "IX_semesters_academic_year_id",
                table: "semesters",
                column: "academic_year_id");

            migrationBuilder.CreateIndex(
                name: "IX_semesters_semester_code",
                table: "semesters",
                column: "semester_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_student_classes_class_id",
                table: "student_classes",
                column: "class_id");

            migrationBuilder.CreateIndex(
                name: "IX_student_classes_student_id",
                table: "student_classes",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_student_scores_assessment_id",
                table: "student_scores",
                column: "assessment_id");

            migrationBuilder.CreateIndex(
                name: "IX_student_scores_student_id",
                table: "student_scores",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_students_parent_id",
                table: "students",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "IX_students_student_code",
                table: "students",
                column: "student_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_students_user_id",
                table: "students",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subjects_subject_code",
                table: "subjects",
                column: "subject_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_teacher_feedbacks_semester_id",
                table: "teacher_feedbacks",
                column: "semester_id");

            migrationBuilder.CreateIndex(
                name: "IX_teacher_feedbacks_student_id",
                table: "teacher_feedbacks",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_teacher_feedbacks_teacher_id",
                table: "teacher_feedbacks",
                column: "teacher_id");

            migrationBuilder.CreateIndex(
                name: "IX_teachers_teacher_code",
                table: "teachers",
                column: "teacher_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_teachers_user_id",
                table: "teachers",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_teaching_assignments_class_id",
                table: "teaching_assignments",
                column: "class_id");

            migrationBuilder.CreateIndex(
                name: "IX_teaching_assignments_semester_id",
                table: "teaching_assignments",
                column: "semester_id");

            migrationBuilder.CreateIndex(
                name: "IX_teaching_assignments_subject_id",
                table: "teaching_assignments",
                column: "subject_id");

            migrationBuilder.CreateIndex(
                name: "IX_teaching_assignments_teacher_id",
                table: "teaching_assignments",
                column: "teacher_id");

            migrationBuilder.CreateIndex(
                name: "IX_time_slots_slot_code",
                table: "time_slots",
                column: "slot_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_timetable_entries_room_id",
                table: "timetable_entries",
                column: "room_id");

            migrationBuilder.CreateIndex(
                name: "IX_timetable_entries_teaching_assignment_id",
                table: "timetable_entries",
                column: "teaching_assignment_id");

            migrationBuilder.CreateIndex(
                name: "IX_timetable_entries_time_slot_id",
                table: "timetable_entries",
                column: "time_slot_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_role_id",
                table: "users",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_username",
                table: "users",
                column: "username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attendance_records");

            migrationBuilder.DropTable(
                name: "conduct_evaluations");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "score_audit_logs");

            migrationBuilder.DropTable(
                name: "semester_reports");

            migrationBuilder.DropTable(
                name: "semester_subject_results");

            migrationBuilder.DropTable(
                name: "student_classes");

            migrationBuilder.DropTable(
                name: "teacher_feedbacks");

            migrationBuilder.DropTable(
                name: "leave_requests");

            migrationBuilder.DropTable(
                name: "timetable_entries");

            migrationBuilder.DropTable(
                name: "student_scores");

            migrationBuilder.DropTable(
                name: "rooms");

            migrationBuilder.DropTable(
                name: "time_slots");

            migrationBuilder.DropTable(
                name: "assessments");

            migrationBuilder.DropTable(
                name: "students");

            migrationBuilder.DropTable(
                name: "assessment_types");

            migrationBuilder.DropTable(
                name: "teaching_assignments");

            migrationBuilder.DropTable(
                name: "parents");

            migrationBuilder.DropTable(
                name: "classes");

            migrationBuilder.DropTable(
                name: "semesters");

            migrationBuilder.DropTable(
                name: "subjects");

            migrationBuilder.DropTable(
                name: "grade_levels");

            migrationBuilder.DropTable(
                name: "teachers");

            migrationBuilder.DropTable(
                name: "academic_years");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "roles");
        }
    }
}
