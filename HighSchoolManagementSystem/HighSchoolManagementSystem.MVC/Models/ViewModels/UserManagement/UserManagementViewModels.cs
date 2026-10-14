using System.ComponentModel.DataAnnotations;

namespace HighSchoolManagementSystem.MVC.Models.ViewModels.UserManagement;

public class UserListViewModel
{
    public string? Search { get; set; }
    public int? RoleId { get; set; }
    public string? Status { get; set; }
    public List<RoleOptionViewModel> Roles { get; set; } = new();
    public List<UserListItemViewModel> Users { get; set; } = new();
    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }
    public int InactiveCount { get; set; }
}

public class RoleOptionViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class UserListItemViewModel
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreateAt { get; set; }
}

public class UserDetailsViewModel : UserListItemViewModel
{
    public string? PhoneNumber { get; set; }
    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? ProfileCode { get; set; }
    public string? ProfileDetails { get; set; }
}

public class CreateUserViewModel
{
    [Required, StringLength(50, MinimumLength = 3)]
    [Display(Name = "Tên đăng nhập")]
    public string Username { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(100)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100)]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 6)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;

    [StringLength(20)]
    [Display(Name = "Số điện thoại")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Giới tính")]
    public string? Gender { get; set; }

    [Display(Name = "Ngày sinh")]
    public DateTime? DateOfBirth { get; set; }

    [Required, Display(Name = "Vai trò")]
    public int RoleId { get; set; }

    [Display(Name = "Kích hoạt tài khoản")]
    public bool IsActive { get; set; } = true;

    // Student profile fields
    [Display(Name = "Ngày nhập học")]
    public DateTime? AdmissionDate { get; set; }

    [Display(Name = "Địa chỉ")]
    [StringLength(255)]
    public string? Address { get; set; }

    // Teacher profile fields
    [Display(Name = "Tổ bộ môn / Chuyên môn")]
    [StringLength(100)]
    public string? Specialization { get; set; }

    [Display(Name = "Trình độ")]
    [StringLength(100)]
    public string? Qualification { get; set; }

    [Display(Name = "Số tiết tối đa / tuần")]
    [Range(1, 40, ErrorMessage = "Số tiết tối đa từ 1 đến 40.")]
    public int? MaxPeriodsPerWeek { get; set; } = 20;

    [Display(Name = "Ngày tuyển dụng")]
    public DateTime? HireDate { get; set; }

    // Parent profile fields
    [Display(Name = "Nghề nghiệp")]
    [StringLength(100)]
    public string? Occupation { get; set; }

    [Display(Name = "Số liên hệ khẩn cấp")]
    [StringLength(50)]
    public string? EmergencyContact { get; set; }
}

public class EditUserViewModel
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(100)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100)]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = string.Empty;

    [StringLength(20)]
    [Display(Name = "Số điện thoại")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Giới tính")]
    public string? Gender { get; set; }

    [Display(Name = "Ngày sinh")]
    public DateTime? DateOfBirth { get; set; }

    [Required, Display(Name = "Vai trò")]
    public int RoleId { get; set; }
}

public class ResetUserPasswordViewModel
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 6)]
    [Display(Name = "Mật khẩu mới")]
    public string NewPassword { get; set; } = string.Empty;

    [Required, Compare(nameof(NewPassword))]
    [Display(Name = "Xác nhận mật khẩu")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
