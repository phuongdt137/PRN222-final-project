using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HighSchoolManagementSystem.Models.Entities;

[Table("rooms")]
public class Room
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("room_code")]
    public string RoomCode { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    [Column("room_name")]
    public string RoomName { get; set; } = null!;

    [MaxLength(50)]
    [Column("building")]
    public string? Building { get; set; }

    [Required]
    [MaxLength(30)]
    [Column("room_type")]
    public string RoomType { get; set; } = "Classroom";

    [Column("capacity")]
    public int Capacity { get; set; } = 40;

    [Required]
    [MaxLength(30)]
    [Column("status")]
    public string Status { get; set; } = "Available";

    // Navigation properties
    public virtual ICollection<TimetableEntry> TimetableEntries { get; set; } = new List<TimetableEntry>();
}
