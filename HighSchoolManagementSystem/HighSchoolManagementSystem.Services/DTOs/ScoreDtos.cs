namespace HighSchoolManagementSystem.Services.DTOs;

public class StudentScoreItemDto
{
    public int StudentId { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int AssessmentId { get; set; }
    public int? ScoreId { get; set; }
    public decimal? Score { get; set; }
    public string? Comment { get; set; }
    public bool IsLocked { get; set; }
    public bool HasAuditLogs { get; set; }
}

public class SaveStudentScoreDto
{
    public int StudentId { get; set; }
    public int AssessmentId { get; set; }
    public decimal? Score { get; set; }
    public string? Comment { get; set; }
    public string? Reason { get; set; }
}

public class BulkSaveScoreDto
{
    public int AssessmentId { get; set; }
    public List<SaveStudentScoreDto> Scores { get; set; } = new();
    public bool LockAfterSave { get; set; }
    public string? GlobalReason { get; set; }
    public bool IsDraft { get; set; }
    public bool IsStaffOverride { get; set; }
}

public class ScoreAuditLogDto
{
    public int Id { get; set; }
    public int StudentScoreId { get; set; }
    public decimal? OldScore { get; set; }
    public decimal? NewScore { get; set; }
    public string ModifiedByName { get; set; } = string.Empty;
    public DateTime ModifiedAt { get; set; }
    public string? Reason { get; set; }
}

public class AssessmentHeaderDto
{
    public int Id { get; set; }
    public string AssessmentName { get; set; } = string.Empty;
    public string TypeCode { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public decimal Weight { get; set; }
}

public class StudentSubjectRowDto
{
    public int StudentId { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    // Map AssessmentId -> Score
    public Dictionary<int, decimal?> AssessmentScores { get; set; } = new();
    // Map AssessmentId -> ScoreId
    public Dictionary<int, int?> AssessmentScoreIds { get; set; } = new();
    // Map AssessmentId -> IsLocked
    public Dictionary<int, bool> AssessmentIsLocked { get; set; } = new();
    public decimal? AverageScore { get; set; }
    public bool IsPassed { get; set; } = true;
    public string? Remarks { get; set; }
}

public class SubjectScoreMatrixDto
{
    public int AssignmentId { get; set; }
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public int SubjectId { get; set; }
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public int SemesterId { get; set; }
    public string SemesterName { get; set; } = string.Empty;
    public string AcademicYearCode { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
    public string EvaluationType { get; set; } = "Score";
    public List<AssessmentHeaderDto> Assessments { get; set; } = new();
    public List<StudentSubjectRowDto> Students { get; set; } = new();
}

public class SubjectHeaderDto
{
    public int Id { get; set; }
    public int AssignmentId { get; set; }
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public string EvaluationType { get; set; } = "Score";
}

public class HomeroomStudentRowDto
{
    public int StudentId { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    // Map SubjectId -> AverageScore
    public Dictionary<int, decimal?> SubjectAverages { get; set; } = new();
    public decimal? Gpa { get; set; }
    public string? AcademicRank { get; set; }
    public string? ConductGrade { get; set; }
    public int? ClassRank { get; set; }
    public string? Remarks { get; set; }
}

public class HomeroomScoreReportDto
{
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public int SemesterId { get; set; }
    public string SemesterName { get; set; } = string.Empty;
    public string AcademicYearCode { get; set; } = string.Empty;
    public string HomeroomTeacherName { get; set; } = string.Empty;
    public List<SubjectHeaderDto> Subjects { get; set; } = new();
    public List<HomeroomStudentRowDto> Students { get; set; } = new();
}

public class AssessmentScoreDto
{
    public int AssessmentId { get; set; }
    public string AssessmentName { get; set; } = string.Empty;
    public string TypeCode { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public decimal Weight { get; set; }
    public decimal? Score { get; set; }
    public string? Comment { get; set; }
    public bool IsLocked { get; set; }
    public DateTime? ConductedAt { get; set; }
}

public class StudentSubjectDetailDto
{
    public int SubjectId { get; set; }
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
    public string EvaluationType { get; set; } = "Score";
    public List<AssessmentScoreDto> Assessments { get; set; } = new();
    public decimal? AverageScore { get; set; }
    public bool IsPassed { get; set; } = true;
    public string? Remarks { get; set; }
}

public class StudentTranscriptDto
{
    public int StudentId { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public int SemesterId { get; set; }
    public string SemesterName { get; set; } = string.Empty;
    public string AcademicYearCode { get; set; } = string.Empty;
    public decimal? Gpa { get; set; }
    public string? AcademicRank { get; set; }
    public string? ConductGrade { get; set; }
    public int? ClassRank { get; set; }
    public string? TeacherComments { get; set; }
    public List<StudentSubjectDetailDto> Subjects { get; set; } = new();
    public List<TeacherFeedbackDto> Feedbacks { get; set; } = new();
}

public class TeacherFeedbackDto
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public int TeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public int SemesterId { get; set; }
    public string SemesterName { get; set; } = string.Empty;
    public string FeedbackType { get; set; } = "Academic"; // Academic, Behavior, General
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateTeacherFeedbackDto
{
    public int StudentId { get; set; }
    public int TeacherId { get; set; }
    public int SemesterId { get; set; }
    public string FeedbackType { get; set; } = "Academic";
    public string Content { get; set; } = string.Empty;
}

public class UpdateTeacherFeedbackDto
{
    public int Id { get; set; }
    public string FeedbackType { get; set; } = "Academic";
    public string Content { get; set; } = string.Empty;
}
