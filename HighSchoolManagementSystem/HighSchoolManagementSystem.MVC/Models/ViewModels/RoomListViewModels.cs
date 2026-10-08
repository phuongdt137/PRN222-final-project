using Microsoft.AspNetCore.Mvc.Rendering;

namespace HighSchoolManagementSystem.MVC.Models.ViewModels;

public class RoomListItemViewModel
{
    public int Id { get; set; }
    public string RoomCode { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public string? Building { get; set; }
    public string RoomType { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class RoomListPageViewModel
{
    public string? SelectedStatus { get; set; }
    public string? SelectedRoomType { get; set; }
    public IEnumerable<SelectListItem> Statuses { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> RoomTypes { get; set; } = Enumerable.Empty<SelectListItem>();
    public IReadOnlyList<RoomListItemViewModel> Rooms { get; set; } = Array.Empty<RoomListItemViewModel>();
}
