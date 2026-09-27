using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HighSchoolManagementSystem.Models.Entities;

[Table("subjects")]
public class Subject
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("subject_code")]
    public string SubjectCode { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    [Column("subject_name")]
    public string SubjectName { get; set; } = null!;

    [Required]
    [MaxLength(30)]
    [Column("evaluation_type")]
    public string EvaluationType { get; set; } = "Score";

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [MaxLength(500)]
    [Column("description")]
    public string? Description { get; set; }

    public virtual ICollection<TeachingAssignment> TeachingAssignments { get; set; } = new List<TeachingAssignment>();
    public virtual ICollection<SemesterSubjectResult> SemesterSubjectResults { get; set; } = new List<SemesterSubjectResult>();
}
