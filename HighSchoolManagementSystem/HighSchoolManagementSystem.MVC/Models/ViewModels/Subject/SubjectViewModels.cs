using System.ComponentModel.DataAnnotations;

namespace HighSchoolManagementSystem.MVC.Models.ViewModels.Subject;

public class SubjectListItemViewModel
{
    public int Id { get; set; }
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public string EvaluationType { get; set; } = "Score";
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int TotalAssignments { get; set; }
}

public class SubjectListViewModel
{
    public string? SearchTerm { get; set; }
    public string? EvaluationTypeFilter { get; set; }
    public string? StatusFilter { get; set; }

    public List<SubjectListItemViewModel> Subjects { get; set; } = new();

    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }
    public int InactiveCount { get; set; }
    public int ScoreCount { get; set; }
    public int PassFailCount { get; set; }
}

public class SubjectAssignmentItemViewModel
{
    public int AssignmentId { get; set; }
    public int TeacherId { get; set; }
    public string TeacherCode { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string SemesterName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class SubjectDetailViewModel
{
    public int Id { get; set; }
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public string EvaluationType { get; set; } = "Score";
    public string? Description { get; set; }
    public bool IsActive { get; set; }

    public List<SubjectAssignmentItemViewModel> TeachingAssignments { get; set; } = new();
    public List<TeacherSelectItemViewModel> AvailableTeachers { get; set; } = new();
    public List<ClassSelectItemViewModel> AvailableClasses { get; set; } = new();
    public List<SemesterSelectItemViewModel> AvailableSemesters { get; set; } = new();
    public AssignTeacherViewModel AssignForm { get; set; } = new();
}

public class TeacherSelectItemViewModel
{
    public int Id { get; set; }
    public string TeacherCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Specialization { get; set; }
    public bool IsMatchingSpecialization { get; set; }
}

public class ClassSelectItemViewModel
{
    public int Id { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string AcademicYear { get; set; } = string.Empty;
}

public class SemesterSelectItemViewModel
{
    public int Id { get; set; }
    public string SemesterName { get; set; } = string.Empty;
    public string AcademicYear { get; set; } = string.Empty;
}

public class AssignTeacherViewModel
{
    [Required]
    public int SubjectId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn giáo viên giảng dạy.")]
    [Display(Name = "Giáo viên giảng dạy")]
    public int TeacherId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn lớp học.")]
    [Display(Name = "Lớp học")]
    public int ClassId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn học kỳ.")]
    [Display(Name = "Học kỳ")]
    public int SemesterId { get; set; }

    [Display(Name = "Trạng thái")]
    public string Status { get; set; } = "Active";
}

public class CreateSubjectViewModel
{
    [Required(ErrorMessage = "Mã môn học không được để trống.")]
    [StringLength(20, ErrorMessage = "Mã môn học tối đa 20 ký tự.")]
    [RegularExpression(@"^[a-zA-Z0-9_-]+$", ErrorMessage = "Mã môn học chỉ được chứa chữ cái, chữ số và dấu gạch ngang.")]
    [Display(Name = "Mã môn học")]
    public string SubjectCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên môn học không được để trống.")]
    [StringLength(100, ErrorMessage = "Tên môn học tối đa 100 ký tự.")]
    [Display(Name = "Tên môn học")]
    public string SubjectName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn hình thức đánh giá.")]
    [Display(Name = "Hình thức đánh giá")]
    public string EvaluationType { get; set; } = "Score";

    [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự.")]
    [Display(Name = "Mô tả môn học")]
    public string? Description { get; set; }

    [Display(Name = "Trạng thái hoạt động")]
    public bool IsActive { get; set; } = true;
}

public class EditSubjectViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Mã môn học không được để trống.")]
    [StringLength(20, ErrorMessage = "Mã môn học tối đa 20 ký tự.")]
    [RegularExpression(@"^[a-zA-Z0-9_-]+$", ErrorMessage = "Mã môn học chỉ được chứa chữ cái, chữ số và dấu gạch ngang.")]
    [Display(Name = "Mã môn học")]
    public string SubjectCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên môn học không được để trống.")]
    [StringLength(100, ErrorMessage = "Tên môn học tối đa 100 ký tự.")]
    [Display(Name = "Tên môn học")]
    public string SubjectName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn hình thức đánh giá.")]
    [Display(Name = "Hình thức đánh giá")]
    public string EvaluationType { get; set; } = "Score";

    [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự.")]
    [Display(Name = "Mô tả môn học")]
    public string? Description { get; set; }

    [Display(Name = "Trạng thái hoạt động")]
    public bool IsActive { get; set; } = true;

    public bool HasScores { get; set; }
}
