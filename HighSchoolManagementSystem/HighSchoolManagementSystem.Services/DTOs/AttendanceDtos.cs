using System.ComponentModel.DataAnnotations;
using HighSchoolManagementSystem.Models.Entities;

namespace HighSchoolManagementSystem.Services.DTOs;

public record AttendanceOption(int Id, string Name);
public class AttendanceFilter
{
    public int? SemesterId { get; set; }
    public int? ClassId { get; set; }
    public int? SubjectId { get; set; }
    public int? StudentId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
}
public class AttendanceSessionDto
{
    public int Id { get; set; }
    public int TimetableEntryId { get; set; }
    public DateTime Date { get; set; }
    public string ClassName { get; set; } = "";
    public string SubjectName { get; set; } = "";
    public string TeacherName { get; set; } = "";
    public string RoomName { get; set; } = "";
    public string Slot { get; set; } = "";
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string Status { get; set; } = AttendanceStatuses.Open;
    public int StudentCount { get; set; }
    public bool CanWrite { get; set; }
    public string RowVersion { get; set; } = "";
}
public class AttendanceListDto
{
    public AttendanceFilter Filter { get; set; } = new();
    public List<AttendanceSessionDto> Items { get; set; } = [];
    public List<AttendanceOption> Semesters { get; set; } = [];
    public List<AttendanceOption> Classes { get; set; } = [];
    public int TotalCount { get; set; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / 25m));
    public bool IsStaff { get; set; }
}
public class AttendanceCreateDto
{
    public string? EmptyMessage { get; set; }
    [DataType(DataType.Date)] public DateTime Date { get; set; }
    public int? SemesterId { get; set; }
    public int? ClassId { get; set; }
    public List<int> EntryIds { get; set; } = [];
    public List<AttendanceSessionDto> Entries { get; set; } = [];
    public List<AttendanceOption> Semesters { get; set; } = [];
    public List<AttendanceOption> Classes { get; set; } = [];
}
public class AttendanceStudentDto
{
    public int StudentId { get; set; }
    public string StudentCode { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Status { get; set; } = AttendanceStatuses.NotMarked;
    public string? Comment { get; set; }
    public int? LeaveRequestId { get; set; }
    public int? ApprovedLeaveId { get; set; }
}
public class AttendanceSheetDto
{
    public AttendanceSessionDto Session { get; set; } = new();
    public List<AttendanceStudentDto> Students { get; set; } = [];
    public bool IsStaff { get; set; }
    public bool RosterChanged { get; set; }
    public bool ScheduleChanged { get; set; }
    public string? ReadOnlyReason { get; set; }
    public List<AttendanceAuditDto> Audit { get; set; } = [];
}
public record AttendanceAuditDto(DateTime At, string Actor, string Action);
public class AttendanceSaveDto
{
    public int SessionId { get; set; }
    [Required] public string RowVersion { get; set; } = "";
    public List<AttendanceSaveRowDto> Students { get; set; } = [];
    [MaxLength(500)] public string? Reason { get; set; }
}
public class AttendanceSaveRowDto
{
    public int StudentId { get; set; }
    [Required] public string Status { get; set; } = "";
    [MaxLength(255)] public string? Comment { get; set; }
    public int? LeaveRequestId { get; set; }
}
public class AttendanceStudentHistoryDto
{
    public AttendanceFilter Filter { get; set; } = new();
    public List<AttendanceOption> Semesters { get; set; } = [];
    public List<AttendanceOption> Subjects { get; set; } = [];
    public List<AttendanceOption> Children { get; set; } = [];
    public string StudentName { get; set; } = "";
    public bool IsParent { get; set; }
    public List<AttendanceHistoryRowDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / 25m));
    public int Present { get; set; }
    public int Absent { get; set; }
    public int Late { get; set; }
    public int Excused { get; set; }
    public int Pending { get; set; }
    public int Recorded => Present + Absent + Late + Excused;
    public decimal? Rate => Common.AttendanceRules.Rate(Present, Late, Recorded);
}
public class AttendanceHistoryRowDto
{
    public AttendanceSessionDto Session { get; set; } = new();
    public string Status { get; set; } = AttendanceStatuses.NotMarked;
    public string? Comment { get; set; }
    public string? RecordedBy { get; set; }
}
