using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HighSchoolManagementSystem.MVC.Models.ViewModels.Score;
using HighSchoolManagementSystem.Repositories.Interfaces;
using HighSchoolManagementSystem.Services.Interfaces;

namespace HighSchoolManagementSystem.MVC.Controllers;

[Authorize(Roles = "Student")]
public class StudentController : Controller
{
    private readonly IStudentRepository _studentRepository;
    private readonly IAcademicYearRepository _academicYearRepository;
    private readonly IScoreService _scoreService;
    private readonly ILogger<StudentController> _logger;

    public StudentController(
        IStudentRepository studentRepository,
        IAcademicYearRepository academicYearRepository,
        IScoreService scoreService,
        ILogger<StudentController> logger)
    {
        _studentRepository = studentRepository;
        _academicYearRepository = academicYearRepository;
        _scoreService = scoreService;
        _logger = logger;
    }

    public IActionResult Dashboard()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Scores(int? semesterId, int? subjectId)
    {
        var student = await ResolveCurrentStudentAsync();
        if (student == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy hồ sơ học sinh của tài khoản này.";
            return RedirectToAction(nameof(Dashboard));
        }

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

        int targetSemesterId = semesterId ?? (semestersList.FirstOrDefault(s => s.IsActive)?.Id ?? (semestersList.FirstOrDefault()?.Id ?? 0));

        var model = new StudentTranscriptViewModel
        {
            StudentId = student.Id,
            SelectedSemesterId = targetSemesterId,
            SelectedSubjectId = subjectId,
            AvailableSemesters = semestersList
        };

        if (targetSemesterId > 0)
        {
            var transcriptResult = await _scoreService.GetStudentTranscriptAsync(student.Id, targetSemesterId, student.UserId);
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

    private async Task<HighSchoolManagementSystem.Models.Entities.Student?> ResolveCurrentStudentAsync()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdValue, out var userId))
        {
            return null;
        }

        return await _studentRepository.GetByUserIdAsync(userId);
    }
}
