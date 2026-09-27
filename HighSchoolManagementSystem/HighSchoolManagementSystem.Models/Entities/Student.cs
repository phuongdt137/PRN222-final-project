using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HighSchoolManagementSystem.Models.Entities;

[Table("students")]
public class Student
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("student_code")]
    public string StudentCode { get; set; } = null!;

    [Column("admission_date")]
    public DateTime? AdmissionDate { get; set; }

    [Required]
    [MaxLength(30)]
    [Column("status")]
    public string Status { get; set; } = "Active";

    [MaxLength(255)]
    [Column("address")]
    public string? Address { get; set; }

    [Column("parent_id")]
    public int? ParentId { get; set; }

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;

    [ForeignKey(nameof(ParentId))]
    public virtual Parent? Parent { get; set; }

    public virtual ICollection<StudentClass> StudentClasses { get; set; } = new List<StudentClass>();
    public virtual ICollection<StudentScore> StudentScores { get; set; } = new List<StudentScore>();
    public virtual ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
    public virtual ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
    public virtual ICollection<ConductEvaluation> ConductEvaluations { get; set; } = new List<ConductEvaluation>();
    public virtual ICollection<SemesterSubjectResult> SemesterSubjectResults { get; set; } = new List<SemesterSubjectResult>();
    public virtual ICollection<SemesterReport> SemesterReports { get; set; } = new List<SemesterReport>();
    public virtual ICollection<TeacherFeedback> TeacherFeedbacks { get; set; } = new List<TeacherFeedback>();
}
