using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HighSchoolManagementSystem.MVC.Models.ViewModels.Score;
using HighSchoolManagementSystem.MVC.Models.ViewModels.Parent;
using HighSchoolManagementSystem.Repositories.Interfaces;
using HighSchoolManagementSystem.Services.Interfaces;
using HighSchoolManagementSystem.Services.Common;
using HighSchoolManagementSystem.Services.DTOs;

namespace HighSchoolManagementSystem.MVC.Controllers;

[Authorize(Roles = "Parent")]
public class ParentController : Controller
{
    private readonly IParentRepository _parentRepository;
    private readonly IAcademicYearRepository _academicYearRepository;
    private readonly IScoreService _scoreService;
    private readonly ILogger<ParentController> _logger;
    private readonly IAttendanceService _attendanceService;

    public ParentController(
        IParentRepository parentRepository,
        IAcademicYearRepository academicYearRepository,
        IScoreService scoreService,
        ILogger<ParentController> logger,
        IAttendanceService attendanceService)
    {
        _parentRepository = parentRepository;
        _academicYearRepository = academicYearRepository;
        _scoreService = scoreService;
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

        return View(new ParentDashboardViewModel
        {
            AttendanceThisMonth = month.Success ? month.Data : null,
            AttendanceToday = daily.Success ? daily.Data : null,
            AttendanceMonthError = month.Success && month.Data != null ? null : month.Message ?? "Không thể tải chuyên cần tháng này.",
            AttendanceTodayError = daily.Success && daily.Data != null ? null : daily.Message ?? "Không thể tải điểm danh hôm nay."
        });
    }

    [HttpGet]
    public async Task<IActionResult> StudentScores(int? studentId, int? semesterId, int? subjectId)
    {
        var parent = await ResolveCurrentParentAsync();
        if (parent == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy hồ sơ phụ huynh của tài khoản này.";
            return RedirectToAction(nameof(Dashboard));
        }

        var children = parent.Students.ToList();
        var allYears = await _academicYearRepository.GetAllAsync();

        var semestersList = new List<SemesterItemOption>();
        foreach (var yr in allYears)
        {
            foreach (var sem in yr.Semesters)
            {
                semestersList.Add(new SemesterItemOption
                {
                    Id = sem.Id,
                    SemesterName = sem.SemesterName,
                    AcademicYearCode = yr.YearCode,
                    IsActive = sem.IsActive
                });
            }
        }

        var model = new ParentScoresViewModel
        {
            Children = children.Select(c => new ParentStudentOption
            {
                Id = c.Id,
                StudentCode = c.StudentCode,
                FullName = c.User?.FullName ?? string.Empty
            }).ToList(),
            AvailableSemesters = semestersList
        };

        if (!children.Any())
        {
            return View(model);
        }

        int targetStudentId = studentId ?? children.First().Id;
        if (!children.Any(c => c.Id == targetStudentId))
        {
            targetStudentId = children.First().Id;
        }

        int targetSemesterId = semesterId ?? (semestersList.FirstOrDefault(s => s.IsActive)?.Id ?? (semestersList.FirstOrDefault()?.Id ?? 0));

        model.SelectedStudentId = targetStudentId;
        model.SelectedSemesterId = targetSemesterId;
        model.SelectedSubjectId = subjectId;

        if (targetSemesterId > 0)
        {
            var transcriptResult = await _scoreService.GetStudentTranscriptAsync(targetStudentId, targetSemesterId, parent.UserId);
            if (transcriptResult.Success)
            {
                model.Transcript = transcriptResult.Data;
                if (subjectId.HasValue &&
                    !(model.Transcript?.Subjects.Any(s => s.SubjectId == subjectId.Value) ?? false))
                {
                    model.SelectedSubjectId = null;
                }
            }
            else
            {
                TempData["ErrorMessage"] = transcriptResult.Message;
            }
        }

        return View(model);
    }

    private async Task<HighSchoolManagementSystem.Models.Entities.Parent?> ResolveCurrentParentAsync()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdValue, out var userId))
        {
            return null;
        }

        return await _parentRepository.GetByUserIdAsync(userId);
    }
}
