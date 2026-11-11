using HighSchoolManagementSystem.Services.DTOs;

namespace HighSchoolManagementSystem.MVC.Models.ViewModels.Student;

public class StudentDashboardViewModel
{
    public AttendanceStudentHistoryDto? AttendanceThisMonth { get; set; }
    public AttendanceStudentHistoryDto? AttendanceToday { get; set; }
    public string? AttendanceMonthError { get; set; }
    public string? AttendanceTodayError { get; set; }
}
