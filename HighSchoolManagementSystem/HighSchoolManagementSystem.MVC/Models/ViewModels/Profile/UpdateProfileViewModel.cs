using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace HighSchoolManagementSystem.MVC.Models.ViewModels.Profile;

public class UpdateProfileViewModel
{
    // =========================================================================
    // CÁC TRƯỜNG CHO PHÉP NGƯỜI DÙNG CẬP NHẬT
    // =========================================================================

    [RegularExpression(@"^0[1-9]\d{8}$", ErrorMessage = "Số điện thoại phải gồm 10 chữ số và bắt đầu bằng số 0 (ví dụ: 0912345678).")]
    [MaxLength(15, ErrorMessage = "Số điện thoại không được vượt quá 15 ký tự.")]
    [Display(Name = "Số điện thoại cá nhân")]
    public string? PhoneNumber { get; set; }

    [MaxLength(10, ErrorMessage = "Giới tính không hợp lệ.")]
    [Display(Name = "Giới tính")]
    public string? Gender { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Ngày sinh")]
    public DateTime? DateOfBirth { get; set; }

    [Display(Name = "Tải lên ảnh đại diện mới")]
    public IFormFile? AvatarFile { get; set; }

    // Đường dẫn ảnh đại diện hiện tại
    public string? AvatarUrl { get; set; }

    // =========================================================================
    // CÁC TRƯỜNG CHỈ XEM (READ-ONLY) - KHÔNG CHO PHÉP TỰ SỬA
    // =========================================================================

    [Display(Name = "Tên đăng nhập")]
    public string Username { get; set; } = string.Empty;

    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Địa chỉ Email")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Vai trò trong hệ thống")]
    public string RoleName { get; set; } = string.Empty;

    public string? RoleDescription { get; set; }
}
