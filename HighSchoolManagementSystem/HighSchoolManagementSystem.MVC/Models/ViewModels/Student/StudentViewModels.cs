using System.ComponentModel.DataAnnotations;
using HighSchoolManagementSystem.Services.DTOs;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HighSchoolManagementSystem.MVC.Models.ViewModels.Student;

public class CreateStudentViewModel : CreateStudentDto
{
    [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu."), DataType(DataType.Password), Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    [Display(Name = "Xác nhận mật khẩu")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class StudentDetailsViewModel : UpdateStudentDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public bool IsAccountActive { get; set; }
    public int? ParentId { get; set; }
    public string? ParentName { get; set; }
    public string? ParentPhone { get; set; }
    public string? ParentEmail { get; set; }
    public int? CurrentClassId { get; set; }
    public int? CurrentMembershipId { get; set; }
    public int? CurrentGradeId { get; set; }
    public string? CurrentClassName { get; set; }
    public string? CurrentGrade { get; set; }
    public bool HasAssignment { get; set; }
    public string? CurrentYear { get; set; }
    public static string StatusText(string status) => status switch
    {
        "Active" => "Đang học", "Inactive" => "Ngừng học", "Graduated" => "Đã tốt nghiệp", _ => status
    };
}

public class StudentListViewModel
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public int? ClassId { get; set; }
    public bool UnassignedOnly { get; set; }
    public string? CurrentYear { get; set; }
    public List<StudentDetailsViewModel> Students { get; set; } = new();
    public List<SelectListItem> Classes { get; set; } = new();
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalCount { get; set; }
}

public class AssignStudentClassViewModel : AssignStudentClassDto
{
    public StudentDetailsViewModel Student { get; set; } = new();
    public string? CurrentYear { get; set; }
    public List<SelectListItem> Classes { get; set; } = new();
    public bool CanAssign => Student.Status == "Active" && !Student.HasAssignment && Classes.Count > 0;
}

public class TransferStudentClassViewModel : TransferStudentClassDto
{
    public StudentDetailsViewModel Student { get; set; } = new();
    public List<SelectListItem> Classes { get; set; } = new();
    public bool CanTransfer => Student.Status == "Active" && Student.CurrentMembershipId.HasValue && Classes.Count > 0;
}

public class StudentAcademicHistoryViewModel
{
    public StudentDetailsViewModel Student { get; set; } = new();
    public List<HighSchoolManagementSystem.Models.Entities.StudentClass> Memberships { get; set; } = new();
    public List<StudentSemesterHistoryViewModel> Semesters { get; set; } = new();
}

public class StudentSemesterHistoryViewModel
{
    public HighSchoolManagementSystem.Models.Entities.Semester Semester { get; set; } = null!;
    public HighSchoolManagementSystem.Models.Entities.SemesterReport? Report { get; set; }
    public HighSchoolManagementSystem.Models.Entities.ConductEvaluation? Conduct { get; set; }
    public List<HighSchoolManagementSystem.Models.Entities.SemesterSubjectResult> Subjects { get; set; } = new();
    public List<HighSchoolManagementSystem.Models.Entities.StudentScore> Scores { get; set; } = new();
}
