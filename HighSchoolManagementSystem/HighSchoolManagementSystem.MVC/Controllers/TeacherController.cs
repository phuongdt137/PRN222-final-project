using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.MVC.Models.ViewModels;
using HighSchoolManagementSystem.MVC.Models.ViewModels.Teacher;
using HighSchoolManagementSystem.Repositories.Interfaces;
using HighSchoolManagementSystem.Services.Common;
using HighSchoolManagementSystem.Services.Interfaces;

namespace HighSchoolManagementSystem.MVC.Controllers;

[Authorize(Roles = "Teacher")]
public class TeacherController : Controller
{
    private readonly IClassService _classService;
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITimetableService _timetableService;
    private readonly ITeacherService _teacherService;
    private readonly ISubjectService _subjectService;

    public TeacherController(
        IClassService classService,
        ITeacherRepository teacherRepository,
        ITimetableService timetableService,
        ITeacherService teacherService,
        ISubjectService subjectService)
    {
        _classService = classService;
        _teacherRepository = teacherRepository;
        _timetableService = timetableService;
        _teacherService = teacherService;
        _subjectService = subjectService;
    }

    public IActionResult Dashboard()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> HomeroomClasses()
    {
        var teacher = await ResolveCurrentTeacherAsync();
        if (teacher == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy hồ sơ giáo viên gắn với tài khoản này.";
            return View(Array.Empty<TeacherAccessibleClassItemViewModel>());
        }

        var result = await _classService.GetHomeroomClassesForTeacherAsync(teacher.Id);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
            return View(Array.Empty<TeacherAccessibleClassItemViewModel>());
        }

        var items = result.Data!
            .Select(c => new TeacherAccessibleClassItemViewModel
            {
                Id = c.Id,
                ClassName = c.ClassName,
                AcademicYearCode = c.AcademicYear.YearCode,
                GradeLevelName = c.GradeLevel.LevelName,
                StudentCount = c.StudentClasses.Count(sc => sc.Status == "Enrolled" && sc.LeftAt == null),
                IsHomeroom = true,
                IsTeaching = c.TeachingAssignments.Any(ta => ta.TeacherId == teacher.Id),
                AccessRoleLabel = "GVCN"
            })
            .ToList();

        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> HomeroomClassDetails(int id, DateTime? weekStart)
    {
        var teacher = await ResolveCurrentTeacherAsync();
        if (teacher == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy hồ sơ giáo viên gắn với tài khoản này.";
            return RedirectToAction(nameof(HomeroomClasses));
        }

        var result = await _classService.GetHomeroomClassForTeacherAsync(id, teacher.Id);
        if (!result.Success || result.Data == null)
        {
            if (result.ErrorCode == ErrorCodes.AuthForbidden)
            {
                return Forbid();
            }

            TempData["ErrorMessage"] = result.Message ?? "Không tìm thấy lớp chủ nhiệm.";
            return RedirectToAction(nameof(HomeroomClasses));
        }

        var c = result.Data;
        var semesterId = await _timetableService.GetActiveSemesterIdAsync();
        var timetable = await _timetableService.GetForClassAsync(c.Id, semesterId);
        var slots = await _timetableService.GetTimeSlotsAsync();
        var enrolledStudents = c.StudentClasses
            .Where(sc => sc.Status == "Enrolled" && sc.LeftAt == null)
            .OrderBy(sc => sc.Student.StudentCode)
            .ToList();

        var teachers = c.TeachingAssignments
            .OrderBy(ta => ta.Subject.SubjectName)
            .ThenBy(ta => ta.Semester.StartDate)
            .Select(ta => new ClassTeacherItemViewModel
            {
                TeacherCode = ta.Teacher.TeacherCode,
                TeacherName = ta.Teacher.User.FullName,
                SubjectCode = ta.Subject.SubjectCode,
                SubjectName = ta.Subject.SubjectName,
                SemesterName = ta.Semester.SemesterName,
                Status = ta.Status
            })
            .ToList();

        var model = new ClassDetailsViewModel
        {
            Id = c.Id,
            ClassName = c.ClassName,
            AcademicYearId = c.AcademicYearId,
            AcademicYearCode = c.AcademicYear.YearCode,
            GradeLevelName = c.GradeLevel.LevelName,
            GradeLevelCode = c.GradeLevel.LevelCode,
            HomeroomTeacherName = c.HomeroomTeacher?.User?.FullName,
            HomeroomTeacherCode = c.HomeroomTeacher?.TeacherCode,
            StudentCount = enrolledStudents.Count,
            TeacherAssignmentCount = teachers.Count,
            Teachers = teachers,
            Students = enrolledStudents
                .Select(sc => new ClassStudentItemViewModel
                {
                    StudentId = sc.StudentId,
                    StudentCode = sc.Student.StudentCode,
                    StudentName = sc.Student.User.FullName,
                    Gender = sc.Student.User.Gender,
                    PhoneNumber = sc.Student.User.PhoneNumber,
                    JoinedAt = sc.JoinedAt,
                    EnrollmentStatus = sc.Status
                })
                .ToList(),
            Timetable = TimetableController.MapList(timetable.Data, weekStart, slots.Data)
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> MyClasses()
    {
        var teacher = await ResolveCurrentTeacherAsync();
        if (teacher == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy hồ sơ giáo viên gắn với tài khoản này.";
            return View(Array.Empty<TeacherAccessibleClassItemViewModel>());
        }

        var result = await _classService.GetAccessibleClassesForTeacherAsync(teacher.Id);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
            return View(Array.Empty<TeacherAccessibleClassItemViewModel>());
        }

        var items = result.Data!
            .Select(c =>
            {
                var isHomeroom = c.HomeroomTeacherId == teacher.Id;
                var isTeaching = c.TeachingAssignments.Any(ta => ta.TeacherId == teacher.Id);
                var roleLabel = isHomeroom && isTeaching
                    ? "GVCN + Bộ môn"
                    : isHomeroom
                        ? "GVCN"
                        : "Bộ môn";

                return new TeacherAccessibleClassItemViewModel
                {
                    Id = c.Id,
                    ClassName = c.ClassName,
                    AcademicYearCode = c.AcademicYear.YearCode,
                    GradeLevelName = c.GradeLevel.LevelName,
                    StudentCount = c.StudentClasses.Count(sc => sc.Status == "Enrolled" && sc.LeftAt == null),
                    IsHomeroom = isHomeroom,
                    IsTeaching = isTeaching,
                    AccessRoleLabel = roleLabel
                };
            })
            .ToList();

        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> ClassStudents(int id)
    {
        var teacher = await ResolveCurrentTeacherAsync();
        if (teacher == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy hồ sơ giáo viên gắn với tài khoản này.";
            return RedirectToAction(nameof(MyClasses));
        }

        var result = await _classService.GetClassStudentsForViewerAsync(id, teacher.Id);
        if (!result.Success || result.Data == null)
        {
            if (result.ErrorCode == ErrorCodes.AuthForbidden)
            {
                return Forbid();
            }

            TempData["ErrorMessage"] = result.Message ?? "Không thể tải danh sách học sinh.";
            return RedirectToAction(nameof(MyClasses));
        }

        var model = MapClassStudentsPage(result.Data, accessContext: "Teacher");
        return View("~/Views/Shared/ClassStudents.cshtml", model);
    }

    [HttpGet]
    public async Task<IActionResult> AssignedClasses(int? semesterId, string? search)
    {
        var teacher = await ResolveCurrentTeacherAsync();
        if (teacher == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy hồ sơ giáo viên gắn với tài khoản này.";
            return View(new TeacherAssignedClassesViewModel());
        }

        var semestersResult = await _subjectService.GetAvailableSemestersAsync();
        var allSemesters = semestersResult.Data?.ToList() ?? new();

        int? activeSemesterId = await _timetableService.GetActiveSemesterIdAsync();
        int? effectiveSemesterId = semesterId ?? activeSemesterId;

        var assignmentsResult = await _teacherService.GetTeachingAssignmentsAsync(
            semesterId: effectiveSemesterId,
            teacherId: teacher.Id);

        var rawList = assignmentsResult.Data?.ToList() ?? new List<TeachingAssignment>();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            rawList = rawList.Where(ta =>
                (ta.Class?.ClassName != null && ta.Class.ClassName.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                (ta.Subject?.SubjectName != null && ta.Subject.SubjectName.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                (ta.Subject?.SubjectCode != null && ta.Subject.SubjectCode.Contains(s, StringComparison.OrdinalIgnoreCase))
            ).ToList();
        }

        var assignmentItems = rawList.Select(ta =>
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
                TeacherCode = teacher.TeacherCode,
                TeacherName = teacher.User?.FullName ?? "N/A",
                Specialization = teacher.Specialization,
                SubjectId = ta.Subject?.Id ?? ta.SubjectId,
                SubjectCode = ta.Subject?.SubjectCode ?? "N/A",
                SubjectName = ta.Subject?.SubjectName ?? "N/A",
                EvaluationType = ta.Subject?.EvaluationType ?? "Score",
                ClassId = ta.ClassId,
                ClassName = ta.Class?.ClassName ?? "N/A",
                GradeLevelName = ta.Class?.GradeLevel?.LevelName,
                StudentCount = ta.Class?.StudentClasses?.Count(sc => sc.Status == "Enrolled" && sc.LeftAt == null) ?? 0,
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

        var viewModel = new TeacherAssignedClassesViewModel
        {
            TeacherId = teacher.Id,
            TeacherCode = teacher.TeacherCode,
            TeacherName = teacher.User?.FullName ?? "N/A",
            Specialization = teacher.Specialization,
            SemesterFilter = effectiveSemesterId,
            SearchTerm = search,
            TotalClassesCount = rawList.Select(ta => ta.ClassId).Distinct().Count(),
            TotalSubjectsCount = rawList.Select(ta => ta.SubjectId).Distinct().Count(),
            TotalPeriodsPerWeek = rawList.Sum(ta => ta.TimetableEntries?.Count ?? 0),
            TotalStudentsCount = rawList.Sum(ta => ta.Class?.StudentClasses?.Count(sc => sc.Status == "Enrolled" && sc.LeftAt == null) ?? 0),
            AvailableSemesters = allSemesters.Select(s => new SemesterFilterOption
            {
                Id = s.Id,
                SemesterName = s.SemesterName,
                AcademicYear = s.AcademicYear?.YearCode ?? string.Empty,
                IsActive = s.IsActive
            }).ToList(),
            Assignments = assignmentItems
        };

        return View(viewModel);
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

    private static ClassStudentsPageViewModel MapClassStudentsPage(Class c, string accessContext)
    {
        var students = c.StudentClasses
            .Where(sc => sc.Status == "Enrolled" && sc.LeftAt == null)
            .OrderBy(sc => sc.Student.StudentCode)
            .Select(sc => new ClassStudentItemViewModel
            {
                StudentId = sc.StudentId,
                StudentCode = sc.Student.StudentCode,
                StudentName = sc.Student.User.FullName,
                Gender = sc.Student.User.Gender,
                PhoneNumber = sc.Student.User.PhoneNumber,
                JoinedAt = sc.JoinedAt,
                EnrollmentStatus = sc.Status
            })
            .ToList();

        return new ClassStudentsPageViewModel
        {
            ClassId = c.Id,
            ClassName = c.ClassName,
            AcademicYearId = c.AcademicYearId,
            AcademicYearCode = c.AcademicYear.YearCode,
            GradeLevelName = c.GradeLevel.LevelName,
            HomeroomTeacherName = c.HomeroomTeacher?.User?.FullName,
            AccessContext = accessContext,
            Students = students
        };
    }
}
