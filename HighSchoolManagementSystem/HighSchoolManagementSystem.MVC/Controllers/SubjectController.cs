using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HighSchoolManagementSystem.MVC.Models.ViewModels.Subject;
using HighSchoolManagementSystem.Services.DTOs;
using HighSchoolManagementSystem.Services.Interfaces;

namespace HighSchoolManagementSystem.MVC.Controllers;

[Authorize(Roles = "Staff,Principal")]
public class SubjectController : Controller
{
    private readonly ISubjectService _subjectService;
    private readonly ITeacherService _teacherService;

    public SubjectController(ISubjectService subjectService, ITeacherService teacherService)
    {
        _subjectService = subjectService;
        _teacherService = teacherService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? evaluationType, string? status)
    {
        bool? isActiveFilter = status switch
        {
            "Active" => true,
            "Inactive" => false,
            _ => null
        };

        var allResult = await _subjectService.GetAllSubjectsAsync();
        var allList = allResult.Data?.ToList() ?? new();

        int totalCount = allList.Count;
        int activeCount = allList.Count(s => s.IsActive);
        int inactiveCount = allList.Count(s => !s.IsActive);
        int scoreCount = allList.Count(s => s.EvaluationType == "Score");
        int passFailCount = allList.Count(s => s.EvaluationType == "PassFail");

        var filterResult = await _subjectService.GetAllSubjectsAsync(search, evaluationType, isActiveFilter);
        var filteredList = filterResult.Data?.ToList() ?? new();

        var viewModel = new SubjectListViewModel
        {
            SearchTerm = search,
            EvaluationTypeFilter = evaluationType,
            StatusFilter = status,
            TotalCount = totalCount,
            ActiveCount = activeCount,
            InactiveCount = inactiveCount,
            ScoreCount = scoreCount,
            PassFailCount = passFailCount,
            Subjects = filteredList.Select(s => new SubjectListItemViewModel
            {
                Id = s.Id,
                SubjectCode = s.SubjectCode,
                SubjectName = s.SubjectName,
                EvaluationType = s.EvaluationType,
                Description = s.Description,
                IsActive = s.IsActive,
                TotalAssignments = s.TeachingAssignments.Count
            }).ToList()
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var result = await _subjectService.GetSubjectDetailsAsync(id);
        if (!result.Success || result.Data == null)
        {
            TempData["ErrorMessage"] = result.Message ?? "Không tìm thấy thông tin môn học.";
            return RedirectToAction(nameof(Index));
        }

        var subject = result.Data;
        var viewModel = new SubjectDetailViewModel
        {
            Id = subject.Id,
            SubjectCode = subject.SubjectCode,
            SubjectName = subject.SubjectName,
            EvaluationType = subject.EvaluationType,
            Description = subject.Description,
            IsActive = subject.IsActive,
            TeachingAssignments = subject.TeachingAssignments.Select(ta => new SubjectAssignmentItemViewModel
            {
                AssignmentId = ta.Id,
                TeacherId = ta.Teacher?.Id ?? 0,
                TeacherCode = ta.Teacher?.TeacherCode ?? "N/A",
                TeacherName = ta.Teacher?.User?.FullName ?? "Chưa phân công",
                ClassName = ta.Class?.ClassName ?? "N/A",
                SemesterName = ta.Semester?.SemesterName ?? "N/A",
                Status = ta.Status
            }).ToList()
        };

        await PopulateAssignmentLookupsAsync(viewModel, subject.SubjectName);

        return View(viewModel);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateSubjectViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateSubjectViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var dto = new CreateSubjectDto
        {
            SubjectCode = model.SubjectCode,
            SubjectName = model.SubjectName,
            EvaluationType = model.EvaluationType,
            Description = model.Description,
            IsActive = model.IsActive
        };

        var result = await _subjectService.CreateSubjectAsync(dto);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Đã xảy ra lỗi khi tạo môn học.");
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var result = await _subjectService.GetSubjectByIdAsync(id);
        if (!result.Success || result.Data == null)
        {
            TempData["ErrorMessage"] = result.Message ?? "Không tìm thấy thông tin môn học cần sửa.";
            return RedirectToAction(nameof(Index));
        }

        var subject = result.Data;
        var hasScoresResult = await _subjectService.CheckSubjectHasScoresAsync(id);
        var model = new EditSubjectViewModel
        {
            Id = subject.Id,
            SubjectCode = subject.SubjectCode,
            SubjectName = subject.SubjectName,
            EvaluationType = subject.EvaluationType,
            Description = subject.Description,
            IsActive = subject.IsActive,
            HasScores = hasScoresResult.Data
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EditSubjectViewModel model)
    {
        if (id != model.Id)
        {
            TempData["ErrorMessage"] = "Yêu cầu cập nhật không hợp lệ.";
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
        {
            model.HasScores = (await _subjectService.CheckSubjectHasScoresAsync(id)).Data;
            return View(model);
        }

        var dto = new UpdateSubjectDto
        {
            Id = model.Id,
            SubjectCode = model.SubjectCode,
            SubjectName = model.SubjectName,
            EvaluationType = model.EvaluationType,
            Description = model.Description,
            IsActive = model.IsActive
        };

        var result = await _subjectService.UpdateSubjectAsync(dto);
        if (!result.Success)
        {
            model.HasScores = (await _subjectService.CheckSubjectHasScoresAsync(id)).Data;
            ModelState.AddModelError(string.Empty, result.Message ?? "Đã xảy ra lỗi khi cập nhật môn học.");
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id, string? returnUrl)
    {
        var result = await _subjectService.ToggleSubjectStatusAsync(id);

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new { success = result.Success, message = result.Message, isActive = result.Data });
        }

        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _subjectService.DeleteSubjectAsync(id);
        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignTeacher(AssignTeacherViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Vui lòng chọn đầy đủ thông tin giáo viên, lớp học và học kỳ.";
            return RedirectToAction(nameof(Details), new { id = model.SubjectId });
        }

        var dto = new AssignTeacherDto
        {
            SubjectId = model.SubjectId,
            TeacherId = model.TeacherId,
            ClassId = model.ClassId,
            SemesterId = model.SemesterId,
            Status = model.Status
        };

        var result = await _subjectService.AssignTeacherAsync(dto);
        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Details), new { id = model.SubjectId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnassignTeacher(int assignmentId, int subjectId)
    {
        var result = await _subjectService.UnassignTeacherAsync(assignmentId);
        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Details), new { id = subjectId });
    }

    private async Task PopulateAssignmentLookupsAsync(SubjectDetailViewModel viewModel, string subjectName)
    {
        var classesResult = await _subjectService.GetAvailableClassesAsync();
        if (classesResult.Success && classesResult.Data != null)
        {
            viewModel.AvailableClasses = classesResult.Data.Select(c => new ClassSelectItemViewModel
            {
                Id = c.Id,
                ClassName = c.ClassName,
                AcademicYear = c.AcademicYear?.YearCode ?? string.Empty
            }).ToList();
        }

        var semestersResult = await _subjectService.GetAvailableSemestersAsync();
        if (semestersResult.Success && semestersResult.Data != null)
        {
            viewModel.AvailableSemesters = semestersResult.Data.Select(s => new SemesterSelectItemViewModel
            {
                Id = s.Id,
                SemesterName = s.SemesterName,
                AcademicYear = s.AcademicYear?.YearCode ?? string.Empty
            }).ToList();
        }

        var teachersResult = await _teacherService.GetAllTeachersAsync(status: "Active");
        if (teachersResult.Success && teachersResult.Data != null)
        {
            var baseSubject = subjectName.Split(' ')[0].Trim();
            viewModel.AvailableTeachers = teachersResult.Data.Select(t =>
            {
                var isMatch = !string.IsNullOrWhiteSpace(t.Specialization) &&
                              (t.Specialization.Contains(baseSubject, StringComparison.OrdinalIgnoreCase) ||
                               baseSubject.Contains(t.Specialization.Replace("Tổ ", ""), StringComparison.OrdinalIgnoreCase));
                return new TeacherSelectItemViewModel
                {
                    Id = t.Id,
                    TeacherCode = t.TeacherCode,
                    FullName = t.User?.FullName ?? "Chưa có tên",
                    Specialization = t.Specialization,
                    IsMatchingSpecialization = isMatch
                };
            })
            .OrderByDescending(t => t.IsMatchingSpecialization)
            .ThenBy(t => t.FullName)
            .ToList();
        }

        viewModel.AssignForm.SubjectId = viewModel.Id;
    }
}
