using System.ComponentModel.DataAnnotations;

namespace HighSchoolManagementSystem.MVC.Models.ViewModels.Auth;

public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập địa chỉ email.")]
    [EmailAddress(ErrorMessage = "Địa chỉ email không đúng định dạng.")]
    [Display(Name = "Địa chỉ Email đã đăng ký")]
    public string Email { get; set; } = string.Empty;
}
