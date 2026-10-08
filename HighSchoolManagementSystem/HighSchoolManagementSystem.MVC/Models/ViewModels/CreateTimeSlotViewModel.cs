using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HighSchoolManagementSystem.MVC.Models.ViewModels;

public class CreateTimeSlotViewModel : IValidatableObject
{
    [Required(ErrorMessage = "Vui lòng nhập mã tiết.")]
    [StringLength(20, ErrorMessage = "Mã tiết không được vượt quá 20 ký tự.")]
    [Display(Name = "Mã tiết")]
    public string SlotCode { get; set; } = string.Empty;

    [Range(1, 20, ErrorMessage = "Số tiết phải từ 1 đến 20.")]
    [Display(Name = "Tiết số")]
    public int PeriodNo { get; set; } = 1;

    [Required(ErrorMessage = "Vui lòng chọn giờ bắt đầu.")]
    [Display(Name = "Giờ bắt đầu")]
    [DataType(DataType.Time)]
    public TimeSpan StartTime { get; set; } = new TimeSpan(7, 0, 0);

    [Required(ErrorMessage = "Vui lòng chọn giờ kết thúc.")]
    [Display(Name = "Giờ kết thúc")]
    [DataType(DataType.Time)]
    public TimeSpan EndTime { get; set; } = new TimeSpan(7, 45, 0);

    [Required(ErrorMessage = "Vui lòng chọn buổi học.")]
    [Display(Name = "Buổi học")]
    public string Session { get; set; } = "Morning";

    public IEnumerable<SelectListItem> Sessions { get; set; } = Enumerable.Empty<SelectListItem>();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndTime <= StartTime)
        {
            yield return new ValidationResult(
                "Giờ kết thúc phải sau giờ bắt đầu.",
                new[] { nameof(EndTime) });
        }
    }
}

public class EditTimeSlotViewModel : CreateTimeSlotViewModel
{
    public int Id { get; set; }
}
