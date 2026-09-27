using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HighSchoolManagementSystem.Models.Entities;

[Table("teacher_feedbacks")]
public class TeacherFeedback
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("student_id")]
    public int StudentId { get; set; }

    [Column("teacher_id")]
    public int TeacherId { get; set; }

    [Column("semester_id")]
    public int SemesterId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("feedback_type")]
    public string FeedbackType { get; set; } = "Academic"; // Academic, Behavior, General

    [Required]
    [MaxLength(1000)]
    [Column("content")]
    public string Content { get; set; } = null!;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(StudentId))]
    public virtual Student Student { get; set; } = null!;

    [ForeignKey(nameof(TeacherId))]
    public virtual Teacher Teacher { get; set; } = null!;

    [ForeignKey(nameof(SemesterId))]
    public virtual Semester Semester { get; set; } = null!;
}
