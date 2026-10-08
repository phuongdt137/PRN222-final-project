using HighSchoolManagementSystem.Models.Entities;

namespace HighSchoolManagementSystem.Repositories.Interfaces;

public interface IRoomRepository
{
    Task<IReadOnlyList<Room>> GetByFiltersAsync(string? status, string? roomType);
    Task<IReadOnlyList<string>> GetDistinctRoomTypesAsync();
    Task<Room?> GetByIdAsync(int id);
    Task<Room?> GetTrackedByIdAsync(int id);
    Task<bool> ExistsByCodeAsync(string roomCode, int? excludeId = null);
    Task AddAsync(Room room);
    Task SaveChangesAsync();
}
