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
    public async Task<IActionResult> Index(string? search, string? specialization, string? status, int page = 1, int pageSize = 10)
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

        int pageNumber = Math.Max(1, page);
        int size = pageSize is < 1 or > 100 ? 10 : pageSize;
        int filteredCount = filteredList.Count;

        var pagedList = filteredList
            .Skip((pageNumber - 1) * size)
            .Take(size)
            .ToList();

        var viewModel = new TeacherListViewModel
        {
            SearchTerm = search,
            SpecializationFilter = specialization,
            StatusFilter = status,
            SpecializationList = specializations,
            TotalCount = totalCount,
            ActiveCount = activeCount,
            InactiveCount = inactiveCount,
            Page = pageNumber,
            PageSize = size,
            FilteredCount = filteredCount,
            Teachers = pagedList.Select(t => new TeacherListItemViewModel
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
                ClassId = ta.ClassId,
                GradeLevelName = ta.Class?.GradeLevel?.LevelName,
                StudentCount = ta.Class?.StudentClasses?.Count(sc => sc.Status == "Enrolled") ?? 0,
                EvaluationType = ta.Subject?.EvaluationType ?? "Score",
                SemesterId = ta.SemesterId,
                IsActiveSemester = ta.Semester?.IsActive ?? false,
                ScheduledPeriodsCount = ta.TimetableEntries?.Count ?? 0,
                HasAssessments = ta.Assessments != null && ta.Assessments.Any(),
                TimetableSchedule = ta.TimetableEntries != null && ta.TimetableEntries.Any()
                    ? string.Join(", ", ta.TimetableEntries.Select(te => $"Thứ {te.DayOfWeek} (Tiết {te.TimeSlot?.PeriodNo ?? 0} - {te.Room?.RoomCode ?? te.Room?.RoomName ?? "Phòng học"})"))
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
    public async Task<IActionResult> Assignments(int id, int? semesterId, int? subjectId, string? status, string? search)
    {
        var result = await _teacherService.GetTeacherDetailsAsync(id);
        if (!result.Success || result.Data == null)
        {
            TempData["ErrorMessage"] = result.Message ?? "Không tìm thấy hồ sơ giáo viên.";
            return RedirectToAction(nameof(Index));
        }

        var teacher = result.Data;

        // Tải danh sách hỗ trợ lọc & gán phân công
        var semestersResult = await _subjectService.GetAvailableSemestersAsync();
        var allSemesters = semestersResult.Data?.ToList() ?? new();

        var subjectsResult = await _subjectService.GetAllSubjectsAsync(isActive: true);
        var allSubjects = subjectsResult.Data?.ToList() ?? new();

        var classesResult = await _subjectService.GetAvailableClassesAsync();
        var allClasses = classesResult.Data?.ToList() ?? new();

        // Lọc danh sách phân công
        var rawAssignments = teacher.TeachingAssignments ?? new List<HighSchoolManagementSystem.Models.Entities.TeachingAssignment>();
        var assignmentsQuery = rawAssignments.AsEnumerable();

        if (semesterId.HasValue && semesterId.Value > 0)
        {
            assignmentsQuery = assignmentsQuery.Where(ta => ta.SemesterId == semesterId.Value);
        }

        if (subjectId.HasValue && subjectId.Value > 0)
        {
            assignmentsQuery = assignmentsQuery.Where(ta => ta.SubjectId == subjectId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status) && status != "All")
        {
            assignmentsQuery = assignmentsQuery.Where(ta => string.Equals(ta.Status, status, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            assignmentsQuery = assignmentsQuery.Where(ta =>
                (ta.Class != null && ta.Class.ClassName.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                (ta.Subject != null && (ta.Subject.SubjectName.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                                       ta.Subject.SubjectCode.Contains(s, StringComparison.OrdinalIgnoreCase))));
        }

        var filteredList = assignmentsQuery
            .OrderByDescending(ta => ta.Semester?.IsActive)
            .ThenBy(ta => ta.Semester?.StartDate)
            .ThenBy(ta => ta.Class?.ClassName)
            .ToList();

        var assignmentItems = filteredList.Select(ta =>
        {
            var scheduleDetails = ta.TimetableEntries != null && ta.TimetableEntries.Any()
                ? ta.TimetableEntries
                    .OrderBy(te => te.DayOfWeek)
                    .ThenBy(te => te.TimeSlot?.PeriodNo)
                    .Select(te => $"Thứ {te.DayOfWeek} (Tiết {te.TimeSlot?.PeriodNo ?? 0} • {te.Room?.RoomCode ?? te.Room?.RoomName ?? "P.Học"})")
                    .ToList()
                : new List<string>();

            return new TeacherAssignmentItemViewModel
            {
                AssignmentId = ta.Id,
                SubjectId = ta.Subject?.Id ?? ta.SubjectId,
                SubjectCode = ta.Subject?.SubjectCode ?? "N/A",
                SubjectName = ta.Subject?.SubjectName ?? "N/A",
                EvaluationType = ta.Subject?.EvaluationType ?? "Score",
                ClassId = ta.ClassId,
                ClassName = ta.Class?.ClassName ?? "N/A",
                GradeLevelName = ta.Class?.GradeLevel?.LevelName,
                StudentCount = ta.Class?.StudentClasses?.Count(sc => sc.Status == "Enrolled") ?? 0,
                SemesterId = ta.SemesterId,
                SemesterName = ta.Semester?.SemesterName ?? "N/A",
                AcademicYearName = ta.Semester?.AcademicYear?.YearCode ?? "N/A",
                IsActiveSemester = ta.Semester?.IsActive ?? false,
                Status = ta.Status,
                ScheduledPeriodsCount = ta.TimetableEntries?.Count ?? 0,
                ScheduleDetails = scheduleDetails,
                TimetableSchedule = scheduleDetails.Any() ? string.Join("; ", scheduleDetails) : null,
                HasAssessments = ta.Assessments != null && ta.Assessments.Any()
            };
        }).ToList();

        var specRoot = !string.IsNullOrWhiteSpace(teacher.Specialization)
            ? teacher.Specialization.Replace("Tổ ", "", StringComparison.OrdinalIgnoreCase).Replace("Học", "", StringComparison.OrdinalIgnoreCase).Trim()
            : string.Empty;

        var viewModel = new TeacherAssignmentsViewModel
        {
            TeacherId = teacher.Id,
            UserId = teacher.UserId,
            TeacherCode = teacher.TeacherCode,
            FullName = teacher.User?.FullName ?? string.Empty,
            Email = teacher.User?.Email ?? string.Empty,
            PhoneNumber = teacher.User?.PhoneNumber,
            Specialization = teacher.Specialization,
            Qualification = teacher.Qualification,
            MaxPeriodsPerWeek = teacher.MaxPeriodsPerWeek,
            Status = teacher.Status,
            AvatarUrl = teacher.User?.AvatarUrl,
            SemesterFilter = semesterId,
            SubjectFilter = subjectId,
            StatusFilter = status,
            SearchTerm = search,
            HomeroomClasses = teacher.HomeroomClasses?.Select(c => new TeacherHomeroomItemViewModel
            {
                ClassId = c.Id,
                ClassName = c.ClassName,
                AcademicYearName = c.AcademicYear?.YearCode ?? string.Empty
            }).ToList() ?? new(),
            Assignments = assignmentItems,
            TotalAssignmentsCount = rawAssignments.Count,
            TotalClassesCount = rawAssignments.Select(ta => ta.ClassId).Distinct().Count(),
            TotalSubjectsCount = rawAssignments.Select(ta => ta.SubjectId).Distinct().Count(),
            TotalScheduledPeriods = rawAssignments
                .Where(ta => ta.Semester?.IsActive == true || (!rawAssignments.Any(x => x.Semester?.IsActive == true)))
                .Sum(ta => ta.TimetableEntries?.Count ?? 0),
            AvailableSemesters = allSemesters.Select(s => new SemesterFilterOption
            {
                Id = s.Id,
                SemesterName = s.SemesterName,
                AcademicYear = s.AcademicYear?.YearCode ?? string.Empty,
                IsActive = s.IsActive
            }).ToList(),
            AvailableSubjects = allSubjects.Select(s => new SubjectFilterOption
            {
                Id = s.Id,
                SubjectCode = s.SubjectCode,
                SubjectName = s.SubjectName,
                EvaluationType = s.EvaluationType,
                IsSpecialized = !string.IsNullOrWhiteSpace(specRoot) && s.SubjectName.Contains(specRoot, StringComparison.OrdinalIgnoreCase)
            }).ToList(),
            AvailableClasses = allClasses.Select(c => new ClassFilterOption
            {
                Id = c.Id,
                ClassName = c.ClassName,
                GradeLevelName = c.GradeLevel?.LevelName ?? string.Empty,
                AcademicYear = c.AcademicYear?.YearCode ?? string.Empty
            }).ToList()
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> TeachingAssignments(int? semesterId, int? teacherId, int? classId, int? subjectId, string? search, int page = 1, int pageSize = 10)
    {
        var rawResult = await _teacherService.GetTeachingAssignmentsAsync(semesterId, teacherId, classId, subjectId);
        var rawList = rawResult.Data?.ToList() ?? new List<HighSchoolManagementSystem.Models.Entities.TeachingAssignment>();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            rawList = rawList.Where(ta =>
                (ta.Teacher?.User?.FullName != null && ta.Teacher.User.FullName.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                (ta.Teacher?.TeacherCode != null && ta.Teacher.TeacherCode.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                (ta.Class?.ClassName != null && ta.Class.ClassName.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                (ta.Subject?.SubjectName != null && ta.Subject.SubjectName.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                (ta.Subject?.SubjectCode != null && ta.Subject.SubjectCode.Contains(s, StringComparison.OrdinalIgnoreCase))
            ).ToList();
        }

        var semestersResult = await _subjectService.GetAvailableSemestersAsync();
        var allSemesters = semestersResult.Data?.ToList() ?? new();

        var teachersResult = await _teacherService.GetAllTeachersAsync();
        var allTeachers = teachersResult.Data?.ToList() ?? new();

        var classesResult = await _subjectService.GetAvailableClassesAsync();
        var allClasses = classesResult.Data?.ToList() ?? new();

        var subjectsResult = await _subjectService.GetAllSubjectsAsync(isActive: true);
        var allSubjects = subjectsResult.Data?.ToList() ?? new();

        int totalAssignments = rawList.Count;
        int totalTeachers = rawList.Select(ta => ta.TeacherId).Distinct().Count();
        int totalClasses = rawList.Select(ta => ta.ClassId).Distinct().Count();
        int totalSubjects = rawList.Select(ta => ta.SubjectId).Distinct().Count();

        int pageNumber = Math.Max(1, page);
        int size = pageSize is < 1 or > 100 ? 10 : pageSize;
        int filteredCount = rawList.Count;

        var pagedRawList = rawList
            .Skip((pageNumber - 1) * size)
            .Take(size)
            .ToList();

        var assignmentItems = pagedRawList.Select(ta =>
        {
            var scheduleDetails = ta.TimetableEntries != null && ta.TimetableEntries.Any()
                ? ta.TimetableEntries
                    .OrderBy(te => te.DayOfWeek)
                    .ThenBy(te => te.TimeSlot?.PeriodNo)
                    .Select(te => $"Thứ {te.DayOfWeek} (Tiết {te.TimeSlot?.PeriodNo ?? 0} • {te.Room?.RoomCode ?? te.Room?.RoomName ?? "P.Học"})")
                    .ToList()
                : new List<string>();

            return new TeacherAssignmentItemViewModel
            {
                AssignmentId = ta.Id,
                TeacherId = ta.TeacherId,
                TeacherCode = ta.Teacher?.TeacherCode ?? "N/A",
                TeacherName = ta.Teacher?.User?.FullName ?? "N/A",
                Specialization = ta.Teacher?.Specialization,
                SubjectId = ta.Subject?.Id ?? ta.SubjectId,
                SubjectCode = ta.Subject?.SubjectCode ?? "N/A",
                SubjectName = ta.Subject?.SubjectName ?? "N/A",
                EvaluationType = ta.Subject?.EvaluationType ?? "Score",
                ClassId = ta.ClassId,
                ClassName = ta.Class?.ClassName ?? "N/A",
                GradeLevelName = ta.Class?.GradeLevel?.LevelName,
                StudentCount = ta.Class?.StudentClasses?.Count(sc => sc.Status == "Enrolled") ?? 0,
                SemesterId = ta.SemesterId,
                SemesterName = ta.Semester?.SemesterName ?? "N/A",
                AcademicYearName = ta.Semester?.AcademicYear?.YearCode ?? "N/A",
                IsActiveSemester = ta.Semester?.IsActive ?? false,
                Status = ta.Status,
                ScheduledPeriodsCount = ta.TimetableEntries?.Count ?? 0,
                ScheduleDetails = scheduleDetails,
                TimetableSchedule = scheduleDetails.Any() ? string.Join("; ", scheduleDetails) : null,
                HasAssessments = ta.Assessments != null && ta.Assessments.Any()
            };
        }).ToList();

        var viewModel = new AllTeachingAssignmentsViewModel
        {
            SemesterFilter = semesterId,
            TeacherFilter = teacherId,
            ClassFilter = classId,
            SubjectFilter = subjectId,
            SearchTerm = search,
            Page = pageNumber,
            PageSize = size,
            FilteredCount = filteredCount,
            Assignments = assignmentItems,
            TotalAssignmentsCount = totalAssignments,
            TotalTeachersCount = totalTeachers,
            TotalClassesCount = totalClasses,
            TotalSubjectsCount = totalSubjects,
            AvailableSemesters = allSemesters.Select(s => new SemesterFilterOption
            {
                Id = s.Id,
                SemesterName = s.SemesterName,
                AcademicYear = s.AcademicYear?.YearCode ?? string.Empty,
                IsActive = s.IsActive
            }).ToList(),
            AvailableTeachers = allTeachers.Select(t => new TeacherFilterOption
            {
                Id = t.Id,
                TeacherCode = t.TeacherCode,
                FullName = t.User?.FullName ?? string.Empty,
                Specialization = t.Specialization
            }).ToList(),
            AvailableClasses = allClasses.Select(c => new ClassFilterOption
            {
                Id = c.Id,
                ClassName = c.ClassName,
                GradeLevelName = c.GradeLevel?.LevelName ?? string.Empty,
                AcademicYear = c.AcademicYear?.YearCode ?? string.Empty
            }).ToList(),
            AvailableSubjects = allSubjects.Select(s => new SubjectFilterOption
            {
                Id = s.Id,
                SubjectCode = s.SubjectCode,
                SubjectName = s.SubjectName,
                EvaluationType = s.EvaluationType
            }).ToList()
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignSubjectClass(AssignTeacherDto dto, string? returnUrl = null)
    {
        if (dto.TeacherId <= 0 || dto.SubjectId <= 0 || dto.ClassId <= 0 || dto.SemesterId <= 0)
        {
            TempData["ErrorMessage"] = "Vui lòng chọn đầy đủ thông tin Giáo viên, Học kỳ, Môn học và Lớp học.";
            if (returnUrl == "All") return RedirectToAction(nameof(TeachingAssignments));
            return RedirectToAction(nameof(Assignments), new { id = dto.TeacherId });
        }

        var teacherResult = await _teacherService.GetTeacherByIdAsync(dto.TeacherId);
        if (!teacherResult.Success || teacherResult.Data == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy thông tin giáo viên.";
            if (returnUrl == "All") return RedirectToAction(nameof(TeachingAssignments));
            return RedirectToAction(nameof(Assignments), new { id = dto.TeacherId });
        }

        if (teacherResult.Data.Status == "Inactive")
        {
            TempData["ErrorMessage"] = $"Giáo viên '{teacherResult.Data.User?.FullName ?? teacherResult.Data.TeacherCode}' đang tạm dừng công tác, không thể phân công giảng dạy.";
            if (returnUrl == "All") return RedirectToAction(nameof(TeachingAssignments));
            return RedirectToAction(nameof(Assignments), new { id = dto.TeacherId });
        }

        var result = await _subjectService.AssignTeacherAsync(dto);
        if (result.Success)
        {
            TempData["SuccessMessage"] = "Phân công lớp và môn học thành công!";
        }
        else
        {
            TempData["ErrorMessage"] = result.Message ?? "Không thể phân công giảng dạy.";
        }

        if (returnUrl == "All") return RedirectToAction(nameof(TeachingAssignments), new { semesterId = dto.SemesterId });
        return RedirectToAction(nameof(Assignments), new { id = dto.TeacherId, semesterId = dto.SemesterId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnassignSubjectClass(int assignmentId, int teacherId, int? semesterId = null, string? returnUrl = null)
    {
        var result = await _subjectService.UnassignTeacherAsync(assignmentId);
        if (result.Success)
        {
            TempData["SuccessMessage"] = "Đã hủy phân công giảng dạy thành công.";
        }
        else
        {
            TempData["ErrorMessage"] = result.Message ?? "Không thể hủy phân công này (có thể đã phát sinh dữ liệu bảng điểm hoặc bài kiểm tra).";
        }

        if (returnUrl == "All") return RedirectToAction(nameof(TeachingAssignments), new { semesterId });
        return RedirectToAction(nameof(Assignments), new { id = teacherId, semesterId });
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
