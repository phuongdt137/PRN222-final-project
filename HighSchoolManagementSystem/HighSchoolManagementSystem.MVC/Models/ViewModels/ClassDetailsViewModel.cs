namespace HighSchoolManagementSystem.MVC.Models.ViewModels;

public class ClassTeacherItemViewModel
{
    public string TeacherCode { get; set; } = null!;
    public string TeacherName { get; set; } = null!;
    public string SubjectCode { get; set; } = null!;
    public string SubjectName { get; set; } = null!;
    public string SemesterName { get; set; } = null!;
    public string Status { get; set; } = null!;
}

public class ClassStudentItemViewModel
{
    public int StudentId { get; set; }
    public string StudentCode { get; set; } = null!;
    public string StudentName { get; set; } = null!;
    public string? Gender { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime JoinedAt { get; set; }
    public string EnrollmentStatus { get; set; } = null!;
}

public class ClassDetailsViewModel
{
    public int Id { get; set; }
    public string ClassName { get; set; } = null!;
    public string AcademicYearCode { get; set; } = null!;
    public int AcademicYearId { get; set; }
    public string GradeLevelName { get; set; } = null!;
    public string GradeLevelCode { get; set; } = null!;
    public string? HomeroomTeacherName { get; set; }
    public string? HomeroomTeacherCode { get; set; }
    public int StudentCount { get; set; }
    public int TeacherAssignmentCount { get; set; }
    public IReadOnlyList<ClassTeacherItemViewModel> Teachers { get; set; } = Array.Empty<ClassTeacherItemViewModel>();
    public IReadOnlyList<ClassStudentItemViewModel> Students { get; set; } = Array.Empty<ClassStudentItemViewModel>();
}
