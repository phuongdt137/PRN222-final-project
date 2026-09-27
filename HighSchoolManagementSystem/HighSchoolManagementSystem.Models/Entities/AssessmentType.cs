using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HighSchoolManagementSystem.Models.Entities;

[Table("assessment_types")]
public class AssessmentType
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("type_code")]
    public string TypeCode { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    [Column("type_name")]
    public string TypeName { get; set; } = null!;

    [Column("weight", TypeName = "decimal(5,2)")]
    public decimal Weight { get; set; } = 1.0m;

    [Column("min_entries")]
    public int MinEntries { get; set; } = 1;

    [Column("max_entries")]
    public int MaxEntries { get; set; } = 5;

    // Navigation properties
    public virtual ICollection<Assessment> Assessments { get; set; } = new List<Assessment>();
}
