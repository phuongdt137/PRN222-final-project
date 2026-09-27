using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HighSchoolManagementSystem.Models.Entities;

[Table("classes")]
public class Class
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("class_name")]
    public string ClassName { get; set; } = null!;

    [Column("homeroom_teacher_id")]
    public int? HomeroomTeacherId { get; set; }

    [Column("academic_year_id")]
    public int AcademicYearId { get; set; }

    [Column("grade_level_id")]
    public int GradeLevelId { get; set; }

    // Navigation properties
    [ForeignKey(nameof(HomeroomTeacherId))]
    public virtual Teacher? HomeroomTeacher { get; set; }

    [ForeignKey(nameof(AcademicYearId))]
    public virtual AcademicYear AcademicYear { get; set; } = null!;

    [ForeignKey(nameof(GradeLevelId))]
    public virtual GradeLevel GradeLevel { get; set; } = null!;

    public virtual ICollection<StudentClass> StudentClasses { get; set; } = new List<StudentClass>();
    public virtual ICollection<TeachingAssignment> TeachingAssignments { get; set; } = new List<TeachingAssignment>();
}
