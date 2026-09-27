using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HighSchoolManagementSystem.Models.Entities;

[Table("teaching_assignments")]
public class TeachingAssignment
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("teacher_id")]
    public int TeacherId { get; set; }

    [Column("subject_id")]
    public int SubjectId { get; set; }

    [Column("class_id")]
    public int ClassId { get; set; }

    [Column("semester_id")]
    public int SemesterId { get; set; }

    [Required]
    [MaxLength(30)]
    [Column("status")]
    public string Status { get; set; } = "Assigned";

    // Navigation properties
    [ForeignKey(nameof(TeacherId))]
    public virtual Teacher Teacher { get; set; } = null!;

    [ForeignKey(nameof(SubjectId))]
    public virtual Subject Subject { get; set; } = null!;

    [ForeignKey(nameof(ClassId))]
    public virtual Class Class { get; set; } = null!;

    [ForeignKey(nameof(SemesterId))]
    public virtual Semester Semester { get; set; } = null!;

    public virtual ICollection<Assessment> Assessments { get; set; } = new List<Assessment>();
    public virtual ICollection<TimetableEntry> TimetableEntries { get; set; } = new List<TimetableEntry>();
}
