namespace HighSchoolManagementSystem.MVC.Models.ViewModels.AcademicYear;

public class AcademicYearListItemViewModel
{
    public int Id { get; set; }
    public string YearCode { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int SemesterCount { get; set; }
    public int ClassCount { get; set; }
    public bool IsCurrent { get; set; }
    public string Status { get; set; } = null!;
    public string StatusCssClass { get; set; } = null!;
}
