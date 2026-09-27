using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HighSchoolManagementSystem.Models.Entities;

[Table("academic_years")]
public class AcademicYear
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("year_code")]
    public string YearCode { get; set; } = null!;

    [Column("start_date")]
    public DateTime StartDate { get; set; }

    [Column("end_date")]
    public DateTime EndDate { get; set; }

    // Navigation properties
    public virtual ICollection<Semester> Semesters { get; set; } = new List<Semester>();
    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();
}
