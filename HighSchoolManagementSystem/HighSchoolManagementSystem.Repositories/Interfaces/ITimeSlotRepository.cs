using HighSchoolManagementSystem.Models.Entities;

namespace HighSchoolManagementSystem.Repositories.Interfaces;

public interface ITimeSlotRepository
{
    Task<IReadOnlyList<TimeSlot>> GetByFiltersAsync(string? session);
    Task<TimeSlot?> GetByIdAsync(int id);
    Task<TimeSlot?> GetTrackedByIdAsync(int id);
    Task<bool> ExistsByCodeAsync(string slotCode, int? excludeId = null);
    Task<bool> ExistsByPeriodInSessionAsync(int periodNo, string session, int? excludeId = null);
    Task AddAsync(TimeSlot timeSlot);
    Task SaveChangesAsync();
}
