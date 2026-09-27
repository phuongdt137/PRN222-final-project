using System.ComponentModel.DataAnnotations;

namespace HighSchoolManagementSystem.MVC.Models.ViewModels.Teacher;

public class TeacherListItemViewModel
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string TeacherCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Specialization { get; set; }
    public string? Qualification { get; set; }
    public string Status { get; set; } = "Active";
    public int? MaxPeriodsPerWeek { get; set; }
    public int TotalAssignments { get; set; }
    public string? HomeroomClassName { get; set; }
}

public class TeacherListViewModel
{
    public string? SearchTerm { get; set; }
    public string? SpecializationFilter { get; set; }
    public string? StatusFilter { get; set; }
    public List<string> SpecializationList { get; set; } = new();
    public List<TeacherListItemViewModel> Teachers { get; set; } = new();
    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }
    public int InactiveCount { get; set; }
}

public class TeacherHomeroomItemViewModel
{
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string AcademicYearName { get; set; } = string.Empty;
}

public class TeacherAssignmentItemViewModel
{
    public int AssignmentId { get; set; }
    public int SubjectId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public string SemesterName { get; set; } = string.Empty;
    public string AcademicYearName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? TimetableSchedule { get; set; }
}

public class TeacherSubjectItemViewModel
{
    public int SubjectId { get; set; }
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public string EvaluationType { get; set; } = "Score";
    public bool IsActive { get; set; }
}

public class TeacherDetailViewModel
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string TeacherCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Specialization { get; set; }
    public string? Qualification { get; set; }
    public DateTime? HireDate { get; set; }
    public int? MaxPeriodsPerWeek { get; set; }
    public string Status { get; set; } = "Active";
    public string? AvatarUrl { get; set; }
    public List<TeacherHomeroomItemViewModel> HomeroomClasses { get; set; } = new();
    public List<TeacherAssignmentItemViewModel> TeachingAssignments { get; set; } = new();
    public List<TeacherSubjectItemViewModel> SpecializedSubjects { get; set; } = new();
}

public class CreateTeacherViewModel
{
    [Required(ErrorMessage = "Mã giáo viên không được để trống.")]
    [StringLength(20, ErrorMessage = "Mã giáo viên tối đa 20 ký tự.")]
    [RegularExpression(@"^[a-zA-Z0-9_-]+$", ErrorMessage = "Mã giáo viên chỉ được chứa chữ cái, chữ số và dấu gạch.")]
    [Display(Name = "Mã giáo viên")]
    public string TeacherCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Họ và tên không được để trống.")]
    [StringLength(100, ErrorMessage = "Họ và tên tối đa 100 ký tự.")]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên đăng nhập không được để trống.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập từ 3 đến 50 ký tự.")]
    [RegularExpression(@"^[a-zA-Z0-9._-]+$", ErrorMessage = "Tên đăng nhập không hợp lệ.")]
    [Display(Name = "Tên đăng nhập")]
    public string Username { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự.")]
    [Display(Name = "Mật khẩu khởi tạo")]
    public string? Password { get; set; }

    [Required(ErrorMessage = "Email không được để trống.")]
    [EmailAddress(ErrorMessage = "Địa chỉ email không đúng định dạng.")]
    [StringLength(100, ErrorMessage = "Email tối đa 100 ký tự.")]
    [Display(Name = "Địa chỉ Email")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
    [StringLength(20, ErrorMessage = "Số điện thoại tối đa 20 ký tự.")]
    [Display(Name = "Số điện thoại")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Giới tính")]
    public string? Gender { get; set; } = "Nam";

    [DataType(DataType.Date)]
    [Display(Name = "Ngày sinh")]
    public DateTime? DateOfBirth { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn tổ bộ môn chuyên môn.")]
    [StringLength(100, ErrorMessage = "Tổ bộ môn tối đa 100 ký tự.")]
    [Display(Name = "Tổ chuyên môn")]
    public string? Specialization { get; set; }

    [StringLength(100, ErrorMessage = "Trình độ bằng cấp tối đa 100 ký tự.")]
    [Display(Name = "Trình độ / Bằng cấp")]
    public string? Qualification { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Ngày tuyển dụng")]
    public DateTime? HireDate { get; set; }

    [Range(1, 40, ErrorMessage = "Số tiết tối đa trong tuần từ 1 đến 40 tiết.")]
    [Display(Name = "Số tiết tối đa / tuần")]
    public int? MaxPeriodsPerWeek { get; set; } = 20;

    [Display(Name = "Trạng thái")]
    public string Status { get; set; } = "Active";
}

public class EditTeacherViewModel
{
    public int Id { get; set; }
    public int UserId { get; set; }

    [Display(Name = "Mã giáo viên")]
    public string TeacherCode { get; set; } = string.Empty;

    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Địa chỉ Email")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Số điện thoại")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Giới tính")]
    public string? Gender { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Ngày sinh")]
    public DateTime? DateOfBirth { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn tổ bộ môn chuyên môn.")]
    [StringLength(100, ErrorMessage = "Tổ bộ môn tối đa 100 ký tự.")]
    [Display(Name = "Tổ chuyên môn")]
    public string? Specialization { get; set; }

    [StringLength(100, ErrorMessage = "Trình độ bằng cấp tối đa 100 ký tự.")]
    [Display(Name = "Trình độ / Bằng cấp")]
    public string? Qualification { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Ngày tuyển dụng")]
    public DateTime? HireDate { get; set; }

    [Range(1, 40, ErrorMessage = "Số tiết tối đa trong tuần từ 1 đến 40 tiết.")]
    [Display(Name = "Số tiết tối đa / tuần")]
    public int? MaxPeriodsPerWeek { get; set; }

    [Display(Name = "Trạng thái hoạt động")]
    public string Status { get; set; } = "Active";
}
