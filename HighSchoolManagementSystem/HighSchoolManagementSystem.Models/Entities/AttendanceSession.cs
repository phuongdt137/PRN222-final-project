using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HighSchoolManagementSystem.Models.Entities;

/// <summary>A dated lesson and its frozen attendance roster.</summary>
[Table("attendance_sessions")]
public class AttendanceSession
{
    [Key, Column("id")] public int Id { get; set; }
    [Column("timetable_entry_id")] public int TimetableEntryId { get; set; }
    [Column("attendance_date", TypeName = "date")] public DateTime AttendanceDate { get; set; }
    [Column("class_id")] public int ClassId { get; set; }
    [Column("subject_id")] public int SubjectId { get; set; }
    [Column("semester_id")] public int SemesterId { get; set; }
    [Column("assigned_teacher_id")] public int AssignedTeacherId { get; set; }
    [Column("time_slot_id")] public int TimeSlotId { get; set; }
    [Column("room_id")] public int RoomId { get; set; }
    [Required, MaxLength(20), Column("slot_code")] public string SlotCode { get; set; } = "";
    [Required, MaxLength(20), Column("school_session")] public string SchoolSession { get; set; } = "";
    [Column("period_no")] public int PeriodNo { get; set; }
    [Column("start_time")] public TimeSpan StartTime { get; set; }
    [Column("end_time")] public TimeSpan EndTime { get; set; }
    [Required, MaxLength(20), Column("status")] public string Status { get; set; } = AttendanceStatuses.Open;
    // Imported records have no reliable creator or submission timestamp.
    [Column("created_by_user_id")] public int? CreatedByUserId { get; set; }
    [Column("created_at")] public DateTime? CreatedAt { get; set; }
    [Column("submitted_at")] public DateTime? SubmittedAt { get; set; }
    [Column("updated_at")] public DateTime? UpdatedAt { get; set; }
    [Timestamp, Column("row_version")] public byte[] RowVersion { get; set; } = [];

    public TimetableEntry TimetableEntry { get; set; } = null!;
    public Class Class { get; set; } = null!;
    public Subject Subject { get; set; } = null!;
    public Semester Semester { get; set; } = null!;
    public Teacher AssignedTeacher { get; set; } = null!;
    public TimeSlot TimeSlot { get; set; } = null!;
    public Room Room { get; set; } = null!;
    public User? CreatedByUser { get; set; }
    public ICollection<AttendanceRecord> Records { get; set; } = new List<AttendanceRecord>();
}

public static class AttendanceStatuses
{
    public const string Open = "Open";
    public const string Submitted = "Submitted";
    public const string Cancelled = "Cancelled";
    public const string NotMarked = "NotMarked";
    public const string Present = "Present";
    public const string Absent = "Absent";
    public const string Late = "Late";
    public const string Excused = "Excused";

    public static bool IsResult(string? value) => value is Present or Absent or Late or Excused;
    public static string Label(string? value) => value switch
    {
        Open => "Chưa điểm danh", Submitted => "Đã điểm danh", Cancelled => "Đã hủy",
        Present => "Có mặt", Absent => "Vắng không phép", Late => "Đi muộn",
        Excused => "Vắng có phép", _ => "Chưa có kết quả"
    };
}
