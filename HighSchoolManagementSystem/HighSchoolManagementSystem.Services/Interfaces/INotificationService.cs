using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Services.Common;
using HighSchoolManagementSystem.Services.DTOs;

namespace HighSchoolManagementSystem.Services.Interfaces;

public interface INotificationService
{
    Task<ServiceResult<NotificationPageDto>> GetUserNotificationsAsync(int userId, int page = 1, int pageSize = 15, bool? isRead = null);
    Task<ServiceResult<NotificationSummaryDto>> GetSummaryAsync(int userId);
    Task<ServiceResult<bool>> MarkAsReadAsync(int notificationId, int userId);
    Task<ServiceResult<bool>> MarkAllAsReadAsync(int userId);
    Task<ServiceResult<bool>> DeleteAsync(int notificationId, int userId);
    Task<ServiceResult<bool>> SendNotificationAsync(int senderId, int recipientId, string title, string content);
    Task<ServiceResult<bool>> SendBulkNotificationsAsync(int senderId, IEnumerable<int> recipientIds, string title, string content);
    Task<ServiceResult<List<Class>>> GetClassesForAudienceAsync();
    Task<ServiceResult<int>> BroadcastNotificationAsync(int senderId, string targetAudience, string title, string content, bool sendEmail = false);
}
