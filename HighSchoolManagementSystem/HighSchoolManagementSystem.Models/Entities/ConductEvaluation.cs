using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HighSchoolManagementSystem.Models.Entities;

[Table("conduct_evaluations")]
public class ConductEvaluation
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("student_id")]
    public int StudentId { get; set; }

    [Column("semester_id")]
    public int SemesterId { get; set; }

    [Column("homeroom_teacher_id")]
    public int HomeroomTeacherId { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("conduct_grade")]
    public string ConductGrade { get; set; } = null!; // Tốt, Khá, Trung bình, Yếu

    [MaxLength(500)]
    [Column("teacher_comment")]
    public string? TeacherComment { get; set; }

    [Column("evaluated_at")]
    public DateTime EvaluatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(StudentId))]
    public virtual Student Student { get; set; } = null!;

    [ForeignKey(nameof(SemesterId))]
    public virtual Semester Semester { get; set; } = null!;

    [ForeignKey(nameof(HomeroomTeacherId))]
    public virtual Teacher HomeroomTeacher { get; set; } = null!;
}
