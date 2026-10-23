using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HighSchoolManagementSystem.MVC.Models.ViewModels.Timetable;

public class TimetableListViewModel
{
    public IReadOnlyList<TimetableItemViewModel> Entries { get; set; } = [];
    public IEnumerable<SelectListItem> TeacherOptions { get; set; } = [];
    public IEnumerable<SelectListItem> SemesterOptions { get; set; } = [];
    public int? SelectedTeacherId { get; set; }
    public int? SelectedSemesterId { get; set; }
    public bool EditMode { get; set; }
    public DateTime WeekStart { get; set; }
    public IReadOnlyDictionary<int, string> PeriodTimes { get; set; } = new Dictionary<int, string>();
    public IReadOnlyDictionary<int, int> PeriodSlotIds { get; set; } = new Dictionary<int, int>();
}

public class TimetableItemViewModel
{
    public int Id { get; set; }
    public int DayOfWeek { get; set; }
    public int PeriodNo { get; set; }
    public string TimeRange { get; set; } = "";
    public string ClassName { get; set; } = "";
    public string SubjectName { get; set; } = "";
    public string TeacherName { get; set; } = "";
    public string RoomName { get; set; } = "";
}

public class TimetableEditViewModel
{
    public int? Id { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn phân công giảng dạy.")] public int TeachingAssignmentId { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn phòng học.")] public int RoomId { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn khung giờ.")] public int TimeSlotId { get; set; }
    [Range(2, 7, ErrorMessage = "Ngày học phải từ Thứ 2 đến Thứ 7.")] public int DayOfWeek { get; set; } = 2;
    public int? SelectedTeacherId { get; set; }
    public int? SelectedSemesterId { get; set; }
    public DateTime? WeekStart { get; set; }
    public string TeacherDisplayName { get; set; } = "";
    public IEnumerable<SelectListItem> AssignmentOptions { get; set; } = [];
    public IEnumerable<SelectListItem> RoomOptions { get; set; } = [];
    public IEnumerable<SelectListItem> TimeSlotOptions { get; set; } = [];
}
