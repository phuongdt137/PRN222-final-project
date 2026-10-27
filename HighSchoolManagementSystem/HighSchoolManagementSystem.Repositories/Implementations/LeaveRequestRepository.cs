using Microsoft.EntityFrameworkCore;
using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Repositories.Data;
using HighSchoolManagementSystem.Repositories.Interfaces;

namespace HighSchoolManagementSystem.Repositories.Implementations;

public class LeaveRequestRepository : ILeaveRequestRepository
{
    private readonly AppDbContext _context;

    public LeaveRequestRepository(AppDbContext context)
    {
        _context = context;
    }

    private IQueryable<LeaveRequest> DetailedQuery() =>
        _context.LeaveRequests
            .Include(lr => lr.Student).ThenInclude(s => s.User)
            .Include(lr => lr.Student).ThenInclude(s => s.StudentClasses).ThenInclude(sc => sc.Class).ThenInclude(c => c.HomeroomTeacher).ThenInclude(t => t!.User)
            .Include(lr => lr.RequestedByUser)
            .Include(lr => lr.ReviewedByUser);

    public async Task<LeaveRequest?> GetByIdWithDetailsAsync(int id)
    {
        return await DetailedQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(lr => lr.Id == id);
    }

    public async Task<LeaveRequest?> GetTrackedByIdAsync(int id)
    {
        return await _context.LeaveRequests
            .FirstOrDefaultAsync(lr => lr.Id == id);
    }

    public async Task<IReadOnlyList<LeaveRequest>> GetByFiltersAsync(string? status, int? studentId = null)
    {
        var query = DetailedQuery().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(lr => lr.Status == status);
        }

        if (studentId.HasValue)
        {
            query = query.Where(lr => lr.StudentId == studentId.Value);
        }

        return await query
            .OrderByDescending(lr => lr.RequestedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<LeaveRequest>> GetForHomeroomTeacherAsync(int teacherId, string? status)
    {
        var query = DetailedQuery()
            .AsNoTracking()
            .Where(lr => lr.Student.StudentClasses.Any(sc =>
                sc.Status == "Enrolled" &&
                sc.LeftAt == null &&
                sc.Class.HomeroomTeacherId == teacherId));

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(lr => lr.Status == status);
        }

        return await query
            .OrderByDescending(lr => lr.RequestedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<LeaveRequest>> GetByStudentIdsAsync(IReadOnlyCollection<int> studentIds, string? status)
    {
        if (studentIds.Count == 0)
        {
            return Array.Empty<LeaveRequest>();
        }

        var query = DetailedQuery()
            .AsNoTracking()
            .Where(lr => studentIds.Contains(lr.StudentId));

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(lr => lr.Status == status);
        }

        return await query
            .OrderByDescending(lr => lr.RequestedAt)
            .ToListAsync();
    }

    public async Task<bool> HasOverlappingAsync(int studentId, DateTime startDate, DateTime endDate, int? excludeId = null)
    {
        var start = startDate.Date;
        var end = endDate.Date;

        var query = _context.LeaveRequests.Where(lr =>
            lr.StudentId == studentId &&
            lr.Status != "Rejected" &&
            lr.Status != "Cancelled" &&
            lr.StartDate.Date <= end &&
            lr.EndDate.Date >= start);

        if (excludeId.HasValue)
        {
            query = query.Where(lr => lr.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task AddAsync(LeaveRequest leaveRequest)
    {
        await _context.LeaveRequests.AddAsync(leaveRequest);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
