namespace HighSchoolManagementSystem.Services.DTOs;

public class PrincipalDashboardDto
{
    public int TotalStudents { get; set; }
    public Dictionary<string, int> StudentsByGrade { get; set; } = new();

    public int TotalTeachers { get; set; }
    public int ActiveTeachers { get; set; }

    public int TotalClasses { get; set; }
    public Dictionary<string, int> ClassesByGrade { get; set; } = new();

    public int TotalRooms { get; set; }

    // Attendance KPI
    public int TodayTotalAttendanceRecords { get; set; }
    public int TodayPresentCount { get; set; }
    public int TodayExcusedCount { get; set; }
    public int TodayUnexcusedCount { get; set; }
    public decimal TodayAttendanceRate { get; set; }
    public List<GradeAttendanceKpiDto> AttendanceByGrade { get; set; } = new();

    // Disciplinary & Conduct recent
    public List<RecentConductEventDto> RecentConductEvents { get; set; } = new();
    public Dictionary<string, int> ConductDistribution { get; set; } = new();

    // Academic Year & Semester
    public string? CurrentYearCode { get; set; }
    public string? CurrentSemesterName { get; set; }
    public int? CurrentSemesterId { get; set; }
}

public class GradeAttendanceKpiDto
{
    public string GradeName { get; set; } = string.Empty;
    public int TotalStudents { get; set; }
    public int PresentCount { get; set; }
    public int ExcusedCount { get; set; }
    public int UnexcusedCount { get; set; }
    public decimal AttendanceRate { get; set; }
}

public class RecentConductEventDto
{
    public DateTime EvaluatedAt { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentCode { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string ConductGrade { get; set; } = string.Empty;
    public string? TeacherComment { get; set; }
    public string TeacherName { get; set; } = string.Empty;
}

public class AcademicReviewPageDto
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
}

public class ClassAcademicReviewDto
{
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string GradeLevelName { get; set; } = string.Empty;
    public string? HomeroomTeacherName { get; set; }
    public int StudentCount { get; set; }
    public int GradedStudentCount { get; set; }
    public decimal? AverageGpa { get; set; }
    public int ExcellentCount { get; set; }
    public int GoodCount { get; set; }
    public int PassCount { get; set; }
    public int FailCount { get; set; }
    public bool IsFullyGraded => StudentCount > 0 && GradedStudentCount >= StudentCount;
}

public class AcademicReportDto
{
    public int SemesterId { get; set; }
    public string SemesterName { get; set; } = string.Empty;
    public string AcademicYearCode { get; set; } = string.Empty;
    public List<GradeAcademicSummaryDto> GradeSummaries { get; set; } = new();
    public List<SubjectPerformanceSummaryDto> SubjectSummaries { get; set; } = new();
    public List<StudentRankingItemDto> TopStudents { get; set; } = new();
}

public class GradeAcademicSummaryDto
{
    public string GradeName { get; set; } = string.Empty;
    public int TotalStudents { get; set; }
    public decimal? AverageGpa { get; set; }
    public int ExcellentCount { get; set; }
    public int GoodCount { get; set; }
    public int PassCount { get; set; }
    public int FailCount { get; set; }
}

public class SubjectPerformanceSummaryDto
{
    public int SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public string SubjectCode { get; set; } = string.Empty;
    public int TotalAssessments { get; set; }
    public decimal? AverageScore { get; set; }
    public decimal PassRate { get; set; }
    public decimal ExcellentRate { get; set; }
}

public class StudentRankingItemDto
{
    public int Rank { get; set; }
    public int StudentId { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public decimal? Gpa { get; set; }
    public string AcademicRank { get; set; } = string.Empty;
    public string ConductGrade { get; set; } = string.Empty;
}

public class AttendanceReportDto
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
}

public class ClassAttendanceStatDto
{
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string GradeName { get; set; } = string.Empty;
    public int TotalStudents { get; set; }
    public int TotalAbsents { get; set; }
    public int TotalLate { get; set; }
    public decimal AttendanceRate { get; set; }
}
