using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HighSchoolManagementSystem.Models.Entities;

[Table("attendance_records")]
public class AttendanceRecord
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("student_id")]
    public int StudentId { get; set; }

    [Column("recorded_by")]
    public int RecordedBy { get; set; }

    [Column("leave_request_id")]
    public int? LeaveRequestId { get; set; }

    [Column("timetable_entry_id")]
    public int TimetableEntryId { get; set; }

    [Column("attendance_date")]
    public DateTime AttendanceDate { get; set; }

    [Required]
    [MaxLength(30)]
    [Column("status")]
    public string Status { get; set; } = "Present"; // Present, Absent, Late, Excused

    [MaxLength(255)]
    [Column("comment")]
    public string? Comment { get; set; }

    // Navigation properties
    [ForeignKey(nameof(StudentId))]
    public virtual Student Student { get; set; } = null!;

    [ForeignKey(nameof(RecordedBy))]
    public virtual Teacher RecordedByTeacher { get; set; } = null!;

    [ForeignKey(nameof(LeaveRequestId))]
    public virtual LeaveRequest? LeaveRequest { get; set; }

    [ForeignKey(nameof(TimetableEntryId))]
    public virtual TimetableEntry TimetableEntry { get; set; } = null!;
}
