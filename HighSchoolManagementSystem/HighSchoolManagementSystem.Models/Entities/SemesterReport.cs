using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HighSchoolManagementSystem.Models.Entities;

[Table("semester_reports")]
public class SemesterReport
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("student_id")]
    public int StudentId { get; set; }

    [Column("semester_id")]
    public int SemesterId { get; set; }

    [Column("gpa", TypeName = "decimal(5,2)")]
    public decimal? Gpa { get; set; }

    [MaxLength(20)]
    [Column("conduct_grade")]
    public string? ConductGrade { get; set; }

    [MaxLength(30)]
    [Column("academic_rank")]
    public string? AcademicRank { get; set; } // Giỏi, Khá, Trung bình, Yếu

    [Column("class_rank")]
    public int? ClassRank { get; set; }

    [MaxLength(500)]
    [Column("teacher_comments")]
    public string? TeacherComments { get; set; }

    // Navigation properties
    [ForeignKey(nameof(StudentId))]
    public virtual Student Student { get; set; } = null!;

    [ForeignKey(nameof(SemesterId))]
    public virtual Semester Semester { get; set; } = null!;
}
