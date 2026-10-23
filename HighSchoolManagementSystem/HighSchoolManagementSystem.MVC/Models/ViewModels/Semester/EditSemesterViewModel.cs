using System.ComponentModel.DataAnnotations;

namespace HighSchoolManagementSystem.MVC.Models.ViewModels.Semester;

public class EditSemesterViewModel
{
    public int Id { get; set; }

    public int AcademicYearId { get; set; }

    public string AcademicYearCode { get; set; } = string.Empty;

    public DateTime AcademicYearStartDate { get; set; }

    public DateTime AcademicYearEndDate { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mã học kỳ.")]
    [StringLength(20, ErrorMessage = "Mã học kỳ không được vượt quá 20 ký tự.")]
    [Display(Name = "Mã học kỳ")]
    public string SemesterCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập tên học kỳ.")]
    [StringLength(50, ErrorMessage = "Tên học kỳ không được vượt quá 50 ký tự.")]
    [Display(Name = "Tên học kỳ")]
    public string SemesterName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn ngày bắt đầu.")]
    [DataType(DataType.Date)]
    [Display(Name = "Ngày bắt đầu")]
    public DateTime StartDate { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn ngày kết thúc.")]
    [DataType(DataType.Date)]
    [Display(Name = "Ngày kết thúc")]
    public DateTime EndDate { get; set; }
}
