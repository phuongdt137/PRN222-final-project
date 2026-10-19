using HighSchoolManagementSystem.Models.Entities;

namespace HighSchoolManagementSystem.Repositories.Interfaces;

public interface ILeaveRequestRepository
{
    Task<LeaveRequest?> GetByIdWithDetailsAsync(int id);
    Task<LeaveRequest?> GetTrackedByIdAsync(int id);
    Task<IReadOnlyList<LeaveRequest>> GetByFiltersAsync(string? status, int? studentId = null);
    Task<IReadOnlyList<LeaveRequest>> GetForHomeroomTeacherAsync(int teacherId, string? status);
    Task<IReadOnlyList<LeaveRequest>> GetByStudentIdsAsync(IReadOnlyCollection<int> studentIds, string? status);
    Task<bool> HasOverlappingAsync(int studentId, DateTime startDate, DateTime endDate, int? excludeId = null);
    Task AddAsync(LeaveRequest leaveRequest);
    Task SaveChangesAsync();
}
