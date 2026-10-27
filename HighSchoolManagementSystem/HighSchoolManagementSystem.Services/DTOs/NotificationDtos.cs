using System.ComponentModel.DataAnnotations;

namespace HighSchoolManagementSystem.Services.DTOs;

public class NotificationDto
{
    public int Id { get; set; }
    public int SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime SentAt { get; set; }
    public string TimeAgo { get; set; } = string.Empty;
    public string Category { get; set; } = "Thông báo";
    public string BadgeColor { get; set; } = "primary";
}

public class NotificationSummaryDto
{
    public int UnreadCount { get; set; }
    public List<NotificationDto> RecentNotifications { get; set; } = new();
}

public class NotificationPageDto
{
    public List<NotificationDto> Items { get; set; } = new();
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 15;
    public int TotalPages { get; set; } = 1;
    public int TotalItems { get; set; } = 0;
    public int UnreadCount { get; set; } = 0;
    public bool? FilterIsRead { get; set; }
}

public class CreateBroadcastNotificationDto
{
    [Required(ErrorMessage = "Vui lòng chọn nhóm đối tượng nhận.")]
    public string TargetAudience { get; set; } = "All";

    [Required(ErrorMessage = "Tiêu đề thông báo không được để trống.")]
    [MaxLength(100, ErrorMessage = "Tiêu đề không được vượt quá 100 ký tự.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nội dung thông báo không được để trống.")]
    [MaxLength(1000, ErrorMessage = "Nội dung không được vượt quá 1000 ký tự.")]
    public string Content { get; set; } = string.Empty;

    public bool SendEmail { get; set; } = false;
}
