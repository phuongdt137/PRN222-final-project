namespace HighSchoolManagementSystem.MVC.Models.ViewModels.Class;

public class TeacherAccessibleClassItemViewModel
{
    public int Id { get; set; }
    public string ClassName { get; set; } = null!;
    public string AcademicYearCode { get; set; } = null!;
    public string GradeLevelName { get; set; } = null!;
    public int StudentCount { get; set; }
    public bool IsHomeroom { get; set; }
    public bool IsTeaching { get; set; }
    public string AccessRoleLabel { get; set; } = null!;
}

public class ClassStudentsPageViewModel
{
    public int ClassId { get; set; }
    public string ClassName { get; set; } = null!;
    public string AcademicYearCode { get; set; } = null!;
    public int AcademicYearId { get; set; }
    public string GradeLevelName { get; set; } = null!;
    public string? HomeroomTeacherName { get; set; }
    public string AccessContext { get; set; } = "Staff";
    public IReadOnlyList<ClassStudentItemViewModel> Students { get; set; } = Array.Empty<ClassStudentItemViewModel>();
}
