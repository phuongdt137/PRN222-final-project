namespace HighSchoolManagementSystem.Services.DTOs;

public class AssessmentDto
{
    public int Id { get; set; }
    public int TeachingAssignmentId { get; set; }
    public int AssessmentTypeId { get; set; }
    public string AssessmentTypeCode { get; set; } = string.Empty;
    public string AssessmentTypeName { get; set; } = string.Empty;
    public decimal Weight { get; set; }
    public int MinEntries { get; set; }
    public int MaxEntries { get; set; }
    public string AssessmentName { get; set; } = string.Empty;
    public DateTime? ConductedAt { get; set; }

    // Thông tin phân công giảng dạy liên quan
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public int SubjectId { get; set; }
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public int TeacherId { get; set; }
    public string TeacherCode { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
    public int SemesterId { get; set; }
    public string SemesterName { get; set; } = string.Empty;
    public string AcademicYearCode { get; set; } = string.Empty;

    // Thống kê điểm số
    public int TotalStudents { get; set; }
    public int GradedStudentsCount { get; set; }
    public int LockedScoresCount { get; set; }
    public bool HasScores => GradedStudentsCount > 0;
    public bool HasLockedScores => LockedScoresCount > 0;
}

public class AssessmentTypeDto
{
    public int Id { get; set; }
    public string TypeCode { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public decimal Weight { get; set; }
    public int MinEntries { get; set; }
    public int MaxEntries { get; set; }
}

public class CreateAssessmentDto
{
    public int TeachingAssignmentId { get; set; }
    public int AssessmentTypeId { get; set; }
    public string AssessmentName { get; set; } = string.Empty;
    public DateTime? ConductedAt { get; set; }
}

public class UpdateAssessmentDto
{
    public int Id { get; set; }
    public int AssessmentTypeId { get; set; }
    public string AssessmentName { get; set; } = string.Empty;
    public DateTime? ConductedAt { get; set; }
}

public class TeachingAssignmentSummaryDto
{
    public int Id { get; set; }
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public int SubjectId { get; set; }
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public int SemesterId { get; set; }
    public string SemesterName { get; set; } = string.Empty;
    public string AcademicYearCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int TotalStudents { get; set; }
    public int AssessmentCount { get; set; }
}
