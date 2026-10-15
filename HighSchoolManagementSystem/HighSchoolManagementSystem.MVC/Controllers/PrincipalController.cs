using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using HighSchoolManagementSystem.MVC.Models.ViewModels.Principal;
using HighSchoolManagementSystem.Services.Interfaces;

namespace HighSchoolManagementSystem.MVC.Controllers;

[Authorize(Roles = "Principal")]
public class PrincipalController : Controller
{
    private readonly IPrincipalService _principalService;
    private readonly ISemesterService _semesterService;
    private readonly IGradeLevelService _gradeLevelService;
    private readonly IClassService _classService;
    private readonly ISubjectService _subjectService;
    private readonly ILogger<PrincipalController> _logger;

    public PrincipalController(
        IPrincipalService principalService,
        ISemesterService semesterService,
        IGradeLevelService gradeLevelService,
        IClassService classService,
        ISubjectService subjectService,
        ILogger<PrincipalController> logger)
    {
        _principalService = principalService;
        _semesterService = semesterService;
        _gradeLevelService = gradeLevelService;
        _classService = classService;
        _subjectService = subjectService;
        _logger = logger;
    }

    // =========================================================================
    // 1. DASHBOARD HIỆU TRƯỞNG
    // =========================================================================
    public async Task<IActionResult> Dashboard()
    {
        var result = await _principalService.GetDashboardStatsAsync();
        if (!result.Success || result.Data == null)
        {
            TempData["ErrorMessage"] = result.Message ?? "Không thể tải số liệu Dashboard.";
            return View(new PrincipalDashboardViewModel());
        }

        var d = result.Data;
        var model = new PrincipalDashboardViewModel
        {
            TotalStudents = d.TotalStudents,
            StudentsByGrade = d.StudentsByGrade,
            TotalTeachers = d.TotalTeachers,
            ActiveTeachers = d.ActiveTeachers,
            TotalClasses = d.TotalClasses,
            ClassesByGrade = d.ClassesByGrade,
            TotalRooms = d.TotalRooms,
            TodayTotalAttendanceRecords = d.TodayTotalAttendanceRecords,
            TodayPresentCount = d.TodayPresentCount,
            TodayExcusedCount = d.TodayExcusedCount,
            TodayUnexcusedCount = d.TodayUnexcusedCount,
            TodayAttendanceRate = d.TodayAttendanceRate,
            AttendanceByGrade = d.AttendanceByGrade,
            RecentConductEvents = d.RecentConductEvents,
            ConductDistribution = d.ConductDistribution,
            CurrentYearCode = d.CurrentYearCode,
            CurrentSemesterName = d.CurrentSemesterName,
            CurrentSemesterId = d.CurrentSemesterId
        };

        return View(model);
    }

    // =========================================================================
    // 2. RÀ SOÁT, PHÊ DUYỆT & CHỐT SỔ ĐIỂM HỌC KỲ (WBS 17.7 - 17.10)
    // =========================================================================
    public async Task<IActionResult> AcademicReview(int? semesterId, int? gradeLevelId, int? classId)
    {
        var semestersResult = await _semesterService.GetAllAsync();
        var semesters = semestersResult.Data ?? new List<HighSchoolManagementSystem.Models.Entities.Semester>();

        int targetSemesterId = semesterId ?? (semesters.FirstOrDefault(s => s.IsActive)?.Id ?? semesters.FirstOrDefault()?.Id ?? 0);

        var reviewResult = await _principalService.GetAcademicReviewDataAsync(targetSemesterId, gradeLevelId, classId);
        if (!reviewResult.Success || reviewResult.Data == null)
        {
            TempData["ErrorMessage"] = reviewResult.Message ?? "Không thể tải dữ liệu rà soát kết quả học tập.";
            return RedirectToAction(nameof(Dashboard));
        }

        var d = reviewResult.Data;
        var model = new AcademicReviewViewModel
        {
            SemesterId = d.SemesterId,
            SemesterName = d.SemesterName,
            AcademicYearCode = d.AcademicYearCode,
            IsSemesterFinalized = d.IsSemesterFinalized,
            TotalClasses = d.TotalClasses,
            TotalStudents = d.TotalStudents,
            EvaluatedStudentsCount = d.EvaluatedStudentsCount,
            SchoolAverageGpa = d.SchoolAverageGpa,
            AcademicRankCounts = d.AcademicRankCounts,
            ClassReviews = d.ClassReviews,
            SelectedGradeLevelId = gradeLevelId,
            SelectedClassId = classId
        };

        await PopulateReviewDropdownsAsync(model, semesters, targetSemesterId, gradeLevelId, classId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FinalizeSemester(int semesterId)
    {
        int principalUserId = ResolveCurrentUserId();
        var result = await _principalService.FinalizeSemesterAsync(semesterId, principalUserId);

        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(AcademicReview), new { semesterId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnlockSemester(int semesterId)
    {
        int principalUserId = ResolveCurrentUserId();
        var result = await _principalService.UnlockSemesterAsync(semesterId, principalUserId);

        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(AcademicReview), new { semesterId });
    }

    // =========================================================================
    // 3. HỆ THỐNG BÁO CÁO HỌC TẬP TOÀN TRƯỜNG (WBS 18.1 - 18.4)
    // =========================================================================
    public async Task<IActionResult> AcademicReports(int? semesterId, int? gradeLevelId, int? subjectId)
    {
        var semestersResult = await _semesterService.GetAllAsync();
        var semesters = semestersResult.Data ?? new List<HighSchoolManagementSystem.Models.Entities.Semester>();
        int targetSemesterId = semesterId ?? (semesters.FirstOrDefault(s => s.IsActive)?.Id ?? semesters.FirstOrDefault()?.Id ?? 0);

        var reportResult = await _principalService.GetAcademicReportDataAsync(targetSemesterId, gradeLevelId, subjectId);
        if (!reportResult.Success || reportResult.Data == null)
        {
            TempData["ErrorMessage"] = reportResult.Message ?? "Không thể tải báo cáo học tập.";
            return RedirectToAction(nameof(Dashboard));
        }

        var d = reportResult.Data;
        var model = new AcademicReportsViewModel
        {
            SemesterId = d.SemesterId,
            SemesterName = d.SemesterName,
            AcademicYearCode = d.AcademicYearCode,
            GradeSummaries = d.GradeSummaries,
            SubjectSummaries = d.SubjectSummaries,
            TopStudents = d.TopStudents,
            SelectedGradeLevelId = gradeLevelId,
            SelectedSubjectId = subjectId
        };

        model.SemesterOptions = semesters.Select(s => new SelectListItem
        {
            Value = s.Id.ToString(),
            Text = $"{s.SemesterName} ({s.AcademicYear?.YearCode})",
            Selected = s.Id == targetSemesterId
        }).ToList();

        var gradesResult = await _gradeLevelService.GetAllAsync();
        model.GradeLevelOptions = gradesResult.Data?.Select(g => new SelectListItem
        {
            Value = g.Id.ToString(),
            Text = g.LevelName,
            Selected = g.Id == gradeLevelId
        }).ToList() ?? new();
        model.GradeLevelOptions.Insert(0, new SelectListItem { Value = "", Text = "-- Tất cả các khối --" });

        var subjectsResult = await _subjectService.GetAllSubjectsAsync();
        model.SubjectOptions = subjectsResult.Data?.Select(s => new SelectListItem
        {
            Value = s.Id.ToString(),
            Text = $"{s.SubjectName} ({s.SubjectCode})",
            Selected = s.Id == subjectId
        }).ToList() ?? new();
        model.SubjectOptions.Insert(0, new SelectListItem { Value = "", Text = "-- Tất cả các môn học --" });

        return View(model);
    }

    // =========================================================================
    // 4. BÁO CÁO CHUYÊN CẦN TOÀN TRƯỜNG (WBS 18.5)
    // =========================================================================
    public async Task<IActionResult> AttendanceReports(int? semesterId, int? gradeLevelId, DateTime? fromDate, DateTime? toDate)
    {
        var semestersResult = await _semesterService.GetAllAsync();
        var semesters = semestersResult.Data ?? new List<HighSchoolManagementSystem.Models.Entities.Semester>();

        var reportResult = await _principalService.GetAttendanceReportDataAsync(semesterId, gradeLevelId, fromDate, toDate);
        if (!reportResult.Success || reportResult.Data == null)
        {
            TempData["ErrorMessage"] = reportResult.Message ?? "Không thể tải báo cáo chuyên cần.";
            return RedirectToAction(nameof(Dashboard));
        }

        var d = reportResult.Data;
        var model = new AttendanceReportsViewModel
        {
            SemesterId = d.SemesterId,
            SemesterName = d.SemesterName,
            FromDate = d.FromDate,
            ToDate = d.ToDate,
            TotalRecords = d.TotalRecords,
            TotalPresent = d.TotalPresent,
            TotalExcused = d.TotalExcused,
            TotalUnexcused = d.TotalUnexcused,
            OverallAttendanceRate = d.OverallAttendanceRate,
            ClassStats = d.ClassStats,
            SelectedGradeLevelId = gradeLevelId
        };

        model.SemesterOptions = semesters.Select(s => new SelectListItem
        {
            Value = s.Id.ToString(),
            Text = $"{s.SemesterName} ({s.AcademicYear?.YearCode})",
            Selected = semesterId.HasValue && s.Id == semesterId.Value
        }).ToList();
        model.SemesterOptions.Insert(0, new SelectListItem { Value = "", Text = "-- Mặc định (Tất cả kỳ) --" });

        var gradesResult = await _gradeLevelService.GetAllAsync();
        model.GradeLevelOptions = gradesResult.Data?.Select(g => new SelectListItem
        {
            Value = g.Id.ToString(),
            Text = g.LevelName,
            Selected = g.Id == gradeLevelId
        }).ToList() ?? new();
        model.GradeLevelOptions.Insert(0, new SelectListItem { Value = "", Text = "-- Tất cả các khối --" });

        return View(model);
    }

    // =========================================================================
    // 5. XUẤT BÁO CÁO CSV / EXCEL (WBS 18.9)
    // =========================================================================
    public async Task<IActionResult> ExportAcademicReportCsv(int semesterId, int? gradeLevelId = null)
    {
        var result = await _principalService.ExportAcademicReportCsvAsync(semesterId, gradeLevelId);
        if (!result.Success || result.Data == null)
        {
            TempData["ErrorMessage"] = result.Message ?? "Không thể xuất file báo cáo.";
            return RedirectToAction(nameof(AcademicReports), new { semesterId, gradeLevelId });
        }

        string fileName = $"BaoCaoHocTap_HK{semesterId}_{DateTime.Now:yyyyMMdd_HHmm}.csv";
        return File(result.Data, "text/csv; charset=utf-8", fileName);
    }

    public async Task<IActionResult> ExportAttendanceReportCsv(int? semesterId = null, int? gradeLevelId = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var result = await _principalService.ExportAttendanceReportCsvAsync(semesterId, gradeLevelId, fromDate, toDate);
        if (!result.Success || result.Data == null)
        {
            TempData["ErrorMessage"] = result.Message ?? "Không thể xuất file báo cáo.";
            return RedirectToAction(nameof(AttendanceReports), new { semesterId, gradeLevelId, fromDate, toDate });
        }

        string fileName = $"BaoCaoChuyenCan_{DateTime.Now:yyyyMMdd_HHmm}.csv";
        return File(result.Data, "text/csv; charset=utf-8", fileName);
    }

    // =========================================================================
    // PRIVATE HELPERS
    // =========================================================================
    private int ResolveCurrentUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdValue, out var id) ? id : 0;
    }

    private async Task PopulateReviewDropdownsAsync(
        AcademicReviewViewModel model,
        IEnumerable<HighSchoolManagementSystem.Models.Entities.Semester> semesters,
        int selectedSemesterId,
        int? gradeLevelId,
        int? classId)
    {
        model.SemesterOptions = semesters.Select(s => new SelectListItem
        {
            Value = s.Id.ToString(),
            Text = $"{s.SemesterName} ({s.AcademicYear?.YearCode})",
            Selected = s.Id == selectedSemesterId
        }).ToList();

        var gradesResult = await _gradeLevelService.GetAllAsync();
        model.GradeLevelOptions = gradesResult.Data?.Select(g => new SelectListItem
        {
            Value = g.Id.ToString(),
            Text = g.LevelName,
            Selected = g.Id == gradeLevelId
        }).ToList() ?? new();
        model.GradeLevelOptions.Insert(0, new SelectListItem { Value = "", Text = "-- Tất cả các khối --" });

        int? academicYearId = semesters.FirstOrDefault(s => s.Id == selectedSemesterId)?.AcademicYearId;
        var classesResult = await _classService.GetByFiltersAsync(academicYearId, gradeLevelId);
        var classes = classesResult.Data ?? new List<HighSchoolManagementSystem.Models.Entities.Class>();

        model.ClassOptions = classes.Select(c => new SelectListItem
        {
            Value = c.Id.ToString(),
            Text = c.ClassName,
            Selected = c.Id == classId
        }).ToList();
        model.ClassOptions.Insert(0, new SelectListItem { Value = "", Text = "-- Tất cả các lớp --" });
    }
}
