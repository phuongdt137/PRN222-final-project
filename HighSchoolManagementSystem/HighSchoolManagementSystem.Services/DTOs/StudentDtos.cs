using System.ComponentModel.DataAnnotations;
using HighSchoolManagementSystem.Services.Common;

namespace HighSchoolManagementSystem.Services.DTOs;

public class UpdateStudentDto : IValidatableObject
{
    [Required(ErrorMessage = "Vui lòng nhập mã học sinh."), StringLength(20, ErrorMessage = "Mã học sinh không được vượt quá 20 ký tự.")]
    [RegularExpression(@"^[a-zA-Z0-9_-]+$", ErrorMessage = "Mã học sinh chỉ gồm chữ không dấu, số, gạch ngang và gạch dưới.")]
    [Display(Name = "Mã học sinh")]
    public string StudentCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập họ và tên."), StringLength(100, ErrorMessage = "Họ và tên không được vượt quá 100 ký tự."), Display(Name = "Họ và tên")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập email."), EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ."), StringLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự."), Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [StringLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự."), RegularExpression(@"^0[1-9][0-9]{8}$", ErrorMessage = "Số điện thoại phải gồm 10 chữ số, bắt đầu bằng 0.")]
    [Display(Name = "Số điện thoại")]
    public string? PhoneNumber { get; set; }

    [RegularExpression("^(Nam|Nữ|Khác)$", ErrorMessage = "Giới tính không hợp lệ."), Display(Name = "Giới tính")]
    public string? Gender { get; set; }

    [DataType(DataType.Date), Display(Name = "Ngày sinh")]
    [MinimumAge(ProfileAgeRules.StudentMinimumAge, nameof(AdmissionDate), ErrorMessage = ProfileAgeRules.StudentMessage)]
    public DateTime? DateOfBirth { get; set; }

    [DataType(DataType.Date), Display(Name = "Ngày nhập học")]
    public DateTime? AdmissionDate { get; set; }

    [StringLength(255, ErrorMessage = "Địa chỉ không được vượt quá 255 ký tự."), Display(Name = "Địa chỉ")]
    public string? Address { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn trạng thái học sinh."), RegularExpression("^(Active|Inactive|Graduated)$", ErrorMessage = "Trạng thái học sinh không hợp lệ.")]
    [Display(Name = "Trạng thái học sinh")]
    public string Status { get; set; } = "Active";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DateOfBirth.HasValue && (DateOfBirth.Value.Year < 1920 || DateOfBirth.Value.Date >= SchoolDate.Today))
            yield return new ValidationResult("Ngày sinh phải từ năm 1920 và trước ngày hiện tại.", new[] { nameof(DateOfBirth) });
        if (AdmissionDate.HasValue && (AdmissionDate.Value.Year < 1920 || AdmissionDate.Value.Date > SchoolDate.Today))
            yield return new ValidationResult("Ngày nhập học phải từ năm 1920 và không sau ngày hiện tại.", new[] { nameof(AdmissionDate) });
        if (DateOfBirth.HasValue && AdmissionDate.HasValue && AdmissionDate.Value.Date <= DateOfBirth.Value.Date)
            yield return new ValidationResult("Ngày nhập học phải sau ngày sinh.", new[] { nameof(AdmissionDate) });
        if (FullName?.Any(char.IsControl) == true)
            yield return new ValidationResult("Họ và tên không được chứa ký tự điều khiển.", new[] { nameof(FullName) });
        if (this is CreateStudentDto create && System.Text.Encoding.UTF8.GetByteCount(create.Password ?? "") > 72)
            yield return new ValidationResult("Mật khẩu không được vượt quá 72 byte UTF-8.", new[] { nameof(CreateStudentDto.Password) });
    }
}

public class CreateStudentDto : UpdateStudentDto
{
    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập."), StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập phải có từ 3 đến 50 ký tự.")]
    [RegularExpression(@"^[a-zA-Z0-9_.]+$", ErrorMessage = "Tên đăng nhập chỉ gồm chữ không dấu, số, dấu chấm và gạch dưới.")]
    [Display(Name = "Tên đăng nhập")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu khởi tạo."), StringLength(64, MinimumLength = 6, ErrorMessage = "Mật khẩu khởi tạo phải có từ 6 đến 64 ký tự."), DataType(DataType.Password), Display(Name = "Mật khẩu khởi tạo")]
    public string Password { get; set; } = string.Empty;
}

public class AssignStudentClassDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn lớp học."), Display(Name = "Lớp học")]
    public int ClassId { get; set; }
}

public class TransferStudentClassDto : AssignStudentClassDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Phân lớp hiện tại không hợp lệ. Vui lòng tải lại trang.")]
    public int MembershipId { get; set; }
}
