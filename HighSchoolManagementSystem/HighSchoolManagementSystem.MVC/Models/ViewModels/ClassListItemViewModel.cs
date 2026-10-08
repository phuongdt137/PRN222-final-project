using Microsoft.AspNetCore.Mvc.Rendering;

namespace HighSchoolManagementSystem.MVC.Models.ViewModels;

public class ClassListItemViewModel
{
    public int Id { get; set; }
    public string ClassName { get; set; } = null!;
    public string AcademicYearCode { get; set; } = null!;
    public string GradeLevelName { get; set; } = null!;
    public string GradeLevelCode { get; set; } = null!;
    public string? HomeroomTeacherName { get; set; }
    public string? HomeroomTeacherCode { get; set; }
    public int StudentCount { get; set; }
}

public class ClassListPageViewModel
{
    public int? SelectedAcademicYearId { get; set; }
    public int? SelectedGradeLevelId { get; set; }
    public IEnumerable<SelectListItem> AcademicYears { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> GradeLevels { get; set; } = Enumerable.Empty<SelectListItem>();
    public IReadOnlyList<ClassListItemViewModel> Classes { get; set; } = Array.Empty<ClassListItemViewModel>();
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Max(1, Math.Ceiling((double)TotalCount / PageSize)) : 1;
}
