using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HighSchoolManagementSystem.Models.Entities;

[Table("semester_subject_results")]
public class SemesterSubjectResult
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("student_id")]
    public int StudentId { get; set; }

    [Column("subject_id")]
    public int SubjectId { get; set; }

    [Column("semester_id")]
    public int SemesterId { get; set; }

    [Column("average_score", TypeName = "decimal(5,2)")]
    public decimal? AverageScore { get; set; }

    [Column("is_passed")]
    public bool IsPassed { get; set; } = true;

    [MaxLength(255)]
    [Column("remarks")]
    public string? Remarks { get; set; }

    // Navigation properties
    [ForeignKey(nameof(StudentId))]
    public virtual Student Student { get; set; } = null!;

    [ForeignKey(nameof(SubjectId))]
    public virtual Subject Subject { get; set; } = null!;

    [ForeignKey(nameof(SemesterId))]
    public virtual Semester Semester { get; set; } = null!;
}
