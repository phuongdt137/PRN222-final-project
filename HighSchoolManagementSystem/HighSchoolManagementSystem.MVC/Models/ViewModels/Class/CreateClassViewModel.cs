using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HighSchoolManagementSystem.MVC.Models.ViewModels.Class;

public class CreateClassViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tên lớp.")]
    [StringLength(50, ErrorMessage = "Tên lớp không được vượt quá 50 ký tự.")]
    [Display(Name = "Tên lớp")]
    public string ClassName { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn năm học.")]
    [Display(Name = "Năm học")]
    public int AcademicYearId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn khối.")]
    [Display(Name = "Khối")]
    public int GradeLevelId { get; set; }

    [Display(Name = "Giáo viên chủ nhiệm")]
    public int? HomeroomTeacherId { get; set; }

    public IEnumerable<SelectListItem> AcademicYears { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> GradeLevels { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> Teachers { get; set; } = Enumerable.Empty<SelectListItem>();
}
