using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HighSchoolManagementSystem.MVC.Models.ViewModels.Score;
using HighSchoolManagementSystem.MVC.Models.ViewModels.Student;
using HighSchoolManagementSystem.Services.DTOs;
using HighSchoolManagementSystem.Services.Common;
using HighSchoolManagementSystem.Services.Interfaces;

namespace HighSchoolManagementSystem.MVC.Controllers;

[Authorize(Roles = "Student")]
public class StudentController : Controller
{
    private readonly IStudentService _studentService;
    private readonly ILogger<StudentController> _logger;
    private readonly IAttendanceService _attendanceService;

    public StudentController(
        IStudentService studentService,
        ILogger<StudentController> logger,
        IAttendanceService attendanceService)
    {
        _studentService = studentService;
        _logger = logger;
        _attendanceService = attendanceService;
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) || userId <= 0)
            return Forbid();

        var today = SchoolDate.Today;
        var month = await _attendanceService.GetHistoryAsync(userId, new AttendanceFilter
        {
            FromDate = new DateTime(today.Year, today.Month, 1), ToDate = today
        });
        if (month.ErrorCode == ErrorCodes.AuthForbidden) return Forbid();
        var daily = await _attendanceService.GetHistoryAsync(userId, new AttendanceFilter { FromDate = today, ToDate = today });
        if (daily.ErrorCode == ErrorCodes.AuthForbidden) return Forbid();

        return View(new StudentDashboardViewModel
        {
            AttendanceThisMonth = month.Success ? month.Data : null,
            AttendanceToday = daily.Success ? daily.Data : null,
            AttendanceMonthError = month.Success && month.Data != null ? null : month.Message ?? "Không thể tải chuyên cần tháng này.",
            AttendanceTodayError = daily.Success && daily.Data != null ? null : daily.Message ?? "Không thể tải điểm danh hôm nay."
        });
    }

    [HttpGet]
    public async Task<IActionResult> Scores(StudentScoresFilterViewModel filter)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdValue, out var userId))
        {
            TempData["ErrorMessage"] = "Không thể xác định danh tính tài khoản học sinh.";
            return RedirectToAction(nameof(Dashboard));
        }

        // 1. Controller nhận ViewModel -> Dựng DTO
        var queryDto = new StudentScoresQueryDto
        {
            UserId = userId,
            SemesterId = filter.SemesterId,
            SubjectId = filter.SubjectId
        };

        // 2. Truyền DTO vào Service xử lý (Service sẽ gọi Repository / DB)
        var result = await _studentService.GetStudentScoresAsync(queryDto);

        // 3. Service trả về ServiceResult: kiểm tra kết quả
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Dashboard));
        }

        // 4. Từ ServiceResult (DTO) map dữ liệu trả về ViewModel
        var data = result.Data!;
        var model = new StudentTranscriptViewModel
        {
            StudentId = data.StudentId,
            SelectedSemesterId = data.SelectedSemesterId,
            SelectedSubjectId = data.SelectedSubjectId,
            AvailableSemesters = data.AvailableSemesters.Select(s => new SemesterItemOption
            {
                Id = s.Id,
                SemesterName = s.SemesterName,
                AcademicYearCode = s.AcademicYearCode,
                IsActive = s.IsActive
            }).ToList(),
            Transcript = data.Transcript
        };

        // 5. Trả ViewModel cho View
        return View(model);
    }
}
