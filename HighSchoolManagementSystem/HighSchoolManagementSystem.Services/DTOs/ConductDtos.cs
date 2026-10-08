using System.ComponentModel.DataAnnotations;

namespace HighSchoolManagementSystem.Services.DTOs;

public class ConductEvaluationItemDto
{
    public int? Id { get; set; }
    public int StudentId { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public decimal? Gpa { get; set; }
    public string? AcademicRank { get; set; }
    public int AbsentsCount { get; set; }
    public int ExcusedCount { get; set; }
    public int LateCount { get; set; }
    public string? ConductGrade { get; set; }
    public string? TeacherComment { get; set; }
    public DateTime? EvaluatedAt { get; set; }
    public string? EvaluatedByTeacherName { get; set; }
    public bool IsEvaluated => !string.IsNullOrWhiteSpace(ConductGrade);
}

public class ClassConductSummaryDto
{
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string AcademicYearCode { get; set; } = string.Empty;
    public string GradeLevelName { get; set; } = string.Empty;
    public int SemesterId { get; set; }
    public string SemesterName { get; set; } = string.Empty;
    public int HomeroomTeacherId { get; set; }
    public string HomeroomTeacherName { get; set; } = string.Empty;
    public int TotalStudents { get; set; }
    public int EvaluatedCount { get; set; }
    public int TotCount { get; set; }
    public int KhaCount { get; set; }
    public int DatCount { get; set; }
    public int ChuaDatCount { get; set; }
    public List<ConductEvaluationItemDto> Students { get; set; } = new();
}

public class SaveStudentConductDto
{
    [Required]
    public int StudentId { get; set; }

    [Required]
    public int SemesterId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn xếp loại hạnh kiểm")]
    [MaxLength(20)]
    public string ConductGrade { get; set; } = string.Empty; // Tốt, Khá, Đạt, Chưa đạt

    [MaxLength(500, ErrorMessage = "Nhận xét không được vượt quá 500 ký tự")]
    public string? TeacherComment { get; set; }
}

public class BulkSaveConductDto
{
    [Required]
    public int ClassId { get; set; }

    [Required]
    public int SemesterId { get; set; }

    public List<SaveStudentConductDto> Items { get; set; } = new();
}

public class StudentConductDetailDto
{
    public int StudentId { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string AcademicYearCode { get; set; } = string.Empty;
    public int SemesterId { get; set; }
    public string SemesterName { get; set; } = string.Empty;
    public string? ConductGrade { get; set; }
    public string? TeacherComment { get; set; }
    public DateTime? EvaluatedAt { get; set; }
    public string? HomeroomTeacherName { get; set; }
    public decimal? Gpa { get; set; }
    public string? AcademicRank { get; set; }
    public int AbsentsCount { get; set; }
    public int ExcusedCount { get; set; }
    public int LateCount { get; set; }
}

public class StudentConductHistoryItemDto
{
    public int EvaluationId { get; set; }
    public int SemesterId { get; set; }
    public string SemesterName { get; set; } = string.Empty;
    public string AcademicYearCode { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string ConductGrade { get; set; } = string.Empty;
    public string? TeacherComment { get; set; }
    public string HomeroomTeacherName { get; set; } = string.Empty;
    public DateTime EvaluatedAt { get; set; }
}

public class ClassConductProgressDto
{
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string GradeLevelName { get; set; } = string.Empty;
    public string HomeroomTeacherName { get; set; } = string.Empty;
    public int TotalStudents { get; set; }
    public int EvaluatedCount { get; set; }
    public int TotCount { get; set; }
    public int KhaCount { get; set; }
    public int DatCount { get; set; }
    public int ChuaDatCount { get; set; }
    public bool IsCompleted => TotalStudents > 0 && EvaluatedCount == TotalStudents;
    public double CompletionRate => TotalStudents > 0 ? Math.Round((double)EvaluatedCount / TotalStudents * 100, 1) : 0;
}

public class PrincipalConductOverviewDto
{
    public int SemesterId { get; set; }
    public string SemesterName { get; set; } = string.Empty;
    public string AcademicYearCode { get; set; } = string.Empty;
    public int TotalClasses { get; set; }
    public int TotalStudents { get; set; }
    public int TotalEvaluated { get; set; }
    public int TotCount { get; set; }
    public int KhaCount { get; set; }
    public int DatCount { get; set; }
    public int ChuaDatCount { get; set; }
    public double OverallCompletionRate => TotalStudents > 0 ? Math.Round((double)TotalEvaluated / TotalStudents * 100, 1) : 0;
    public List<ClassConductProgressDto> ClassProgress { get; set; } = new();
}
