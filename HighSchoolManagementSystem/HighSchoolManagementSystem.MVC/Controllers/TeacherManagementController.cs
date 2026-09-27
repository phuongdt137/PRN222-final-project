using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HighSchoolManagementSystem.MVC.Models.ViewModels.Teacher;
using HighSchoolManagementSystem.Services.DTOs;
using HighSchoolManagementSystem.Services.Interfaces;

namespace HighSchoolManagementSystem.MVC.Controllers;

[Authorize(Roles = "Staff,Principal")]
public class TeacherManagementController : Controller
{
    private readonly ITeacherService _teacherService;
    private readonly ISubjectService _subjectService;

    public TeacherManagementController(ITeacherService teacherService, ISubjectService subjectService)
    {
        _teacherService = teacherService;
        _subjectService = subjectService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? specialization, string? status)
    {
        var allResult = await _teacherService.GetAllTeachersAsync();
        var allList = allResult.Data?.ToList() ?? new();

        var specsResult = await _teacherService.GetSpecializationsAsync();
        var specializations = specsResult.Data?.ToList() ?? new();

        int totalCount = allList.Count;
        int activeCount = allList.Count(t => t.Status == "Active");
        int inactiveCount = allList.Count(t => t.Status == "Inactive");

        var filterResult = await _teacherService.GetAllTeachersAsync(search, specialization, status);
        var filteredList = filterResult.Data?.ToList() ?? new();

        var viewModel = new TeacherListViewModel
        {
            SearchTerm = search,
            SpecializationFilter = specialization,
            StatusFilter = status,
            SpecializationList = specializations,
            TotalCount = totalCount,
            ActiveCount = activeCount,
            InactiveCount = inactiveCount,
            Teachers = filteredList.Select(t => new TeacherListItemViewModel
            {
                Id = t.Id,
                UserId = t.UserId,
                TeacherCode = t.TeacherCode,
                FullName = t.User?.FullName ?? "Chưa có tên",
                Email = t.User?.Email ?? string.Empty,
                PhoneNumber = t.User?.PhoneNumber,
                Specialization = t.Specialization,
                Qualification = t.Qualification,
                Status = t.Status,
                MaxPeriodsPerWeek = t.MaxPeriodsPerWeek,
                TotalAssignments = t.TeachingAssignments?.Count ?? 0,
                HomeroomClassName = t.HomeroomClasses?.FirstOrDefault()?.ClassName
            }).ToList()
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var result = await _teacherService.GetTeacherDetailsAsync(id);
        if (!result.Success || result.Data == null)
        {
            TempData["ErrorMessage"] = result.Message ?? "Không tìm thấy thông tin giáo viên.";
            return RedirectToAction(nameof(Index));
        }

        var teacher = result.Data;
        var viewModel = new TeacherDetailViewModel
        {
            Id = teacher.Id,
            UserId = teacher.UserId,
            TeacherCode = teacher.TeacherCode,
            FullName = teacher.User?.FullName ?? string.Empty,
            Username = teacher.User?.Username ?? string.Empty,
            Email = teacher.User?.Email ?? string.Empty,
            PhoneNumber = teacher.User?.PhoneNumber,
            Gender = teacher.User?.Gender,
            DateOfBirth = teacher.User?.DateOfBirth,
            Specialization = teacher.Specialization,
            Qualification = teacher.Qualification,
            HireDate = teacher.HireDate,
            MaxPeriodsPerWeek = teacher.MaxPeriodsPerWeek,
            Status = teacher.Status,
            AvatarUrl = teacher.User?.AvatarUrl,
            HomeroomClasses = teacher.HomeroomClasses?.Select(c => new TeacherHomeroomItemViewModel
            {
                ClassId = c.Id,
                ClassName = c.ClassName,
                AcademicYearName = c.AcademicYear?.YearCode ?? string.Empty
            }).ToList() ?? new(),
            TeachingAssignments = teacher.TeachingAssignments?.Select(ta => new TeacherAssignmentItemViewModel
            {
                AssignmentId = ta.Id,
                SubjectId = ta.Subject?.Id ?? ta.SubjectId,
                ClassName = ta.Class?.ClassName ?? "N/A",
                SubjectCode = ta.Subject?.SubjectCode ?? "N/A",
                SubjectName = ta.Subject?.SubjectName ?? "N/A",
                SemesterName = ta.Semester?.SemesterName ?? "N/A",
                AcademicYearName = ta.Semester?.AcademicYear?.YearCode ?? "N/A",
                Status = ta.Status,
                TimetableSchedule = ta.TimetableEntries != null && ta.TimetableEntries.Any()
                    ? string.Join(", ", ta.TimetableEntries.Select(te => $"Thứ {te.DayOfWeek} (Tiết {te.TimeSlot?.PeriodNo ?? 0} - {te.Room?.RoomName ?? "Phòng học"})"))
                    : null
            }).ToList() ?? new()
        };

        if (!string.IsNullOrWhiteSpace(teacher.Specialization))
        {
            var subjectsResult = await _subjectService.GetAllSubjectsAsync();
            if (subjectsResult.Success && subjectsResult.Data != null)
            {
                var specRoot = teacher.Specialization.Replace("Tổ ", "", StringComparison.OrdinalIgnoreCase).Replace("Học", "", StringComparison.OrdinalIgnoreCase).Trim();
                viewModel.SpecializedSubjects = subjectsResult.Data
                    .Where(s => s.SubjectName.Contains(specRoot, StringComparison.OrdinalIgnoreCase))
                    .Select(s => new TeacherSubjectItemViewModel
                    {
                        SubjectId = s.Id,
                        SubjectCode = s.SubjectCode,
                        SubjectName = s.SubjectName,
                        EvaluationType = s.EvaluationType,
                        IsActive = s.IsActive
                    }).ToList();
            }
        }

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Assignments(int id)
    {
        var result = await _teacherService.GetTeacherDetailsAsync(id);
        if (!result.Success || result.Data == null)
        {
            TempData["ErrorMessage"] = result.Message ?? "Không tìm thấy hồ sơ giáo viên.";
            return RedirectToAction(nameof(Index));
        }

        var teacher = result.Data;
        var viewModel = new TeacherDetailViewModel
        {
            Id = teacher.Id,
            UserId = teacher.UserId,
            TeacherCode = teacher.TeacherCode,
            FullName = teacher.User?.FullName ?? string.Empty,
            Username = teacher.User?.Username ?? string.Empty,
            Email = teacher.User?.Email ?? string.Empty,
            PhoneNumber = teacher.User?.PhoneNumber,
            Gender = teacher.User?.Gender,
            Specialization = teacher.Specialization,
            Qualification = teacher.Qualification,
            MaxPeriodsPerWeek = teacher.MaxPeriodsPerWeek,
            Status = teacher.Status,
            HomeroomClasses = teacher.HomeroomClasses?.Select(c => new TeacherHomeroomItemViewModel
            {
                ClassId = c.Id,
                ClassName = c.ClassName,
                AcademicYearName = c.AcademicYear?.YearCode ?? string.Empty
            }).ToList() ?? new(),
            TeachingAssignments = teacher.TeachingAssignments?.Select(ta => new TeacherAssignmentItemViewModel
            {
                AssignmentId = ta.Id,
                SubjectId = ta.Subject?.Id ?? ta.SubjectId,
                ClassName = ta.Class?.ClassName ?? "N/A",
                SubjectCode = ta.Subject?.SubjectCode ?? "N/A",
                SubjectName = ta.Subject?.SubjectName ?? "N/A",
                SemesterName = ta.Semester?.SemesterName ?? "N/A",
                AcademicYearName = ta.Semester?.AcademicYear?.YearCode ?? "N/A",
                Status = ta.Status,
                TimetableSchedule = ta.TimetableEntries != null && ta.TimetableEntries.Any()
                    ? string.Join("; ", ta.TimetableEntries.Select(te => $"Thứ {te.DayOfWeek}, Tiết {te.TimeSlot?.PeriodNo ?? 0} ({te.Room?.RoomCode ?? "P.Học"})"))
                    : null
            }).ToList() ?? new()
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await PopulateSpecializationsAsync();
        return View(new CreateTeacherViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateTeacherViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateSpecializationsAsync();
            return View(model);
        }

        var dto = new CreateTeacherDto
        {
            TeacherCode = model.TeacherCode,
            FullName = model.FullName,
            Username = model.Username,
            Password = model.Password ?? string.Empty,
            Email = model.Email,
            PhoneNumber = model.PhoneNumber,
            Gender = model.Gender,
            DateOfBirth = model.DateOfBirth,
            Specialization = model.Specialization,
            Qualification = model.Qualification,
            HireDate = model.HireDate,
            MaxPeriodsPerWeek = model.MaxPeriodsPerWeek,
            Status = model.Status
        };

        var result = await _teacherService.CreateTeacherAsync(dto);
        if (!result.Success)
        {
            await PopulateSpecializationsAsync();
            ModelState.AddModelError(string.Empty, result.Message ?? "Đã xảy ra lỗi khi tạo giáo viên.");
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var result = await _teacherService.GetTeacherDetailsAsync(id);
        if (!result.Success || result.Data == null)
        {
            TempData["ErrorMessage"] = result.Message ?? "Không tìm thấy giáo viên cần chỉnh sửa.";
            return RedirectToAction(nameof(Index));
        }

        var teacher = result.Data;
        var model = new EditTeacherViewModel
        {
            Id = teacher.Id,
            UserId = teacher.UserId,
            TeacherCode = teacher.TeacherCode,
            FullName = teacher.User?.FullName ?? string.Empty,
            Email = teacher.User?.Email ?? string.Empty,
            PhoneNumber = teacher.User?.PhoneNumber,
            Gender = teacher.User?.Gender,
            DateOfBirth = teacher.User?.DateOfBirth,
            Specialization = teacher.Specialization,
            Qualification = teacher.Qualification,
            HireDate = teacher.HireDate,
            MaxPeriodsPerWeek = teacher.MaxPeriodsPerWeek,
            Status = teacher.Status
        };

        await PopulateSpecializationsAsync();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EditTeacherViewModel model)
    {
        if (id != model.Id)
        {
            TempData["ErrorMessage"] = "Yêu cầu chỉnh sửa không hợp lệ.";
            return RedirectToAction(nameof(Index));
        }

        var existingResult = await _teacherService.GetTeacherDetailsAsync(id);
        if (!existingResult.Success || existingResult.Data == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy hồ sơ giáo viên.";
            return RedirectToAction(nameof(Index));
        }

        var existingTeacher = existingResult.Data;

        if (!ModelState.IsValid)
        {
            model.TeacherCode = existingTeacher.TeacherCode;
            model.FullName = existingTeacher.User?.FullName ?? string.Empty;
            model.Email = existingTeacher.User?.Email ?? string.Empty;
            model.PhoneNumber = existingTeacher.User?.PhoneNumber;
            model.Gender = existingTeacher.User?.Gender;
            model.DateOfBirth = existingTeacher.User?.DateOfBirth;

            await PopulateSpecializationsAsync();
            return View(model);
        }

        var dto = new UpdateTeacherDto
        {
            Id = model.Id,
            UserId = existingTeacher.UserId,
            TeacherCode = existingTeacher.TeacherCode,
            FullName = existingTeacher.User?.FullName ?? string.Empty,
            Email = existingTeacher.User?.Email ?? string.Empty,
            PhoneNumber = existingTeacher.User?.PhoneNumber,
            Gender = existingTeacher.User?.Gender,
            DateOfBirth = existingTeacher.User?.DateOfBirth,
            Specialization = model.Specialization,
            Qualification = model.Qualification,
            HireDate = model.HireDate,
            MaxPeriodsPerWeek = model.MaxPeriodsPerWeek,
            Status = model.Status
        };

        var result = await _teacherService.UpdateTeacherAsync(dto);
        if (!result.Success)
        {
            model.TeacherCode = existingTeacher.TeacherCode;
            model.FullName = existingTeacher.User?.FullName ?? string.Empty;
            model.Email = existingTeacher.User?.Email ?? string.Empty;
            model.PhoneNumber = existingTeacher.User?.PhoneNumber;
            model.Gender = existingTeacher.User?.Gender;
            model.DateOfBirth = existingTeacher.User?.DateOfBirth;

            await PopulateSpecializationsAsync();
            ModelState.AddModelError(string.Empty, result.Message ?? "Đã xảy ra lỗi khi cập nhật giáo viên.");
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id, string? returnUrl)
    {
        var result = await _teacherService.ToggleTeacherStatusAsync(id);
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

    private async Task PopulateSpecializationsAsync()
    {
        var specsResult = await _teacherService.GetSpecializationsAsync();
        var specList = specsResult.Data?.ToList() ?? new List<string>();

        var subjectsResult = await _subjectService.GetAllSubjectsAsync();
        if (subjectsResult.Success && subjectsResult.Data != null)
        {
            foreach (var sub in subjectsResult.Data)
            {
                var baseName = sub.SubjectName.Split(' ')[0].Trim();
                if (!string.IsNullOrEmpty(baseName))
                {
                    if (!specList.Contains(baseName) && !specList.Contains("Tổ " + baseName))
                    {
                        specList.Add(baseName);
                    }
                }
            }
        }

        ViewBag.Specializations = specList.Distinct().OrderBy(x => x).ToList();
    }
}
