using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Repositories.Interfaces;
using HighSchoolManagementSystem.Services.Common;
using HighSchoolManagementSystem.Services.DTOs;
using HighSchoolManagementSystem.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace HighSchoolManagementSystem.Services.Implementations;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IEmailService _emailService;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        INotificationRepository notificationRepository,
        IEmailService emailService,
        ILogger<NotificationService> logger)
    {
        _notificationRepository = notificationRepository;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<ServiceResult<NotificationPageDto>> GetUserNotificationsAsync(
        int userId, int page = 1, int pageSize = 15, bool? isRead = null)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 15;

            var total = await _notificationRepository.GetTotalCountAsync(userId, isRead);
            var unreadTotal = await _notificationRepository.GetUnreadCountAsync(userId);
            var entities = await _notificationRepository.GetByUserIdAsync(userId, page, pageSize, isRead);

            var items = entities.Select(MapToDto).ToList();
            var totalPages = (int)Math.Ceiling((double)total / pageSize);
            if (totalPages < 1) totalPages = 1;

            var result = new NotificationPageDto
            {
                Items = items,
                CurrentPage = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                TotalItems = total,
                UnreadCount = unreadTotal,
                FilterIsRead = isRead
            };

            return ServiceResult<NotificationPageDto>.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách thông báo cho người dùng {UserId}", userId);
            return ServiceResult<NotificationPageDto>.Fail(ErrorCodes.DatabaseError, "Không thể tải danh sách thông báo.");
        }
    }

    public async Task<ServiceResult<NotificationSummaryDto>> GetSummaryAsync(int userId)
    {
        try
        {
            var unreadCount = await _notificationRepository.GetUnreadCountAsync(userId);
            var recent = await _notificationRepository.GetRecentAsync(userId, 5);

            var summary = new NotificationSummaryDto
            {
                UnreadCount = unreadCount,
                RecentNotifications = recent.Select(MapToDto).ToList()
            };

            return ServiceResult<NotificationSummaryDto>.Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy thông báo gần đây cho người dùng {UserId}", userId);
            return ServiceResult<NotificationSummaryDto>.Fail(ErrorCodes.DatabaseError, "Không thể tải thông báo tóm tắt.");
        }
    }

    public async Task<ServiceResult<bool>> MarkAsReadAsync(int notificationId, int userId)
    {
        try
        {
            await _notificationRepository.MarkAsReadAsync(notificationId, userId);
            return ServiceResult<bool>.Ok(true, "Đã đánh dấu thông báo là đã đọc.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi đánh dấu thông báo {NotificationId} đã đọc cho người dùng {UserId}", notificationId, userId);
            return ServiceResult<bool>.Fail(ErrorCodes.DatabaseError, "Không thể cập nhật trạng thái thông báo.");
        }
    }

    public async Task<ServiceResult<bool>> MarkAllAsReadAsync(int userId)
    {
        try
        {
            await _notificationRepository.MarkAllAsReadAsync(userId);
            return ServiceResult<bool>.Ok(true, "Đã đánh dấu toàn bộ thông báo là đã đọc.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi đánh dấu tất cả thông báo đã đọc cho người dùng {UserId}", userId);
            return ServiceResult<bool>.Fail(ErrorCodes.DatabaseError, "Không thể cập nhật toàn bộ thông báo.");
        }
    }

    public async Task<ServiceResult<bool>> DeleteAsync(int notificationId, int userId)
    {
        try
        {
            await _notificationRepository.DeleteAsync(notificationId, userId);
            return ServiceResult<bool>.Ok(true, "Đã xóa thông báo.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xóa thông báo {NotificationId} cho người dùng {UserId}", notificationId, userId);
            return ServiceResult<bool>.Fail(ErrorCodes.DatabaseError, "Không thể xóa thông báo.");
        }
    }

    public async Task<ServiceResult<bool>> SendNotificationAsync(int senderId, int recipientId, string title, string content)
    {
        if (string.IsNullOrWhiteSpace(title))
            return ServiceResult<bool>.Fail(ErrorCodes.ValidationFailed, "Tiêu đề thông báo không được để trống.");
        if (string.IsNullOrWhiteSpace(content))
            return ServiceResult<bool>.Fail(ErrorCodes.ValidationFailed, "Nội dung thông báo không được để trống.");

        try
        {
            var notif = new Notification
            {
                SenderId = senderId,
                RecipientId = recipientId,
                Title = title.Trim().Length > 100 ? title.Trim().Substring(0, 100) : title.Trim(),
                Content = content.Trim().Length > 1000 ? content.Trim().Substring(0, 1000) : content.Trim(),
                IsRead = false,
                SentAt = DateTime.UtcNow
            };

            await _notificationRepository.AddAsync(notif);
            await _notificationRepository.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true, "Gửi thông báo thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gửi thông báo từ {SenderId} tới {RecipientId}", senderId, recipientId);
            return ServiceResult<bool>.Fail(ErrorCodes.DatabaseError, "Không thể gửi thông báo.");
        }
    }

    public async Task<ServiceResult<bool>> SendBulkNotificationsAsync(int senderId, IEnumerable<int> recipientIds, string title, string content)
    {
        var distinctRecipients = recipientIds.Distinct().Where(id => id > 0).ToList();
        if (!distinctRecipients.Any())
            return ServiceResult<bool>.Ok(true, "Không có người nhận hợp lệ.");

        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
            return ServiceResult<bool>.Fail(ErrorCodes.ValidationFailed, "Tiêu đề và nội dung không được để trống.");

        try
        {
            var safeTitle = title.Trim().Length > 100 ? title.Trim().Substring(0, 100) : title.Trim();
            var safeContent = content.Trim().Length > 1000 ? content.Trim().Substring(0, 1000) : content.Trim();
            var now = DateTime.UtcNow;

            var notifications = distinctRecipients.Select(rId => new Notification
            {
                SenderId = senderId,
                RecipientId = rId,
                Title = safeTitle,
                Content = safeContent,
                IsRead = false,
                SentAt = now
            }).ToList();

            await _notificationRepository.AddRangeAsync(notifications);
            await _notificationRepository.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true, $"Đã gửi thông báo tới {notifications.Count} người nhận.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gửi thông báo hàng loạt từ {SenderId}", senderId);
            return ServiceResult<bool>.Fail(ErrorCodes.DatabaseError, "Không thể gửi thông báo hàng loạt.");
        }
    }

    public async Task<ServiceResult<List<Class>>> GetClassesForAudienceAsync()
    {
        try
        {
            var classes = await _notificationRepository.GetActiveClassesAsync();
            return ServiceResult<List<Class>>.Ok(classes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách lớp cho thông báo");
            return ServiceResult<List<Class>>.Fail(ErrorCodes.DatabaseError, "Không thể tải danh sách lớp học.");
        }
    }

    public async Task<ServiceResult<int>> BroadcastNotificationAsync(
        int senderId, string targetAudience, string title, string content, bool sendEmail = false)
    {
        if (string.IsNullOrWhiteSpace(title))
            return ServiceResult<int>.Fail(ErrorCodes.ValidationFailed, "Tiêu đề thông báo không được để trống.");
        if (string.IsNullOrWhiteSpace(content))
            return ServiceResult<int>.Fail(ErrorCodes.ValidationFailed, "Nội dung thông báo không được để trống.");

        try
        {
            var users = await _notificationRepository.GetUsersByAudienceAsync(targetAudience);
            var recipientIds = users.Select(u => u.Id).Distinct().Where(id => id != senderId).ToList();

            if (!recipientIds.Any())
            {
                return ServiceResult<int>.Fail(ErrorCodes.ValidationFailed, "Không tìm thấy người nhận nào thuộc nhóm đối tượng đã chọn.");
            }

            var bulkResult = await SendBulkNotificationsAsync(senderId, recipientIds, title, content);
            if (!bulkResult.Success)
            {
                return ServiceResult<int>.Fail(bulkResult.ErrorCode ?? ErrorCodes.DatabaseError, bulkResult.Message ?? "Lỗi gửi thông báo.");
            }

            // Gửi email nếu người dùng tích chọn
            if (sendEmail)
            {
                var usersWithEmail = users.Where(u => u.Id != senderId && !string.IsNullOrWhiteSpace(u.Email)).ToList();
                foreach (var u in usersWithEmail)
                {
                    _ = _emailService.SendEmailAsync(
                        u.Email,
                        $"[THPT] {title.Trim()}",
                        $@"
<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: 'Segoe UI', Arial, sans-serif; background-color: #F8FAFC; margin: 0; padding: 24px; color: #1E293B; }}
        .container {{ max-width: 600px; margin: 0 auto; background: #FFFFFF; border-radius: 8px; border: 1px solid #E2E8F0; overflow: hidden; }}
        .header {{ background-color: #1B5B9B; color: #FFFFFF; padding: 20px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 18px; font-weight: 700; }}
        .content {{ padding: 28px 24px; font-size: 14px; line-height: 1.6; }}
        .box {{ background-color: #F1F5F9; border-left: 4px solid #1B5B9B; border-radius: 4px; padding: 16px; margin: 20px 0; }}
        .footer {{ background-color: #F8FAFC; padding: 14px 24px; font-size: 12px; color: #64748B; text-align: center; border-top: 1px solid #E2E8F0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>THÔNG BÁO TỪ NHÀ TRƯỜNG</h1>
        </div>
        <div class='content'>
            <p>Kính gửi <strong>{System.Net.WebUtility.HtmlEncode(u.FullName)}</strong>,</p>
            <div class='box'>
                <h3 style='margin-top:0; color: #1B5B9B;'>{System.Net.WebUtility.HtmlEncode(title)}</h3>
                <p style='white-space: pre-line; margin-bottom: 0;'>{System.Net.WebUtility.HtmlEncode(content)}</p>
            </div>
            <p style='color: #64748B; font-size: 13px;'>Quý Thầy/Cô, Phụ huynh và Học sinh có thể đăng nhập cổng thông tin điện tử của nhà trường để theo dõi chi tiết.</p>
        </div>
        <div class='footer'>
            <p>&copy; {DateTime.Now.Year} Trường THPT • Hệ thống Quản trị & Điều hành học vụ.</p>
        </div>
    </div>
</body>
</html>");
                }
            }

            return ServiceResult<int>.Ok(recipientIds.Count, $"Đã gửi thông báo thành công tới {recipientIds.Count} người nhận.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi phát thông báo tới {Audience}", targetAudience);
            return ServiceResult<int>.Fail(ErrorCodes.DatabaseError, "Lỗi khi phát thông báo.");
        }
    }

    private static NotificationDto MapToDto(Notification entity)
    {
        var (category, color) = DetectCategory(entity.Title, entity.Content);
        return new NotificationDto
        {
            Id = entity.Id,
            SenderId = entity.SenderId,
            SenderName = entity.Sender?.FullName ?? "Hệ thống nhà trường",
            Title = entity.Title,
            Content = entity.Content,
            IsRead = entity.IsRead,
            SentAt = entity.SentAt,
            TimeAgo = FormatTimeAgo(entity.SentAt),
            Category = category,
            BadgeColor = color
        };
    }

    private static (string Category, string Color) DetectCategory(string title, string content)
    {
        var text = (title + " " + content).ToLower();
        if (text.Contains("vắng") || text.Contains("chuyên cần") || text.Contains("đi muộn") || text.Contains("điểm danh"))
            return ("Chuyên cần", "warning");
        if (text.Contains("điểm") || text.Contains("kiểm tra") || text.Contains("học lực"))
            return ("Điểm số", "info");
        if (text.Contains("nghỉ") || text.Contains("phép") || text.Contains("đơn"))
            return ("Nghỉ phép", "success");
        if (text.Contains("thời khóa biểu") || text.Contains("lịch học") || text.Contains("phân công"))
            return ("Thời khóa biểu", "primary");
        return ("Hệ thống", "secondary");
    }

    private static string FormatTimeAgo(DateTime utcTime)
    {
        var span = DateTime.UtcNow - utcTime;
        if (span.TotalSeconds < 60) return "Vừa xong";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} phút trước";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours} giờ trước";
        if (span.TotalDays < 2) return "Hôm qua";
        if (span.TotalDays < 7) return $"{(int)span.TotalDays} ngày trước";
        return utcTime.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
    }
}
