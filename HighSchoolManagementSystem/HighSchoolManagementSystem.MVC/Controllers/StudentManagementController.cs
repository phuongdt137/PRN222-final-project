using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.MVC.Models.ViewModels.Student;
using HighSchoolManagementSystem.Services.DTOs;
using HighSchoolManagementSystem.Services.Common;
using HighSchoolManagementSystem.Services.Implementations;
using HighSchoolManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HighSchoolManagementSystem.MVC.Controllers;

[Authorize(Roles = "Staff")]
public class StudentManagementController(IStudentService students) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? status, int? classId, bool unassignedOnly = false, int page = 1)
    {
        var result = ModelState.IsValid
            ? await students.GetStudentsAsync(search, status, classId, unassignedOnly, page)
            : ServiceResult<HighSchoolManagementSystem.Models.PagedResult<Student>>.Fail(ErrorCodes.ValidationFailed, "Bộ lọc hoặc số trang không hợp lệ.");
        var classes = await students.GetCurrentClassesAsync();
        var year = await students.GetCurrentYearAsync();
        if (!result.Success || !classes.Success || !year.Success)
            ViewData["ErrorMessage"] = result.Message ?? classes.Message ?? year.Message;
        return View(new StudentListViewModel
        {
            Search = search, Status = status, ClassId = classId, UnassignedOnly = unassignedOnly,
            CurrentYear = year.Data?.YearCode, Students = result.Data?.Items.Select(Map).ToList() ?? new(),
            Page = result.Data?.Page ?? 1, TotalPages = result.Data?.TotalPages ?? 1, TotalCount = result.Data?.TotalCount ?? 0,
            Classes = ClassOptions(classes.Data)
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var result = await students.GetStudentAsync(id);
        if (!result.Success || result.Data == null) return ReadFailure(result.ErrorCode, result.Message);
        return View(Map(result.Data));
    }

    [HttpGet]
    public IActionResult Create() => View(new CreateStudentViewModel { AdmissionDate = SchoolDate.Today });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateStudentViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await students.CreateStudentAsync(model);
        if (!result.Success || result.Data == null)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Không thể tạo học sinh.");
            return View(model);
        }
        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = result.Data.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var result = await students.GetStudentAsync(id);
        if (!result.Success || result.Data == null) return ReadFailure(result.ErrorCode, result.Message);
        ViewData["StudentId"] = id;
        return View((UpdateStudentDto)Map(result.Data));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UpdateStudentDto model)
    {
        ViewData["StudentId"] = id;
        if (!ModelState.IsValid) return View(model);
        var result = await students.UpdateStudentAsync(id, model);
        if (result.ErrorCode == StudentService.NotFound) return NotFound();
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Không thể cập nhật học sinh.");
            return View(model);
        }
        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public Task<IActionResult> AssignClass(int id) => AssignmentPage(id, 0);

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignClass(int id, AssignStudentClassDto model)
    {
        if (ModelState.IsValid)
        {
            var result = await students.AssignClassAsync(id, model.ClassId);
            if (result.ErrorCode == StudentService.NotFound) return NotFound();
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction(nameof(Details), new { id });
            }
            ModelState.AddModelError(string.Empty, result.Message ?? "Không thể phân lớp.");
        }
        return await AssignmentPage(id, model.ClassId);
    }

    private async Task<IActionResult> AssignmentPage(int id, int selectedClass)
    {
        var result = await students.GetStudentAsync(id);
        if (!result.Success || result.Data == null) return ReadFailure(result.ErrorCode, result.Message);
        var classes = await students.GetCurrentClassesAsync();
        var year = await students.GetCurrentYearAsync();
        if (!classes.Success || !year.Success) ModelState.AddModelError(string.Empty, classes.Message ?? year.Message ?? "Không thể tải lớp học.");
        return View("AssignClass", new AssignStudentClassViewModel
        {
            Student = Map(result.Data), ClassId = selectedClass, CurrentYear = year.Data?.YearCode,
            Classes = ClassOptions(classes.Data)
        });
    }

    [HttpGet]
    public async Task<IActionResult> AcademicHistory(int id)
    {
        var result = await students.GetAcademicHistoryAsync(id);
        if (!result.Success || result.Data == null) return ReadFailure(result.ErrorCode, result.Message);
        var student = result.Data;
        var semesters = student.SemesterReports.Select(r => r.Semester)
            .Concat(student.SemesterSubjectResults.Select(r => r.Semester))
            .Concat(student.ConductEvaluations.Select(r => r.Semester))
            .Concat(student.StudentScores.Select(r => r.Assessment.TeachingAssignment.Semester))
            .DistinctBy(s => s.Id).OrderByDescending(s => s.AcademicYear.StartDate).ThenByDescending(s => s.StartDate);
        return View(new StudentAcademicHistoryViewModel
        {
            Student = Map(student),
            Memberships = student.StudentClasses.OrderByDescending(sc => sc.Class.AcademicYear.StartDate)
                .ThenByDescending(sc => sc.JoinedAt).ThenByDescending(sc => sc.Id).ToList(),
            Semesters = semesters.Select(s => new StudentSemesterHistoryViewModel
            {
                Semester = s, Report = student.SemesterReports.FirstOrDefault(r => r.SemesterId == s.Id),
                Conduct = student.ConductEvaluations.Where(r => r.SemesterId == s.Id).OrderByDescending(r => r.EvaluatedAt).FirstOrDefault(),
                Subjects = student.SemesterSubjectResults.Where(r => r.SemesterId == s.Id).OrderBy(r => r.Subject.SubjectName).ToList(),
                Scores = student.StudentScores.Where(r => r.Assessment.TeachingAssignment.SemesterId == s.Id)
                    .OrderBy(r => r.Assessment.TeachingAssignment.Subject.SubjectName).ThenBy(r => r.Assessment.ConductedAt).ThenBy(r => r.Id).ToList()
            }).ToList()
        });
    }

    [HttpGet]
    public Task<IActionResult> TransferClass(int id) => TransferPage(id);

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TransferClass(int id, TransferStudentClassDto model)
    {
        if (ModelState.IsValid)
        {
            var result = await students.TransferClassAsync(id, model);
            if (result.ErrorCode == StudentService.NotFound) return NotFound();
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction(nameof(Details), new { id });
            }
            ModelState.AddModelError(string.Empty, result.Message ?? "Không thể chuyển lớp.");
        }
        return await TransferPage(id, model);
    }

    private async Task<IActionResult> TransferPage(int id, TransferStudentClassDto? submitted = null)
    {
        var result = await students.GetStudentAsync(id);
        if (!result.Success || result.Data == null) return ReadFailure(result.ErrorCode, result.Message);
        var student = Map(result.Data);
        var classes = await students.GetCurrentClassesAsync();
        if (!classes.Success) ModelState.AddModelError(string.Empty, classes.Message ?? "Không thể tải lớp học.");
        return View("TransferClass", new TransferStudentClassViewModel
        {
            Student = student, ClassId = submitted?.ClassId ?? 0,
            // Retain the original token on validation failure; a stale form requires an explicit reload.
            MembershipId = submitted?.MembershipId ?? student.CurrentMembershipId ?? 0,
            Classes = ClassOptions(classes.Data?.Where(c => c.Id != student.CurrentClassId && c.GradeLevelId == student.CurrentGradeId).ToList())
        });
    }

    private IActionResult ReadFailure(string? code, string? message)
    {
        if (code == StudentService.NotFound) return NotFound();
        TempData["ErrorMessage"] = message;
        return RedirectToAction(nameof(Index));
    }

    private static List<SelectListItem> ClassOptions(List<Class>? classes) => classes?.Select(c =>
        new SelectListItem($"{c.ClassName} — {c.GradeLevel.LevelName} — {c.AcademicYear.YearCode}", c.Id.ToString())).ToList() ?? new();

    private static StudentDetailsViewModel Map(Student student)
    {
        var memberships = student.StudentClasses.Where(sc => sc.Class.AcademicYear.IsCurrent).ToList();
        var enrolled = memberships.Where(sc => sc.Status == "Enrolled" && sc.LeftAt == null).ToList();
        var current = enrolled.Count == 1 ? enrolled[0] : null;
        return new StudentDetailsViewModel
        {
            Id = student.Id, StudentCode = student.StudentCode, FullName = student.User.FullName,
            Email = student.User.Email, PhoneNumber = student.User.PhoneNumber, Gender = student.User.Gender,
            DateOfBirth = student.User.DateOfBirth, AdmissionDate = student.AdmissionDate, Address = student.Address,
            Status = student.Status, Username = student.User.Username, IsAccountActive = student.User.IsActive,
            ParentId = student.ParentId, ParentName = student.Parent?.User.FullName, ParentPhone = student.Parent?.User.PhoneNumber,
            ParentEmail = student.Parent?.User.Email, HasAssignment = memberships.Count > 0,
            CurrentClassId = current?.ClassId, CurrentClassName = current?.Class.ClassName,
            CurrentMembershipId = current?.Id, CurrentGradeId = current?.Class.GradeLevelId,
            CurrentGrade = current?.Class.GradeLevel.LevelName, CurrentYear = current?.Class.AcademicYear.YearCode
        };
    }
}
