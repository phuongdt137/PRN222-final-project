namespace HighSchoolManagementSystem.MVC.Models.ViewModels;

public class StaffDashboardLeaveItemViewModel
{
    public int Id { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string? ClassName { get; set; }
    public string RequestedByName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class StaffDashboardViewModel
{
    public int PendingLeaveCount { get; set; }
    public int ApprovedLeaveCount { get; set; }
    public int RejectedLeaveCount { get; set; }
    public int AvailableRoomCount { get; set; }
    public int TotalRoomCount { get; set; }
    public IReadOnlyList<StaffDashboardLeaveItemViewModel> PendingLeaves { get; set; }
        = Array.Empty<StaffDashboardLeaveItemViewModel>();
}
