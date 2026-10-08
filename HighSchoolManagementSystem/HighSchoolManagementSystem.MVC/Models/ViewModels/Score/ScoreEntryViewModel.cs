using HighSchoolManagementSystem.Services.DTOs;

namespace HighSchoolManagementSystem.MVC.Models.ViewModels.Score;

public class ScoreEntryViewModel
{
    public int AssessmentId { get; set; }
    public string AssessmentName { get; set; } = string.Empty;
    public string AssessmentTypeName { get; set; } = string.Empty;
    public decimal Weight { get; set; }
    public decimal MaxScore { get; set; } = 10;
    public string SubjectName { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string SemesterName { get; set; } = string.Empty;
    public int AssignmentId { get; set; }
    public bool IsAssessmentLocked { get; set; }
    public bool IsStaff { get; set; }
    public bool CanEdit { get; set; }
    public DateTime? ConductedAt { get; set; }
    
    public List<StudentScoreItemRowViewModel> Students { get; set; } = new();

    public int TotalStudents => Students.Count;
    public int GradedCount => Students.Count(s => s.Score.HasValue);
    public int CommentedCount => Students.Count(s => !string.IsNullOrWhiteSpace(s.Comment));
    public decimal? AverageScore => GradedCount > 0 
        ? Math.Round(Students.Where(s => s.Score.HasValue).Average(s => s.Score!.Value), 2) 
        : null;
}

public class StudentScoreItemRowViewModel
{
    public int StudentId { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int? ScoreId { get; set; }
    public decimal? Score { get; set; }
    public string? Comment { get; set; }
    public bool IsLocked { get; set; }
    public bool HasExistingScore => Score.HasValue || ScoreId.HasValue;
}

public class BulkScoreFormModel
{
    public int AssessmentId { get; set; }
    public string? Reason { get; set; }
    public bool LockAfterSave { get; set; }
    public bool IsDraft { get; set; }
    public bool IsStaffOverride { get; set; }
    public List<StudentScoreInputItem> Scores { get; set; } = new();
}

public class StudentScoreInputItem
{
    public int StudentId { get; set; }
    public string? Score { get; set; }
    public string? Comment { get; set; }

    public decimal? GetDecimalScore()
    {
        if (!TryGetDecimalScore(out var result))
            throw new FormatException("Điểm không đúng định dạng số.");
        return result;
    }

    public bool TryGetDecimalScore(out decimal? score)
    {
        score = null;
        if (string.IsNullOrWhiteSpace(Score)) return true;
        var clean = Score.Trim().Replace(',', '.');
        if (!decimal.TryParse(clean, System.Globalization.NumberStyles.AllowLeadingSign | System.Globalization.NumberStyles.AllowDecimalPoint,
            System.Globalization.CultureInfo.InvariantCulture, out var result)) return false;
        score = result;
        return true;
    }
}

public class SingleScoreFormModel : StudentScoreInputItem
{
    public int AssessmentId { get; set; }
    public string? Reason { get; set; }
}
