using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.MVC.Models.ViewModels;
using HighSchoolManagementSystem.Repositories.Interfaces;
using HighSchoolManagementSystem.Services.Common;
using HighSchoolManagementSystem.Services.Interfaces;

namespace HighSchoolManagementSystem.MVC.Controllers;

[Authorize(Roles = "Teacher")]
public class TeacherController : Controller
{
    private readonly IClassService _classService;
    private readonly ITeacherRepository _teacherRepository;

    public TeacherController(
        IClassService classService,
        ITeacherRepository teacherRepository)
    {
        _classService = classService;
        _teacherRepository = teacherRepository;
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
    public async Task<IActionResult> HomeroomClassDetails(int id)
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
                .ToList()
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
