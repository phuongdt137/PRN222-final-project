using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Services.Common;

namespace HighSchoolManagementSystem.Services.Interfaces;

public interface IRoomService
{
    Task<ServiceResult<IReadOnlyList<Room>>> GetByFiltersAsync(string? status, string? roomType);
    Task<ServiceResult<IReadOnlyList<string>>> GetRoomTypesAsync();
    Task<ServiceResult<Room>> GetByIdAsync(int id);
    Task<ServiceResult<Room>> CreateAsync(
        string roomCode,
        string roomName,
        string? building,
        string roomType,
        int capacity,
        string status);
    Task<ServiceResult<Room>> UpdateAsync(
        int id,
        string roomCode,
        string roomName,
        string? building,
        string roomType,
        int capacity,
        string status);
}
