using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HighSchoolManagementSystem.Models.Entities;

[Table("student_scores")]
public class StudentScore
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("student_id")]
    public int StudentId { get; set; }

    [Column("assessment_id")]
    public int AssessmentId { get; set; }

    [Column("score", TypeName = "decimal(5,2)")]
    public decimal? Score { get; set; }

    [MaxLength(255)]
    [Column("comment")]
    public string? Comment { get; set; }

    [Column("is_locked")]
    public bool IsLocked { get; set; } = false;

    // Navigation properties
    [ForeignKey(nameof(StudentId))]
    public virtual Student Student { get; set; } = null!;

    [ForeignKey(nameof(AssessmentId))]
    public virtual Assessment Assessment { get; set; } = null!;

    public virtual ICollection<ScoreAuditLog> ScoreAuditLogs { get; set; } = new List<ScoreAuditLog>();
}
