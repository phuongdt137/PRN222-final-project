namespace HighSchoolManagementSystem.Services.DTOs;

public class CreateSubjectDto
{
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public string EvaluationType { get; set; } = "Score";
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateSubjectDto
{
    public int Id { get; set; }
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public string EvaluationType { get; set; } = "Score";
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class AssignTeacherDto
{
    public int SubjectId { get; set; }
    public int TeacherId { get; set; }
    public int ClassId { get; set; }
    public int SemesterId { get; set; }
    public string Status { get; set; } = "Active";
}
