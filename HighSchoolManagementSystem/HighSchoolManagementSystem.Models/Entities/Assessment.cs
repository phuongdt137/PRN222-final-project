using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HighSchoolManagementSystem.Models.Entities;

[Table("assessments")]
public class Assessment
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("teaching_assignment_id")]
    public int TeachingAssignmentId { get; set; }

    [Column("assessment_type_id")]
    public int AssessmentTypeId { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("assessment_name")]
    public string AssessmentName { get; set; } = null!;

    [Column("conducted_at")]
    public DateTime? ConductedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(TeachingAssignmentId))]
    public virtual TeachingAssignment TeachingAssignment { get; set; } = null!;

    [ForeignKey(nameof(AssessmentTypeId))]
    public virtual AssessmentType AssessmentType { get; set; } = null!;

    public virtual ICollection<StudentScore> StudentScores { get; set; } = new List<StudentScore>();
}
