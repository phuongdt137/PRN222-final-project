using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HighSchoolManagementSystem.Models.Entities;

[Table("parents")]
public class Parent
{
    [Key]
    [Column("parent_id")]
    public int ParentId { get; set; }

    [MaxLength(100)]
    [Column("occupation")]
    public string? Occupation { get; set; }

    [MaxLength(50)]
    [Column("emergency_contact")]
    public string? EmergencyContact { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;

    public virtual ICollection<Student> Students { get; set; } = new List<Student>();
}
