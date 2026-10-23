using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HighSchoolManagementSystem.MVC.Models.ViewModels.Class;

public class AssignHomeroomTeacherViewModel
{
    public int ClassId { get; set; }

    public string ClassName { get; set; } = string.Empty;

    public string AcademicYearCode { get; set; } = string.Empty;

    public string GradeLevelName { get; set; } = string.Empty;

    public string? CurrentHomeroomTeacherName { get; set; }

    public string? CurrentHomeroomTeacherCode { get; set; }

    [Display(Name = "Giáo viên chủ nhiệm")]
    public int? HomeroomTeacherId { get; set; }

    public IEnumerable<SelectListItem> Teachers { get; set; } = Enumerable.Empty<SelectListItem>();
}
