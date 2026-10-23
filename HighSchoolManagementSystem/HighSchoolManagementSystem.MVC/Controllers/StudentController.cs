using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HighSchoolManagementSystem.MVC.Models.ViewModels.Score;
using HighSchoolManagementSystem.MVC.Models.ViewModels.Student;
using HighSchoolManagementSystem.Services.DTOs;
using HighSchoolManagementSystem.Services.Interfaces;

namespace HighSchoolManagementSystem.MVC.Controllers;

[Authorize(Roles = "Student")]
public class StudentController : Controller
{
    private readonly IStudentService _studentService;
    private readonly ILogger<StudentController> _logger;

    public StudentController(
        IStudentService studentService,
        ILogger<StudentController> logger)
    {
        _studentService = studentService;
        _logger = logger;
    }

    public IActionResult Dashboard()
    {
        return View();
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
