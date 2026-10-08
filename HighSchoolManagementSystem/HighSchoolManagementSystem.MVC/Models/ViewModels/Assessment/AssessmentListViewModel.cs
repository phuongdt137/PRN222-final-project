using HighSchoolManagementSystem.Services.DTOs;

namespace HighSchoolManagementSystem.MVC.Models.ViewModels.Assessment;

public class AssessmentListViewModel
{
    public int? AssignmentId { get; set; }
    public string? SearchTerm { get; set; }

    public List<TeachingAssignmentSummaryDto> Assignments { get; set; } = new();
    public TeachingAssignmentSummaryDto? SelectedAssignment { get; set; }

    public List<AssessmentDto> Assessments { get; set; } = new();

    public int TotalAssessments => Assessments.Count;
    public int RegularAssessmentsCount => Assessments.Count(a => a.AssessmentTypeCode == "TX");
    public int MidtermAssessmentsCount => Assessments.Count(a => a.AssessmentTypeCode == "GK");
    public int FinalAssessmentsCount => Assessments.Count(a => a.AssessmentTypeCode == "CK");
}
