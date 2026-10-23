using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HighSchoolManagementSystem.MVC.Models.ViewModels.LeaveRequest;

public class LeaveRequestListItemViewModel
{
    public int Id { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string? ClassName { get; set; }
    public string RequestedByName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
    public string? ReviewedByName { get; set; }
}

public class LeaveRequestListPageViewModel
{
    public string? SelectedStatus { get; set; }
    public string ViewerRole { get; set; } = string.Empty;
    public bool CanCreate { get; set; }
    public bool CanReview { get; set; }
    public IEnumerable<SelectListItem> Statuses { get; set; } = Enumerable.Empty<SelectListItem>();
    public IReadOnlyList<LeaveRequestListItemViewModel> Items { get; set; } = Array.Empty<LeaveRequestListItemViewModel>();
}

public class CreateLeaveRequestViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn học sinh.")]
    [Display(Name = "Học sinh")]
    public int StudentId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn ngày bắt đầu.")]
    [DataType(DataType.Date)]
    [Display(Name = "Từ ngày")]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Vui lòng chọn ngày kết thúc.")]
    [DataType(DataType.Date)]
    [Display(Name = "Đến ngày")]
    public DateTime EndDate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Vui lòng nhập lý do nghỉ.")]
    [StringLength(500, ErrorMessage = "Lý do không được vượt quá 500 ký tự.")]
    [Display(Name = "Lý do")]
    public string Reason { get; set; } = string.Empty;

    [Display(Name = "Minh chứng (ảnh)")]
    public IFormFile? AttachmentFile { get; set; }

    public bool ShowStudentPicker { get; set; }
    public IEnumerable<SelectListItem> Students { get; set; } = Enumerable.Empty<SelectListItem>();
}

public class LeaveRequestDetailsViewModel
{
    public int Id { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string? ClassName { get; set; }
    public string RequestedByName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Attachment { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
    public string? ReviewNote { get; set; }
    public DateTime? ReviewAt { get; set; }
    public string? ReviewedByName { get; set; }

    public bool CanReview { get; set; }
    public bool CanCancel { get; set; }

    [StringLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự.")]
    [Display(Name = "Ghi chú duyệt")]
    public string? ReviewNoteInput { get; set; }
}

public class ReviewLeaveRequestViewModel
{
    public int Id { get; set; }

    [StringLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự.")]
    public string? ReviewNote { get; set; }

    public bool Approve { get; set; }
}
