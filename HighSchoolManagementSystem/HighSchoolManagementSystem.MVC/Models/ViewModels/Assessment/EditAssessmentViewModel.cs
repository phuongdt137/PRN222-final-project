using System.ComponentModel.DataAnnotations;
using HighSchoolManagementSystem.Services.DTOs;

namespace HighSchoolManagementSystem.MVC.Models.ViewModels.Assessment;

public class EditAssessmentViewModel
{
    public int Id { get; set; }

    public int TeachingAssignmentId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn loại bài kiểm tra")]
    [Display(Name = "Loại kiểm tra / Đánh giá")]
    public int AssessmentTypeId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên bài kiểm tra")]
    [StringLength(100, ErrorMessage = "Tên bài kiểm tra không được vượt quá 100 ký tự")]
    [Display(Name = "Tên bài kiểm tra")]
    public string AssessmentName { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [Display(Name = "Ngày tổ chức kiểm tra")]
    public DateTime? ConductedAt { get; set; }

    // Dữ liệu hỗ trợ giao diện
    public AssessmentDto? AssessmentDetails { get; set; }
    public List<AssessmentTypeDto> AvailableTypes { get; set; } = new();
    public bool HasLockedScores { get; set; }
}
