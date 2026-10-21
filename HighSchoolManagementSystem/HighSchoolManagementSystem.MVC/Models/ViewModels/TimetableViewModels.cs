using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HighSchoolManagementSystem.MVC.Models.ViewModels;

public class TimetableListViewModel
{
    public IReadOnlyList<TimetableItemViewModel> Entries { get; set; } = [];
    public IReadOnlyList<TimetableSlotViewModel> Slots { get; set; } = [];
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
    public int TimeSlotId { get; set; }
    public int ClassId { get; set; }
    public int DayOfWeek { get; set; }
    public int PeriodNo { get; set; }
    public string TimeRange { get; set; } = "";
    public string ClassName { get; set; } = "";
    public string SubjectName { get; set; } = "";
    public string TeacherName { get; set; } = "";
    public string RoomName { get; set; } = "";
}

public class TimetableSlotViewModel
{
    public int Id { get; set; }
    public int PeriodNo { get; set; }
    public string Session { get; set; } = "";
    public string SlotCode { get; set; } = "";
    public string TimeRange { get; set; } = "";
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

public class AssessmentConfigurationViewModel
{
    public IReadOnlyList<AssessmentConfigurationItemViewModel> Items { get; set; } = [];
}

public class AssessmentConfigurationItemViewModel
{
    public int Id { get; set; }
    public string TypeCode { get; set; } = "";
    public string TypeName { get; set; } = "";
    public decimal Weight { get; set; }
    public int MinEntries { get; set; }
    public int MaxEntries { get; set; }
    public int AssessmentCount { get; set; }
    public decimal MinScore { get; set; }
    public decimal MaxScore { get; set; }
}

public class AssessmentConfigurationEditViewModel
{
    public int? Id { get; set; }
    [Required, StringLength(20)] public string TypeCode { get; set; } = "";
    [Required, StringLength(50)] public string TypeName { get; set; } = "";
    [Range(typeof(decimal), "0.01", "100")] public decimal Weight { get; set; } = 1;
    [Range(0, 100)] public int MinEntries { get; set; } = 1;
    [Range(0, 100)] public int MaxEntries { get; set; } = 5;
    [Range(typeof(decimal), "0", "10")] public decimal MinScore { get; set; } = 0;
    [Range(typeof(decimal), "0", "10")] public decimal MaxScore { get; set; } = 10;
}
