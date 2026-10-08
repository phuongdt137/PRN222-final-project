using HighSchoolManagementSystem.Services.DTOs;
using HighSchoolManagementSystem.MVC.Models.ViewModels.Score;

namespace HighSchoolManagementSystem.MVC.Models.ViewModels.Conduct;

public class ConductEvaluationListViewModel
{
    public int? SelectedClassId { get; set; }
    public int? SelectedSemesterId { get; set; }
    public string? StatusFilter { get; set; } // "All", "Evaluated", "NotEvaluated"
    public string? GradeFilter { get; set; } // "All", "Tốt", "Khá", "Đạt", "Chưa đạt"
    public string? SearchTerm { get; set; }

    public List<ClassItemOption> AvailableClasses { get; set; } = new();
    public List<SemesterItemOption> AvailableSemesters { get; set; } = new();
    public ClassConductSummaryDto? Summary { get; set; }

    public bool IsHomeroomTeacher { get; set; }
    public bool CanEdit { get; set; }
}

public class ConductBulkEntryViewModel
{
    public int ClassId { get; set; }
    public int SemesterId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string SemesterName { get; set; } = string.Empty;
    public string AcademicYearCode { get; set; } = string.Empty;
    public string HomeroomTeacherName { get; set; } = string.Empty;
    public List<ConductBulkEntryItemViewModel> Students { get; set; } = new();
}

public class ConductBulkEntryItemViewModel
{
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
    public string? ConductGrade { get; set; } // Tốt, Khá, Đạt, Chưa đạt
    public string? TeacherComment { get; set; }
}

public class StudentConductViewModel
{
    public int StudentId { get; set; }
    public int SelectedSemesterId { get; set; }
    public List<SemesterItemOption> AvailableSemesters { get; set; } = new();
    public StudentConductDetailDto? Detail { get; set; }
    public List<StudentConductHistoryItemDto> History { get; set; } = new();
}

public class ParentConductViewModel
{
    public int? SelectedStudentId { get; set; }
    public int? SelectedSemesterId { get; set; }
    public List<ParentStudentOption> Children { get; set; } = new();
    public List<SemesterItemOption> AvailableSemesters { get; set; } = new();
    public StudentConductDetailDto? Detail { get; set; }
    public List<StudentConductHistoryItemDto> History { get; set; } = new();
}

public class ConductHistoryViewModel
{
    public int StudentId { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public List<StudentConductHistoryItemDto> History { get; set; } = new();
}

public class PrincipalConductSummaryViewModel
{
    public int? SelectedSemesterId { get; set; }
    public List<SemesterItemOption> AvailableSemesters { get; set; } = new();
    public PrincipalConductOverviewDto? Overview { get; set; }
}
