using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HighSchoolManagementSystem.Models.Entities;

[Table("score_audit_logs")]
public class ScoreAuditLog
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("student_score_id")]
    public int StudentScoreId { get; set; }

    [Column("old_score", TypeName = "decimal(5,2)")]
    public decimal? OldScore { get; set; }

    [Column("new_score", TypeName = "decimal(5,2)")]
    public decimal? NewScore { get; set; }

    [Column("modified_by")]
    public int ModifiedBy { get; set; }

    [Column("modified_at")]
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(255)]
    [Column("reason")]
    public string? Reason { get; set; }

    // Navigation properties
    [ForeignKey(nameof(StudentScoreId))]
    public virtual StudentScore StudentScore { get; set; } = null!;

    [ForeignKey(nameof(ModifiedBy))]
    public virtual User ModifiedByUser { get; set; } = null!;
}
