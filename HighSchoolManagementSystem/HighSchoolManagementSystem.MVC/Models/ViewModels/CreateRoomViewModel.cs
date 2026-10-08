using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HighSchoolManagementSystem.MVC.Models.ViewModels;

public class CreateRoomViewModel : IValidatableObject
{
    [Required(ErrorMessage = "Vui lòng nhập mã phòng.")]
    [StringLength(20, ErrorMessage = "Mã phòng không được vượt quá 20 ký tự.")]
    [Display(Name = "Mã phòng")]
    public string RoomCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập tên phòng.")]
    [StringLength(50, ErrorMessage = "Tên phòng không được vượt quá 50 ký tự.")]
    [Display(Name = "Tên phòng")]
    public string RoomName { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "Tên tòa nhà không được vượt quá 50 ký tự.")]
    [Display(Name = "Tòa nhà")]
    public string? Building { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn loại phòng.")]
    [Display(Name = "Loại phòng")]
    public string RoomType { get; set; } = "Classroom";

    [Range(1, 5000, ErrorMessage = "Sức chứa phải từ 1 đến 5000.")]
    [Display(Name = "Sức chứa")]
    public int Capacity { get; set; } = 40;

    [Required(ErrorMessage = "Vui lòng chọn trạng thái.")]
    [Display(Name = "Trạng thái")]
    public string Status { get; set; } = "Available";

    public IEnumerable<SelectListItem> RoomTypes { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> Statuses { get; set; } = Enumerable.Empty<SelectListItem>();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var isGym = string.Equals(RoomType, "Gym", StringComparison.OrdinalIgnoreCase);
        var maxCapacity = isGym ? 5000 : 500;

        if (Capacity < 1 || Capacity > maxCapacity)
        {
            yield return new ValidationResult(
                isGym
                    ? "Sức chứa nhà đa năng phải từ 1 đến 5000."
                    : "Sức chứa phòng phải từ 1 đến 500.",
                new[] { nameof(Capacity) });
        }
    }
}

public class EditRoomViewModel : CreateRoomViewModel
{
    public int Id { get; set; }
}
