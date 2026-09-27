using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HighSchoolManagementSystem.Models.Entities;

[Table("teachers")]
public class Teacher
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("teacher_code")]
    public string TeacherCode { get; set; } = null!;

    [MaxLength(100)]
    [Column("specialization")]
    public string? Specialization { get; set; }

    [Column("max_periods_per_week")]
    public int? MaxPeriodsPerWeek { get; set; }

    [MaxLength(100)]
    [Column("qualification")]
    public string? Qualification { get; set; }

    [Column("hire_date")]
    public DateTime? HireDate { get; set; }

    [Required]
    [MaxLength(30)]
    [Column("status")]
    public string Status { get; set; } = "Active";

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;

    public virtual ICollection<Class> HomeroomClasses { get; set; } = new List<Class>();
    public virtual ICollection<TeachingAssignment> TeachingAssignments { get; set; } = new List<TeachingAssignment>();
    public virtual ICollection<ConductEvaluation> ConductEvaluations { get; set; } = new List<ConductEvaluation>();
    public virtual ICollection<AttendanceRecord> RecordedAttendances { get; set; } = new List<AttendanceRecord>();
    public virtual ICollection<LeaveRequest> ReviewedLeaves { get; set; } = new List<LeaveRequest>();
    public virtual ICollection<TeacherFeedback> TeacherFeedbacks { get; set; } = new List<TeacherFeedback>();
}
