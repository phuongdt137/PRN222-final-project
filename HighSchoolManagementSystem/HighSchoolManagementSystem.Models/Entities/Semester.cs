using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HighSchoolManagementSystem.Models.Entities;

[Table("semesters")]
public class Semester
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("semester_code")]
    public string SemesterCode { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    [Column("semester_name")]
    public string SemesterName { get; set; } = null!;

    [Column("start_date")]
    public DateTime StartDate { get; set; }

    [Column("end_date")]
    public DateTime EndDate { get; set; }

    [Column("academic_year_id")]
    public int AcademicYearId { get; set; }

    // Navigation properties
    [ForeignKey(nameof(AcademicYearId))]
    public virtual AcademicYear AcademicYear { get; set; } = null!;

    public virtual ICollection<TeachingAssignment> TeachingAssignments { get; set; } = new List<TeachingAssignment>();
    public virtual ICollection<ConductEvaluation> ConductEvaluations { get; set; } = new List<ConductEvaluation>();
    public virtual ICollection<SemesterSubjectResult> SemesterSubjectResults { get; set; } = new List<SemesterSubjectResult>();
    public virtual ICollection<SemesterReport> SemesterReports { get; set; } = new List<SemesterReport>();
    public virtual ICollection<TeacherFeedback> TeacherFeedbacks { get; set; } = new List<TeacherFeedback>();
}
