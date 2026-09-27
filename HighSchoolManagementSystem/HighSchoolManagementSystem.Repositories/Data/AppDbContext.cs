using Microsoft.EntityFrameworkCore;
using HighSchoolManagementSystem.Models.Entities;

namespace HighSchoolManagementSystem.Repositories.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public virtual DbSet<Role> Roles => Set<Role>();
    public virtual DbSet<User> Users => Set<User>();
    public virtual DbSet<Parent> Parents => Set<Parent>();
    public virtual DbSet<Student> Students => Set<Student>();
    public virtual DbSet<Teacher> Teachers => Set<Teacher>();
    public virtual DbSet<AcademicYear> AcademicYears => Set<AcademicYear>();
    public virtual DbSet<Semester> Semesters => Set<Semester>();
    public virtual DbSet<GradeLevel> GradeLevels => Set<GradeLevel>();
    public virtual DbSet<Class> Classes => Set<Class>();
    public virtual DbSet<StudentClass> StudentClasses => Set<StudentClass>();
    public virtual DbSet<Subject> Subjects => Set<Subject>();
    public virtual DbSet<TeachingAssignment> TeachingAssignments => Set<TeachingAssignment>();
    public virtual DbSet<AssessmentType> AssessmentTypes => Set<AssessmentType>();
    public virtual DbSet<Assessment> Assessments => Set<Assessment>();
    public virtual DbSet<StudentScore> StudentScores => Set<StudentScore>();
    public virtual DbSet<ScoreAuditLog> ScoreAuditLogs => Set<ScoreAuditLog>();
    public virtual DbSet<Room> Rooms => Set<Room>();
    public virtual DbSet<TimeSlot> TimeSlots => Set<TimeSlot>();
    public virtual DbSet<TimetableEntry> TimetableEntries => Set<TimetableEntry>();
    public virtual DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public virtual DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public virtual DbSet<ConductEvaluation> ConductEvaluations => Set<ConductEvaluation>();
    public virtual DbSet<Notification> Notifications => Set<Notification>();
    public virtual DbSet<SemesterSubjectResult> SemesterSubjectResults => Set<SemesterSubjectResult>();
    public virtual DbSet<SemesterReport> SemesterReports => Set<SemesterReport>();
    public virtual DbSet<TeacherFeedback> TeacherFeedbacks => Set<TeacherFeedback>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ========================================================
        // 1. UNIQUE INDEXES
        // ========================================================
        modelBuilder.Entity<Role>()
            .HasIndex(r => r.RoleName)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Parent>()
            .HasIndex(p => p.UserId)
            .IsUnique();

        modelBuilder.Entity<Student>()
            .HasIndex(s => s.UserId)
            .IsUnique();

        modelBuilder.Entity<Student>()
            .HasIndex(s => s.StudentCode)
            .IsUnique();

        modelBuilder.Entity<Teacher>()
            .HasIndex(t => t.UserId)
            .IsUnique();

        modelBuilder.Entity<Teacher>()
            .HasIndex(t => t.TeacherCode)
            .IsUnique();

        modelBuilder.Entity<AcademicYear>()
            .HasIndex(y => y.YearCode)
            .IsUnique();

        modelBuilder.Entity<Semester>()
            .HasIndex(s => s.SemesterCode)
            .IsUnique();

        modelBuilder.Entity<GradeLevel>()
            .HasIndex(g => g.LevelCode)
            .IsUnique();

        modelBuilder.Entity<Subject>()
            .HasIndex(s => s.SubjectCode)
            .IsUnique();

        modelBuilder.Entity<AssessmentType>()
            .HasIndex(a => a.TypeCode)
            .IsUnique();

        modelBuilder.Entity<Room>()
            .HasIndex(r => r.RoomCode)
            .IsUnique();

        modelBuilder.Entity<TimeSlot>()
            .HasIndex(t => t.SlotCode)
            .IsUnique();

        // ========================================================
        // 2. EXPLICIT RELATIONSHIPS CONFIGURATION
        // ========================================================

        // 1-to-1 relationships with User
        modelBuilder.Entity<Parent>()
            .HasOne(p => p.User)
            .WithOne(u => u.Parent)
            .HasForeignKey<Parent>(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Student>()
            .HasOne(s => s.User)
            .WithOne(u => u.Student)
            .HasForeignKey<Student>(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Teacher>()
            .HasOne(t => t.User)
            .WithOne(u => u.Teacher)
            .HasForeignKey<Teacher>(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Notification (Sender & Recipient)
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasOne(n => n.Sender)
                .WithMany(u => u.SentNotifications)
                .HasForeignKey(n => n.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(n => n.Recipient)
                .WithMany(u => u.ReceivedNotifications)
                .HasForeignKey(n => n.RecipientId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // LeaveRequest
        modelBuilder.Entity<LeaveRequest>(entity =>
        {
            entity.HasOne(lr => lr.Student)
                .WithMany(s => s.LeaveRequests)
                .HasForeignKey(lr => lr.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(lr => lr.RequestedByUser)
                .WithMany()
                .HasForeignKey(lr => lr.RequestBy)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(lr => lr.ReviewedByTeacher)
                .WithMany(t => t.ReviewedLeaves)
                .HasForeignKey(lr => lr.ReviewedBy)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // AttendanceRecord
        modelBuilder.Entity<AttendanceRecord>(entity =>
        {
            entity.HasOne(ar => ar.Student)
                .WithMany(s => s.AttendanceRecords)
                .HasForeignKey(ar => ar.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(ar => ar.RecordedByTeacher)
                .WithMany(t => t.RecordedAttendances)
                .HasForeignKey(ar => ar.RecordedBy)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(ar => ar.LeaveRequest)
                .WithMany(lr => lr.AttendanceRecords)
                .HasForeignKey(ar => ar.LeaveRequestId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(ar => ar.TimetableEntry)
                .WithMany(te => te.AttendanceRecords)
                .HasForeignKey(ar => ar.TimetableEntryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Class
        modelBuilder.Entity<Class>(entity =>
        {
            entity.HasOne(c => c.HomeroomTeacher)
                .WithMany(t => t.HomeroomClasses)
                .HasForeignKey(c => c.HomeroomTeacherId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(c => c.AcademicYear)
                .WithMany(y => y.Classes)
                .HasForeignKey(c => c.AcademicYearId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.GradeLevel)
                .WithMany(g => g.Classes)
                .HasForeignKey(c => c.GradeLevelId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ========================================================
        // 3. GLOBAL CONVENTION: PREVENT SQL SERVER CASCADE CYCLES
        // ========================================================
        foreach (var foreignKey in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            if (foreignKey.IsOwnership) continue;
            if (foreignKey.DeleteBehavior == DeleteBehavior.Cascade)
            {
                foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
            }
        }
    }
}
