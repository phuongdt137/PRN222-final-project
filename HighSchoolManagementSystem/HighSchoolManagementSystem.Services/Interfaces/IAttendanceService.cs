using HighSchoolManagementSystem.Services.Common;
using HighSchoolManagementSystem.Services.DTOs;

namespace HighSchoolManagementSystem.Services.Interfaces;

public interface IAttendanceService
{
    Task<ServiceResult<AttendanceListDto>> GetSessionsAsync(int userId, AttendanceFilter filter);
    Task<ServiceResult<AttendanceCreateDto>> PreviewAsync(int userId, AttendanceCreateDto input);
    Task<ServiceResult> CreateAsync(int userId, AttendanceCreateDto input);
    Task<ServiceResult<AttendanceSheetDto>> GetSheetAsync(int userId, int sessionId);
    Task<ServiceResult> SaveAsync(int userId, AttendanceSaveDto input);
    Task<ServiceResult> ManageSessionAsync(int userId, int sessionId, string rowVersion, string action, string? reason);
    Task<ServiceResult<AttendanceStudentHistoryDto>> GetHistoryAsync(int userId, AttendanceFilter filter);
}
