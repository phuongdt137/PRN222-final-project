using System.ComponentModel.DataAnnotations;
using HighSchoolManagementSystem.Services.Common;

namespace HighSchoolManagementSystem.Services.DTOs;

public class UpdateParentDto : IValidatableObject
{
    [Required(ErrorMessage = "Vui lòng nhập họ và tên."), StringLength(100, ErrorMessage = "Họ và tên không được vượt quá 100 ký tự.")]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập email."), EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ."), StringLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [StringLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự."), RegularExpression(@"^0[1-9]\d{8}$", ErrorMessage = "Số điện thoại phải gồm 10 chữ số, bắt đầu bằng 0.")]
    [Display(Name = "Số điện thoại")]
    public string? PhoneNumber { get; set; }

    [RegularExpression("^(Nam|Nữ|Khác)$", ErrorMessage = "Giới tính không hợp lệ.")]
    [Display(Name = "Giới tính")]
    public string? Gender { get; set; }

    [DataType(DataType.Date), Display(Name = "Ngày sinh")]
    [MinimumAge(ProfileAgeRules.ParentMinimumAge, ErrorMessage = ProfileAgeRules.ParentMessage)]
    public DateTime? DateOfBirth { get; set; }

    [StringLength(100, ErrorMessage = "Nghề nghiệp không được vượt quá 100 ký tự."), Display(Name = "Nghề nghiệp")]
    public string? Occupation { get; set; }

    [StringLength(50, ErrorMessage = "Liên hệ khẩn cấp không được vượt quá 50 ký tự."), Display(Name = "Liên hệ khẩn cấp")]
    public string? EmergencyContact { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DateOfBirth.HasValue && (DateOfBirth.Value.Year < 1920 || DateOfBirth.Value.Date >= SchoolDate.Today))
            yield return new ValidationResult("Ngày sinh phải từ năm 1920 và trước ngày hiện tại.", new[] { nameof(DateOfBirth) });
    }
}

public class CreateParentDto : UpdateParentDto
{
    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập."), StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập phải có từ 3 đến 50 ký tự.")]
    [RegularExpression(@"^[a-zA-Z0-9_.]+$", ErrorMessage = "Tên đăng nhập chỉ gồm chữ không dấu, số, dấu chấm và gạch dưới.")]
    [Display(Name = "Tên đăng nhập")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu khởi tạo."), StringLength(64, MinimumLength = 6, ErrorMessage = "Mật khẩu khởi tạo phải có từ 6 đến 64 ký tự."), DataType(DataType.Password)]
    [Display(Name = "Mật khẩu khởi tạo")]
    public string Password { get; set; } = string.Empty;
}
