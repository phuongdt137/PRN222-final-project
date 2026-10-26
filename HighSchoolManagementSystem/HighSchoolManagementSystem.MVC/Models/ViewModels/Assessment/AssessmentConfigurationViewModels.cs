using System.ComponentModel.DataAnnotations;

namespace HighSchoolManagementSystem.MVC.Models.ViewModels.Assessment;

public class AssessmentConfigurationViewModel
{
    public IReadOnlyList<AssessmentConfigurationItemViewModel> Items { get; set; } = [];
    public decimal TotalWeight => Items.Sum(item => item.Weight);
}

public class AssessmentConfigurationItemViewModel
{
    public int Id { get; set; }
    public string TypeCode { get; set; } = "";
    public string TypeName { get; set; } = "";
    public decimal Weight { get; set; }
    public int MinEntries { get; set; }
    public int MaxEntries { get; set; }
    public int AssessmentCount { get; set; }
    public decimal MinScore { get; set; }
    public decimal MaxScore { get; set; }
}

public class AssessmentConfigurationEditViewModel
{
    public int? Id { get; set; }
    [Required, StringLength(20)] public string TypeCode { get; set; } = "";
    [Required, StringLength(50)] public string TypeName { get; set; } = "";
    [Range(typeof(decimal), "0.01", "100")] public decimal Weight { get; set; } = 1;
    [Range(0, 100)] public int MinEntries { get; set; } = 1;
    [Range(0, 100)] public int MaxEntries { get; set; } = 5;
    [Range(typeof(decimal), "0", "10")] public decimal MinScore { get; set; } = 0;
    [Range(typeof(decimal), "0", "10")] public decimal MaxScore { get; set; } = 10;
}
