using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HighSchoolManagementSystem.Models.Entities;

[Table("timetable_entries")]
public class TimetableEntry
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("teaching_assignment_id")]
    public int TeachingAssignmentId { get; set; }

    [Column("room_id")]
    public int RoomId { get; set; }

    [Column("time_slot_id")]
    public int TimeSlotId { get; set; }

    [Column("day_of_week")]
    public int DayOfWeek { get; set; } // 2..7 (Thứ 2..Thứ 7) hoặc 1..7 (Chủ nhật..Thứ 7)

    // Navigation properties
    [ForeignKey(nameof(TeachingAssignmentId))]
    public virtual TeachingAssignment TeachingAssignment { get; set; } = null!;

    [ForeignKey(nameof(RoomId))]
    public virtual Room Room { get; set; } = null!;

    [ForeignKey(nameof(TimeSlotId))]
    public virtual TimeSlot TimeSlot { get; set; } = null!;

    public virtual ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
}
