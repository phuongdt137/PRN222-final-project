using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HighSchoolManagementSystem.Models.Entities;

[Table("grade_levels")]
public class GradeLevel
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("level_code")]
    public string LevelCode { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    [Column("level_name")]
    public string LevelName { get; set; } = null!;

    // Navigation properties
    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();
}
