using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Repositories.Data;
using HighSchoolManagementSystem.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HighSchoolManagementSystem.Repositories.Implementations;

public class NotificationRepository : INotificationRepository
{
    private readonly AppDbContext _context;

    public NotificationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Notification?> GetByIdAsync(int id)
    {
        return await _context.Notifications
            .Include(n => n.Sender)
            .Include(n => n.Recipient)
            .FirstOrDefaultAsync(n => n.Id == id);
    }

    public async Task<List<Notification>> GetByUserIdAsync(int userId, int page = 1, int pageSize = 20, bool? isRead = null)
    {
        var query = _context.Notifications
            .AsNoTracking()
            .Include(n => n.Sender)
            .Where(n => n.RecipientId == userId);

        if (isRead.HasValue)
        {
            query = query.Where(n => n.IsRead == isRead.Value);
        }

        return await query
            .OrderByDescending(n => n.SentAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetTotalCountAsync(int userId, bool? isRead = null)
    {
        var query = _context.Notifications.Where(n => n.RecipientId == userId);
        if (isRead.HasValue)
        {
            query = query.Where(n => n.IsRead == isRead.Value);
        }
        return await query.CountAsync();
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await _context.Notifications
            .CountAsync(n => n.RecipientId == userId && !n.IsRead);
    }

    public async Task<List<Notification>> GetRecentAsync(int userId, int count = 5)
    {
        return await _context.Notifications
            .AsNoTracking()
            .Include(n => n.Sender)
            .Where(n => n.RecipientId == userId)
            .OrderByDescending(n => n.SentAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task AddAsync(Notification notification)
    {
        await _context.Notifications.AddAsync(notification);
    }

    public async Task AddRangeAsync(IEnumerable<Notification> notifications)
    {
        await _context.Notifications.AddRangeAsync(notifications);
    }

    public async Task MarkAsReadAsync(int id, int userId)
    {
        var item = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.RecipientId == userId);
        if (item != null && !item.IsRead)
        {
            item.IsRead = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task MarkAllAsReadAsync(int userId)
    {
        var unreadItems = await _context.Notifications
            .Where(n => n.RecipientId == userId && !n.IsRead)
            .ToListAsync();

        if (unreadItems.Any())
        {
            foreach (var item in unreadItems)
            {
                item.IsRead = true;
            }
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteAsync(int id, int userId)
    {
        var item = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.RecipientId == userId);
        if (item != null)
        {
            _context.Notifications.Remove(item);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<Class>> GetActiveClassesAsync()
    {
        return await _context.Classes
            .AsNoTracking()
            .OrderBy(c => c.ClassName)
            .ToListAsync();
    }

    public async Task<List<User>> GetUsersByAudienceAsync(string targetAudience)
    {
        if (string.IsNullOrWhiteSpace(targetAudience) || targetAudience == "All")
        {
            return await _context.Users
                .Where(u => u.IsActive)
                .ToListAsync();
        }

        if (targetAudience == "Teacher" || targetAudience == "Staff" || targetAudience == "Student" || targetAudience == "Parent")
        {
            return await _context.Users
                .Include(u => u.Role)
                .Where(u => u.Role.RoleName == targetAudience && u.IsActive)
                .ToListAsync();
        }

        if (targetAudience.StartsWith("Class_"))
        {
            var parts = targetAudience.Split('_');
            if (parts.Length == 2 && int.TryParse(parts[1], out var classId))
            {
                var studentUsers = await _context.StudentClasses
                    .Where(sc => sc.ClassId == classId && sc.Status == "Enrolled" && sc.LeftAt == null)
                    .Include(sc => sc.Student)
                        .ThenInclude(s => s.User)
                    .Select(sc => sc.Student.User)
                    .Where(u => u.IsActive)
                    .ToListAsync();

                var parentUsers = await _context.StudentClasses
                    .Where(sc => sc.ClassId == classId && sc.Status == "Enrolled" && sc.LeftAt == null)
                    .Include(sc => sc.Student)
                        .ThenInclude(s => s.Parent)
                            .ThenInclude(p => p!.User)
                    .Where(sc => sc.Student.Parent != null && sc.Student.Parent.User != null && sc.Student.Parent.User.IsActive)
                    .Select(sc => sc.Student.Parent!.User)
                    .ToListAsync();

                return studentUsers.Concat(parentUsers).DistinctBy(u => u.Id).ToList();
            }
        }

        return new List<User>();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
