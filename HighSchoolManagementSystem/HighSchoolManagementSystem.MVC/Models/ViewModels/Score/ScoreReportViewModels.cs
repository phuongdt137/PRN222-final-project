using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Services.DTOs;

namespace HighSchoolManagementSystem.MVC.Models.ViewModels.Score;

public class SubjectScoreMatrixViewModel
{
    public int SelectedAssignmentId { get; set; }
    public List<TeachingAssignmentSummaryDto> TeacherAssignments { get; set; } = new();
    public SubjectScoreMatrixDto? Matrix { get; set; }
}

public class HomeroomScoresViewModel
{
    public int? SelectedClassId { get; set; }
    public int? SelectedSemesterId { get; set; }
    public List<ClassItemOption> HomeroomClasses { get; set; } = new();
    public List<SemesterItemOption> Semesters { get; set; } = new();
    public HomeroomScoreReportDto? Report { get; set; }
}

public class ClassItemOption
{
    public int Id { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string AcademicYearCode { get; set; } = string.Empty;
}

public class SemesterItemOption
{
    public int Id { get; set; }
    public string SemesterName { get; set; } = string.Empty;
    public string AcademicYearCode { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class StudentTranscriptViewModel
{
    public int StudentId { get; set; }
    public int? CurrentTeacherId { get; set; }
    public int? SelectedSemesterId { get; set; }
    public int? SelectedSubjectId { get; set; }
    public List<SemesterItemOption> AvailableSemesters { get; set; } = new();
    public StudentTranscriptDto? Transcript { get; set; }
}

public class ParentScoresViewModel
{
    public int? SelectedStudentId { get; set; }
    public int? SelectedSemesterId { get; set; }
    public int? SelectedSubjectId { get; set; }
    public List<ParentStudentOption> Children { get; set; } = new();
    public List<SemesterItemOption> AvailableSemesters { get; set; } = new();
    public StudentTranscriptDto? Transcript { get; set; }
}

public class StudentScoreDetailsViewModel
{
    public StudentTranscriptDto Transcript { get; set; } = null!;
    public int? SelectedSubjectId { get; set; }
}

public class ParentStudentOption
{
    public int Id { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}

public class CreateFeedbackInputModel
{
    public int StudentId { get; set; }
    public int SemesterId { get; set; }
    public string FeedbackType { get; set; } = "Academic";
    public string Content { get; set; } = string.Empty;
    public string? ReturnUrl { get; set; }
}

public class EditFeedbackInputModel
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int SemesterId { get; set; }
    public string FeedbackType { get; set; } = "Academic";
    public string Content { get; set; } = string.Empty;
}
