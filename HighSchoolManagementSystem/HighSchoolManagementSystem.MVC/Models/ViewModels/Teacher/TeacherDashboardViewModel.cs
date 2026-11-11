using HighSchoolManagementSystem.Services.DTOs;

namespace HighSchoolManagementSystem.MVC.Models.ViewModels.Teacher;

public class TeacherDashboardViewModel
{
    public AttendanceListDto? AttendanceToday { get; set; }
    public string? AttendanceError { get; set; }
}
