using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HighSchoolManagementSystem.Models.Entities;

[Table("time_slots")]
public class TimeSlot
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("slot_code")]
    public string SlotCode { get; set; } = null!;

    [Column("period_no")]
    public int PeriodNo { get; set; }

    [Column("start_time")]
    public TimeSpan StartTime { get; set; }

    [Column("end_time")]
    public TimeSpan EndTime { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("session")]
    public string Session { get; set; } = "Morning"; // Morning / Afternoon

    // Navigation properties
    public virtual ICollection<TimetableEntry> TimetableEntries { get; set; } = new List<TimetableEntry>();
}
