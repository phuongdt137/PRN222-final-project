using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Services.Common;

namespace HighSchoolManagementSystem.Services.Interfaces;

public interface ITimeSlotService
{
    Task<ServiceResult<IReadOnlyList<TimeSlot>>> GetByFiltersAsync(string? session);
    Task<ServiceResult<TimeSlot>> GetByIdAsync(int id);
    Task<ServiceResult<TimeSlot>> CreateAsync(
        string slotCode,
        int periodNo,
        TimeSpan startTime,
        TimeSpan endTime,
        string session);
    Task<ServiceResult<TimeSlot>> UpdateAsync(
        int id,
        string slotCode,
        int periodNo,
        TimeSpan startTime,
        TimeSpan endTime,
        string session);
}
