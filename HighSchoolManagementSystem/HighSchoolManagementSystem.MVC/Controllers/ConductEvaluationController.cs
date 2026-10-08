using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HighSchoolManagementSystem.MVC.Models.ViewModels.Conduct;
using HighSchoolManagementSystem.MVC.Models.ViewModels.Score;
using HighSchoolManagementSystem.Repositories.Interfaces;
using HighSchoolManagementSystem.Services.DTOs;
using HighSchoolManagementSystem.Services.Interfaces;

namespace HighSchoolManagementSystem.MVC.Controllers;

[Authorize]
public class ConductEvaluationController : Controller
{
    private readonly IConductEvaluationService _conductService;
    private readonly IClassRepository _classRepository;
    private readonly IAcademicYearRepository _academicYearRepository;
    private readonly ISemesterRepository _semesterRepository;
    private readonly ITeacherRepository _teacherRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IParentRepository _parentRepository;
    private readonly ILogger<ConductEvaluationController> _logger;

    public ConductEvaluationController(
        IConductEvaluationService conductService,
        IClassRepository classRepository,
        IAcademicYearRepository academicYearRepository,
        ISemesterRepository semesterRepository,
        ITeacherRepository teacherRepository,
        IStudentRepository studentRepository,
        IParentRepository parentRepository,
        ILogger<ConductEvaluationController> logger)
    {
        _conductService = conductService;
        _classRepository = classRepository;
        _academicYearRepository = academicYearRepository;
        _semesterRepository = semesterRepository;
        _teacherRepository = teacherRepository;
        _studentRepository = studentRepository;
        _parentRepository = parentRepository;
        _logger = logger;
    }

    // =========================================================================
    // 1. DANH SÁCH ĐÁNH GIÁ HẠNH KIỂM (GVCN, HIỆU TRƯỞNG, GIÁO VỤ)
    // =========================================================================
    [HttpGet]
    [Authorize(Roles = "Teacher,Principal,Staff")]
    public async Task<IActionResult> Index(
        int? classId, int? semesterId, string? status, string? grade, string? search)
    {
        int currentUserId = ResolveCurrentUserId();
        string userRole = User.FindFirstValue(ClaimTypes.Role) ?? "Teacher";

        var (availableClasses, isHomeroom) = await ResolveAvailableClassesAsync(currentUserId, userRole);
        var semestersList = await ResolveSemestersListAsync();

        var model = new ConductEvaluationListViewModel
        {
            AvailableClasses = availableClasses,
            AvailableSemesters = semestersList,
            StatusFilter = status,
            GradeFilter = grade,
            SearchTerm = search,
            IsHomeroomTeacher = isHomeroom,
            CanEdit = userRole == "Teacher" || userRole == "Staff"
        };

        if (!availableClasses.Any())
        {
            if (userRole == "Teacher")
            {
                ViewBag.Notice = "Bạn hiện không được phân công làm Giáo viên chủ nhiệm của lớp học nào.";
            }
            return View(model);
        }

        int targetClassId = classId ?? availableClasses.First().Id;
        if (!availableClasses.Any(c => c.Id == targetClassId))
        {
            targetClassId = availableClasses.First().Id;
        }

        int targetSemesterId = semesterId ?? (semestersList.FirstOrDefault(s => s.IsActive)?.Id ?? (semestersList.FirstOrDefault()?.Id ?? 0));

        model.SelectedClassId = targetClassId;
        model.SelectedSemesterId = targetSemesterId;

        if (targetClassId > 0 && targetSemesterId > 0)
        {
            var summaryResult = await _conductService.GetClassConductSummaryAsync(
                targetClassId, targetSemesterId, currentUserId, userRole);

            if (summaryResult.Success && summaryResult.Data != null)
            {
                var summary = summaryResult.Data;

                // Áp dụng bộ lọc hiển thị (Filter)
                if (!string.IsNullOrWhiteSpace(status) && status != "All")
                {
                    if (status == "Evaluated")
                        summary.Students = summary.Students.Where(s => s.IsEvaluated).ToList();
                    else if (status == "NotEvaluated")
                        summary.Students = summary.Students.Where(s => !s.IsEvaluated).ToList();
                }

                if (!string.IsNullOrWhiteSpace(grade) && grade != "All")
                {
                    summary.Students = summary.Students.Where(s => string.Equals(s.ConductGrade, grade, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var lowerSearch = search.Trim().ToLower();
                    summary.Students = summary.Students.Where(s =>
                        s.FullName.ToLower().Contains(lowerSearch) ||
                        s.StudentCode.ToLower().Contains(lowerSearch)).ToList();
                }

                model.Summary = summary;
            }
            else
            {
                TempData["ErrorMessage"] = summaryResult.Message;
            }
        }

        return View(model);
    }

    // =========================================================================
    // 2. ĐÁNH GIÁ HÀNG LOẠT CẢ LỚP (BULK ENTRY)
    // =========================================================================
    [HttpGet]
    [Authorize(Roles = "Teacher,Principal,Staff")]
    public async Task<IActionResult> BulkEntry(int classId, int? semesterId)
    {
        int currentUserId = ResolveCurrentUserId();
        string userRole = User.FindFirstValue(ClaimTypes.Role) ?? "Teacher";

        var semestersList = await ResolveSemestersListAsync();
        int targetSemesterId = semesterId ?? (semestersList.FirstOrDefault(s => s.IsActive)?.Id ?? (semestersList.FirstOrDefault()?.Id ?? 0));

        var summaryResult = await _conductService.GetClassConductSummaryAsync(
            classId, targetSemesterId, currentUserId, userRole);

        if (!summaryResult.Success || summaryResult.Data == null)
        {
            TempData["ErrorMessage"] = summaryResult.Message ?? "Không thể tải danh sách học sinh để đánh giá.";
            return RedirectToAction(nameof(Index), new { classId, semesterId = targetSemesterId });
        }

        var summary = summaryResult.Data;
        var model = new ConductBulkEntryViewModel
        {
            ClassId = summary.ClassId,
            SemesterId = summary.SemesterId,
            ClassName = summary.ClassName,
            SemesterName = summary.SemesterName,
            AcademicYearCode = summary.AcademicYearCode,
            HomeroomTeacherName = summary.HomeroomTeacherName,
            Students = summary.Students.Select(s => new ConductBulkEntryItemViewModel
            {
                StudentId = s.StudentId,
                StudentCode = s.StudentCode,
                FullName = s.FullName,
                DateOfBirth = s.DateOfBirth,
                Gender = s.Gender,
                Gpa = s.Gpa,
                AcademicRank = s.AcademicRank,
                AbsentsCount = s.AbsentsCount,
                ExcusedCount = s.ExcusedCount,
                LateCount = s.LateCount,
                ConductGrade = s.ConductGrade,
                TeacherComment = s.TeacherComment
            }).ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Teacher,Principal,Staff")]
    public async Task<IActionResult> SaveBulk(ConductBulkEntryViewModel model)
    {
        int currentUserId = ResolveCurrentUserId();
        string userRole = User.FindFirstValue(ClaimTypes.Role) ?? "Teacher";

        var bulkDto = new BulkSaveConductDto
        {
            ClassId = model.ClassId,
            SemesterId = model.SemesterId,
            Items = model.Students.Select(s => new SaveStudentConductDto
            {
                StudentId = s.StudentId,
                SemesterId = model.SemesterId,
                ConductGrade = s.ConductGrade ?? string.Empty,
                TeacherComment = s.TeacherComment
            }).ToList()
        };

        var result = await _conductService.BulkSaveConductAsync(bulkDto, currentUserId, userRole);
        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message ?? "Đã lưu thành công kết quả đánh giá hạnh kiểm cả lớp.";
            return RedirectToAction(nameof(Index), new { classId = model.ClassId, semesterId = model.SemesterId });
        }

        TempData["ErrorMessage"] = result.Message;
        return View("BulkEntry", model);
    }

    // =========================================================================
    // 3. ĐÁNH GIÁ & CẬP NHẬT ĐƠN LẺ / NHẬN XÉT (AJAX POST)
    // =========================================================================
    [HttpPost]
    [Authorize(Roles = "Teacher,Principal,Staff")]
    public async Task<IActionResult> SaveSingle([FromBody] SaveStudentConductDto dto)
    {
        if (!ModelState.IsValid)
        {
            return Json(new { success = false, message = "Dữ liệu nhập không hợp lệ." });
        }

        int currentUserId = ResolveCurrentUserId();
        string userRole = User.FindFirstValue(ClaimTypes.Role) ?? "Teacher";

        var result = await _conductService.SaveStudentConductAsync(dto, currentUserId, userRole);
        if (result.Success)
        {
            return Json(new { success = true, message = result.Message, data = result.Data });
        }

        return Json(new { success = false, message = result.Message });
    }

    // =========================================================================
    // 4. LỊCH SỬ HẠNH KIỂM CỦA HỌC SINH (HISTORY)
    // =========================================================================
    [HttpGet]
    public async Task<IActionResult> History(int studentId)
    {
        int currentUserId = ResolveCurrentUserId();
        string userRole = User.FindFirstValue(ClaimTypes.Role) ?? "Student";

        var student = await _studentRepository.GetByIdAsync(studentId);
        if (student == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy thông tin học sinh.";
            return RedirectToAction(nameof(Index));
        }

        var historyResult = await _conductService.GetStudentConductHistoryAsync(studentId, currentUserId, userRole);
        if (!historyResult.Success)
        {
            TempData["ErrorMessage"] = historyResult.Message;
            return RedirectToAction(nameof(Index));
        }

        var studentClass = student.StudentClasses.FirstOrDefault(sc => sc.Status == "Enrolled" && sc.LeftAt == null);

        var model = new ConductHistoryViewModel
        {
            StudentId = student.Id,
            StudentCode = student.StudentCode,
            FullName = student.User?.FullName ?? "N/A",
            ClassName = studentClass?.Class?.ClassName ?? "N/A",
            History = (historyResult.Data ?? Enumerable.Empty<StudentConductHistoryItemDto>()).ToList()
        };

        return View(model);
    }

    // =========================================================================
    // 5. HỌC SINH XEM HẠNH KIỂM CỦA MÌNH (STUDENT VIEW)
    // =========================================================================
    [HttpGet]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> MyConduct(int? semesterId)
    {
        int currentUserId = ResolveCurrentUserId();
        var student = await _studentRepository.GetByUserIdAsync(currentUserId);
        if (student == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy hồ sơ học sinh của tài khoản này.";
            return RedirectToAction("Dashboard", "Student");
        }

        var semestersList = await ResolveSemestersListAsync();
        int targetSemesterId = semesterId ?? (semestersList.FirstOrDefault(s => s.IsActive)?.Id ?? (semestersList.FirstOrDefault()?.Id ?? 0));

        var model = new StudentConductViewModel
        {
            StudentId = student.Id,
            SelectedSemesterId = targetSemesterId,
            AvailableSemesters = semestersList
        };

        if (targetSemesterId > 0)
        {
            var detailResult = await _conductService.GetStudentConductAsync(student.Id, targetSemesterId, currentUserId, "Student");
            if (detailResult.Success)
            {
                model.Detail = detailResult.Data;
            }

            var historyResult = await _conductService.GetStudentConductHistoryAsync(student.Id, currentUserId, "Student");
            if (historyResult.Success && historyResult.Data != null)
            {
                model.History = historyResult.Data.ToList();
            }
        }

        return View(model);
    }

    // =========================================================================
    // 6. PHỤ HUYNH XEM HẠNH KIỂM CỦA CON EM (PARENT VIEW)
    // =========================================================================
    [HttpGet]
    [Authorize(Roles = "Parent")]
    public async Task<IActionResult> StudentConduct(int? studentId, int? semesterId)
    {
        int currentUserId = ResolveCurrentUserId();
        var parent = await _parentRepository.GetByUserIdAsync(currentUserId);
        if (parent == null || !parent.Students.Any())
        {
            TempData["ErrorMessage"] = "Không tìm thấy thông tin con em gắn với tài khoản này.";
            return RedirectToAction("Dashboard", "Parent");
        }

        var children = parent.Students.ToList();
        var semestersList = await ResolveSemestersListAsync();

        int targetStudentId = studentId ?? children.First().Id;
        if (!children.Any(c => c.Id == targetStudentId))
        {
            targetStudentId = children.First().Id;
        }

        int targetSemesterId = semesterId ?? (semestersList.FirstOrDefault(s => s.IsActive)?.Id ?? (semestersList.FirstOrDefault()?.Id ?? 0));

        var model = new ParentConductViewModel
        {
            SelectedStudentId = targetStudentId,
            SelectedSemesterId = targetSemesterId,
            Children = children.Select(c => new ParentStudentOption
            {
                Id = c.Id,
                StudentCode = c.StudentCode,
                FullName = c.User?.FullName ?? string.Empty
            }).ToList(),
            AvailableSemesters = semestersList
        };

        if (targetStudentId > 0 && targetSemesterId > 0)
        {
            var detailResult = await _conductService.GetStudentConductAsync(targetStudentId, targetSemesterId, currentUserId, "Parent");
            if (detailResult.Success)
            {
                model.Detail = detailResult.Data;
            }

            var historyResult = await _conductService.GetStudentConductHistoryAsync(targetStudentId, currentUserId, "Parent");
            if (historyResult.Success && historyResult.Data != null)
            {
                model.History = historyResult.Data.ToList();
            }
        }

        return View(model);
    }

    // =========================================================================
    // 7. BÁO CÁO GIÁM SÁT TOÀN TRƯỜNG CHO HIỆU TRƯỞNG & GIÁO VỤ (PRINCIPAL OVERVIEW)
    // =========================================================================
    [HttpGet]
    [Authorize(Roles = "Principal,Staff")]
    public async Task<IActionResult> PrincipalSummary(int? semesterId)
    {
        var semestersList = await ResolveSemestersListAsync();
        int targetSemesterId = semesterId ?? (semestersList.FirstOrDefault(s => s.IsActive)?.Id ?? (semestersList.FirstOrDefault()?.Id ?? 0));

        var model = new PrincipalConductSummaryViewModel
        {
            SelectedSemesterId = targetSemesterId,
            AvailableSemesters = semestersList
        };

        if (targetSemesterId > 0)
        {
            var overviewResult = await _conductService.GetPrincipalOverviewAsync(targetSemesterId);
            if (overviewResult.Success)
            {
                model.Overview = overviewResult.Data;
            }
            else
            {
                TempData["ErrorMessage"] = overviewResult.Message;
            }
        }

        return View(model);
    }

    // =========================================================================
    // PRIVATE HELPERS
    // =========================================================================

    private int ResolveCurrentUserId()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(idClaim, out var id) ? id : 0;
    }

    private async Task<(List<ClassItemOption> Classes, bool IsHomeroom)> ResolveAvailableClassesAsync(int currentUserId, string userRole)
    {
        if (userRole == "Principal" || userRole == "Staff")
        {
            var allClasses = await _classRepository.GetByFiltersAsync(null, null);
            var list = allClasses.Select(c => new ClassItemOption
            {
                Id = c.Id,
                ClassName = c.ClassName,
                AcademicYearCode = c.AcademicYear?.YearCode ?? string.Empty
            }).OrderBy(c => c.ClassName).ToList();

            return (list, false);
        }

        // Với Teacher: chỉ lấy các lớp chủ nhiệm
        var teacher = await _teacherRepository.GetByUserIdAsync(currentUserId);
        if (teacher == null)
        {
            return (new List<ClassItemOption>(), false);
        }

        var homeroomClasses = await _classRepository.GetHomeroomClassesByTeacherIdAsync(teacher.Id);
        var teacherList = homeroomClasses.Select(c => new ClassItemOption
        {
            Id = c.Id,
            ClassName = c.ClassName,
            AcademicYearCode = c.AcademicYear?.YearCode ?? string.Empty
        }).OrderBy(c => c.ClassName).ToList();

        return (teacherList, true);
    }

    private async Task<List<SemesterItemOption>> ResolveSemestersListAsync()
    {
        var allYears = await _academicYearRepository.GetAllAsync();
        var semestersList = new List<SemesterItemOption>();

        foreach (var yr in allYears.OrderByDescending(y => y.StartDate))
        {
            foreach (var sem in yr.Semesters.OrderBy(s => s.SemesterName))
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

        return semestersList;
    }
}
