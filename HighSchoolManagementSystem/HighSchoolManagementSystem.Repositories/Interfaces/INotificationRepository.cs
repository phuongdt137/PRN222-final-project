using HighSchoolManagementSystem.Models.Entities;

namespace HighSchoolManagementSystem.Repositories.Interfaces;

public interface INotificationRepository
{
    Task<Notification?> GetByIdAsync(int id);
    Task<List<Notification>> GetByUserIdAsync(int userId, int page = 1, int pageSize = 20, bool? isRead = null);
    Task<int> GetTotalCountAsync(int userId, bool? isRead = null);
    Task<int> GetUnreadCountAsync(int userId);
    Task<List<Notification>> GetRecentAsync(int userId, int count = 5);
    Task<List<User>> GetUsersByAudienceAsync(string targetAudience);
    Task<List<Class>> GetActiveClassesAsync();
    Task AddAsync(Notification notification);
    Task AddRangeAsync(IEnumerable<Notification> notifications);
    Task MarkAsReadAsync(int id, int userId);
    Task MarkAllAsReadAsync(int userId);
    Task DeleteAsync(int id, int userId);
    Task SaveChangesAsync();
}
