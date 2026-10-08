using Microsoft.AspNetCore.Mvc.Rendering;

namespace HighSchoolManagementSystem.MVC.Models.ViewModels;

public class TimeSlotListItemViewModel
{
    public int Id { get; set; }
    public string SlotCode { get; set; } = string.Empty;
    public int PeriodNo { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string Session { get; set; } = string.Empty;
}

public class TimeSlotListPageViewModel
{
    public string? SelectedSession { get; set; }
    public IEnumerable<SelectListItem> Sessions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IReadOnlyList<TimeSlotListItemViewModel> TimeSlots { get; set; } = Array.Empty<TimeSlotListItemViewModel>();
}
