using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.MVC.Models.ViewModels.Assessment;
using HighSchoolManagementSystem.Repositories.Interfaces;
using HighSchoolManagementSystem.Services.Common;
using HighSchoolManagementSystem.Services.DTOs;
using HighSchoolManagementSystem.Services.Interfaces;

namespace HighSchoolManagementSystem.MVC.Controllers;

[Authorize(Roles = "Teacher")]
public class AssessmentController : Controller
{
    private readonly IAssessmentService _assessmentService;
    private readonly ITeacherRepository _teacherRepository;
    private readonly ILogger<AssessmentController> _logger;

    public AssessmentController(
        IAssessmentService assessmentService,
        ITeacherRepository teacherRepository,
        ILogger<AssessmentController> logger)
    {
        _assessmentService = assessmentService;
        _teacherRepository = teacherRepository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? assignmentId, string? search)
    {
        var teacher = await ResolveCurrentTeacherAsync();
        if (teacher == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy hồ sơ giáo viên gắn với tài khoản này.";
            return RedirectToAction("Dashboard", "Teacher");
        }

        var assignmentsResult = await _assessmentService.GetTeacherAssignmentsAsync(teacher.Id);
        var assignments = assignmentsResult.Data?.ToList() ?? new();

        if (!assignments.Any())
        {
            var emptyModel = new AssessmentListViewModel
            {
                Assignments = new(),
                Assessments = new()
            };
            return View(emptyModel);
        }

        int selectedAssignmentId = assignmentId ?? assignments.First().Id;
        var selectedAssignment = assignments.FirstOrDefault(a => a.Id == selectedAssignmentId) ?? assignments.First();
        selectedAssignmentId = selectedAssignment.Id;

        var assessmentsResult = await _assessmentService.GetAssessmentsByAssignmentAsync(selectedAssignmentId, teacher.Id);
        var assessmentsList = assessmentsResult.Data?.ToList() ?? new();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            assessmentsList = assessmentsList.Where(a =>
                a.AssessmentName.ToLower().Contains(term) ||
                a.AssessmentTypeName.ToLower().Contains(term)).ToList();
        }

        var model = new AssessmentListViewModel
        {
            AssignmentId = selectedAssignmentId,
            SearchTerm = search,
            Assignments = assignments,
            SelectedAssignment = selectedAssignment,
            Assessments = assessmentsList
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? assignmentId)
    {
        var teacher = await ResolveCurrentTeacherAsync();
        if (teacher == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy hồ sơ giáo viên gắn với tài khoản này.";
            return RedirectToAction("Dashboard", "Teacher");
        }

        var assignmentsResult = await _assessmentService.GetTeacherAssignmentsAsync(teacher.Id);
        var assignments = assignmentsResult.Data?.ToList() ?? new();

        if (!assignments.Any())
        {
            TempData["ErrorMessage"] = "Bạn chưa được phân công giảng dạy lớp nào để tạo bài kiểm tra.";
            return RedirectToAction(nameof(Index));
        }

        var typesResult = await _assessmentService.GetAssessmentTypesAsync();
        var types = typesResult.Data?.ToList() ?? new();

        int targetAssignmentId = assignmentId ?? assignments.First().Id;
        var selectedAssignment = assignments.FirstOrDefault(a => a.Id == targetAssignmentId) ?? assignments.First();

        var model = new CreateAssessmentViewModel
        {
            TeachingAssignmentId = selectedAssignment.Id,
            SelectedAssignment = selectedAssignment,
            AvailableAssignments = assignments,
            AvailableTypes = types,
            ConductedAt = DateTime.Today
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateAssessmentViewModel model)
    {
        var teacher = await ResolveCurrentTeacherAsync();
        if (teacher == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy hồ sơ giáo viên gắn với tài khoản này.";
            return RedirectToAction("Dashboard", "Teacher");
        }

        if (!ModelState.IsValid)
        {
            await PopulateCreateViewModelDropdownsAsync(model, teacher.Id);
            return View(model);
        }

        var dto = new CreateAssessmentDto
        {
            TeachingAssignmentId = model.TeachingAssignmentId,
            AssessmentTypeId = model.AssessmentTypeId,
            AssessmentName = model.AssessmentName,
            ConductedAt = model.ConductedAt
        };

        var result = await _assessmentService.CreateAssessmentAsync(dto, teacher.Id);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Đã xảy ra lỗi khi tạo bài kiểm tra.");
            await PopulateCreateViewModelDropdownsAsync(model, teacher.Id);
            return View(model);
        }

        TempData["SuccessMessage"] = $"Tạo bài kiểm tra \"{result.Data!.AssessmentName}\" thành công!";
        return RedirectToAction(nameof(Index), new { assignmentId = model.TeachingAssignmentId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var teacher = await ResolveCurrentTeacherAsync();
        if (teacher == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy hồ sơ giáo viên gắn với tài khoản này.";
            return RedirectToAction("Dashboard", "Teacher");
        }

        var result = await _assessmentService.GetAssessmentByIdAsync(id, teacher.Id);
        if (!result.Success || result.Data == null)
        {
            if (result.ErrorCode == ErrorCodes.AuthForbidden)
            {
                return Forbid();
            }

            TempData["ErrorMessage"] = result.Message ?? "Không tìm thấy bài kiểm tra yêu cầu.";
            return RedirectToAction(nameof(Index));
        }

        var typesResult = await _assessmentService.GetAssessmentTypesAsync();
        var types = typesResult.Data?.ToList() ?? new();

        var assessment = result.Data;
        var model = new EditAssessmentViewModel
        {
            Id = assessment.Id,
            TeachingAssignmentId = assessment.TeachingAssignmentId,
            AssessmentTypeId = assessment.AssessmentTypeId,
            AssessmentName = assessment.AssessmentName,
            ConductedAt = assessment.ConductedAt,
            AssessmentDetails = assessment,
            AvailableTypes = types,
            HasLockedScores = assessment.HasLockedScores
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EditAssessmentViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        var teacher = await ResolveCurrentTeacherAsync();
        if (teacher == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy hồ sơ giáo viên gắn với tài khoản này.";
            return RedirectToAction("Dashboard", "Teacher");
        }

        if (!ModelState.IsValid)
        {
            await PopulateEditViewModelAsync(model, teacher.Id);
            return View(model);
        }

        var dto = new UpdateAssessmentDto
        {
            Id = model.Id,
            AssessmentTypeId = model.AssessmentTypeId,
            AssessmentName = model.AssessmentName,
            ConductedAt = model.ConductedAt
        };

        var result = await _assessmentService.UpdateAssessmentAsync(dto, teacher.Id);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Đã xảy ra lỗi khi cập nhật bài kiểm tra.");
            await PopulateEditViewModelAsync(model, teacher.Id);
            return View(model);
        }

        TempData["SuccessMessage"] = $"Cập nhật bài kiểm tra \"{result.Data!.AssessmentName}\" thành công!";
        return RedirectToAction(nameof(Index), new { assignmentId = result.Data.TeachingAssignmentId });
    }

    private async Task<Teacher?> ResolveCurrentTeacherAsync()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdValue, out var userId))
        {
            return null;
        }

        return await _teacherRepository.GetByUserIdAsync(userId);
    }

    private async Task PopulateCreateViewModelDropdownsAsync(CreateAssessmentViewModel model, int teacherId)
    {
        var assignmentsResult = await _assessmentService.GetTeacherAssignmentsAsync(teacherId);
        model.AvailableAssignments = assignmentsResult.Data?.ToList() ?? new();
        model.SelectedAssignment = model.AvailableAssignments.FirstOrDefault(a => a.Id == model.TeachingAssignmentId);

        var typesResult = await _assessmentService.GetAssessmentTypesAsync();
        model.AvailableTypes = typesResult.Data?.ToList() ?? new();
    }

    private async Task PopulateEditViewModelAsync(EditAssessmentViewModel model, int teacherId)
    {
        var detailResult = await _assessmentService.GetAssessmentByIdAsync(model.Id, teacherId);
        model.AssessmentDetails = detailResult.Data;
        model.HasLockedScores = detailResult.Data?.HasLockedScores ?? false;

        var typesResult = await _assessmentService.GetAssessmentTypesAsync();
        model.AvailableTypes = typesResult.Data?.ToList() ?? new();
    }
}
