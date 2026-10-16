using Microsoft.AspNetCore.Mvc.Rendering;
using HighSchoolManagementSystem.Services.DTOs;

namespace HighSchoolManagementSystem.MVC.Models.ViewModels.Principal;

public class PrincipalDashboardViewModel
{
    public int TotalStudents { get; set; }
    public Dictionary<string, int> StudentsByGrade { get; set; } = new();

    public int TotalTeachers { get; set; }
    public int ActiveTeachers { get; set; }

    public int TotalClasses { get; set; }
    public Dictionary<string, int> ClassesByGrade { get; set; } = new();

    public int TotalRooms { get; set; }

    public int TodayTotalAttendanceRecords { get; set; }
    public int TodayPresentCount { get; set; }
    public int TodayExcusedCount { get; set; }
    public int TodayUnexcusedCount { get; set; }
    public decimal TodayAttendanceRate { get; set; }
    public List<GradeAttendanceKpiDto> AttendanceByGrade { get; set; } = new();

    public List<RecentConductEventDto> RecentConductEvents { get; set; } = new();
    public Dictionary<string, int> ConductDistribution { get; set; } = new();

    public string? CurrentYearCode { get; set; }
    public string? CurrentSemesterName { get; set; }
    public int? CurrentSemesterId { get; set; }
}

public class AcademicReviewViewModel
{
    public int SemesterId { get; set; }
    public string SemesterName { get; set; } = string.Empty;
    public string AcademicYearCode { get; set; } = string.Empty;
    public bool IsSemesterFinalized { get; set; }
    public int TotalClasses { get; set; }
    public int TotalStudents { get; set; }
    public int EvaluatedStudentsCount { get; set; }
    public decimal SchoolAverageGpa { get; set; }

    public Dictionary<string, int> AcademicRankCounts { get; set; } = new();
    public List<ClassAcademicReviewDto> ClassReviews { get; set; } = new();

    // Filters & Dropdowns
    public int? SelectedGradeLevelId { get; set; }
    public int? SelectedClassId { get; set; }
    public List<SelectListItem> SemesterOptions { get; set; } = new();
    public List<SelectListItem> GradeLevelOptions { get; set; } = new();
    public List<SelectListItem> ClassOptions { get; set; } = new();
}

public class AcademicReportsViewModel
{
    public int SemesterId { get; set; }
    public string SemesterName { get; set; } = string.Empty;
    public string AcademicYearCode { get; set; } = string.Empty;

    public List<GradeAcademicSummaryDto> GradeSummaries { get; set; } = new();
    public List<SubjectPerformanceSummaryDto> SubjectSummaries { get; set; } = new();
    public List<StudentRankingItemDto> TopStudents { get; set; } = new();

    // Filters & Dropdowns
    public int? SelectedGradeLevelId { get; set; }
    public int? SelectedSubjectId { get; set; }
    public List<SelectListItem> SemesterOptions { get; set; } = new();
    public List<SelectListItem> GradeLevelOptions { get; set; } = new();
    public List<SelectListItem> SubjectOptions { get; set; } = new();
}

public class AttendanceReportsViewModel
{
    public int? SemesterId { get; set; }
    public string SemesterName { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }

    public int TotalRecords { get; set; }
    public int TotalPresent { get; set; }
    public int TotalExcused { get; set; }
    public int TotalUnexcused { get; set; }
    public decimal OverallAttendanceRate { get; set; }

    public List<ClassAttendanceStatDto> ClassStats { get; set; } = new();

    // Filters & Dropdowns
    public int? SelectedGradeLevelId { get; set; }
    public List<SelectListItem> SemesterOptions { get; set; } = new();
    public List<SelectListItem> GradeLevelOptions { get; set; } = new();
}

public class YearEndPromotionViewModel
{
    public int SourceAcademicYearId { get; set; }
    public string SourceAcademicYearCode { get; set; } = string.Empty;

    public int? SourceClassId { get; set; }
    public string? SourceClassName { get; set; }
    public int? SourceGradeLevelId { get; set; }
    public string? SourceGradeLevelName { get; set; }

    public bool IsFinalGrade { get; set; }
    public int? NextGradeLevelId { get; set; }
    public string? NextGradeLevelName { get; set; }

    public int? TargetAcademicYearId { get; set; }
    public string? TargetAcademicYearCode { get; set; }

    public int? PromoteTargetClassId { get; set; }
    public int? RepeatTargetClassId { get; set; }

    public bool AllSemestersFinalized { get; set; }
    public List<SemesterFinalizationStatusDto> SourceSemesters { get; set; } = new();

    public int TotalStudents { get; set; }
    public int SuggestedPromoteOrGraduateCount { get; set; }
    public int SuggestedRepeatCount { get; set; }
    public int AlreadyProcessedCount { get; set; }

    public List<StudentYearEndPromotionItemDto> Students { get; set; } = new();

    // Dropdowns
    public List<SelectListItem> SourceAcademicYearOptions { get; set; } = new();
    public List<SelectListItem> SourceClassOptions { get; set; } = new();
    public List<SelectListItem> TargetAcademicYearOptions { get; set; } = new();
    public List<SelectListItem> PromoteTargetClassOptions { get; set; } = new();
    public List<SelectListItem> RepeatTargetClassOptions { get; set; } = new();
}
