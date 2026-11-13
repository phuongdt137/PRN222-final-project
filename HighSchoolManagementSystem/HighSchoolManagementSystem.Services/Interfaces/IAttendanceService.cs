using HighSchoolManagementSystem.Services.Common;
using HighSchoolManagementSystem.Services.DTOs;

namespace HighSchoolManagementSystem.Services.Interfaces;

public interface IAttendanceService
{
    Task<ServiceResult<AttendanceListDto>> GetSessionsAsync(int userId, AttendanceFilter filter);

    Task<ServiceResult<AttendanceSheetDto>> GetSheetAsync(int userId, int sessionId = 0, int? timetableEntryId = null, DateTime? date = null);

    Task<ServiceResult> SaveAsync(int userId, AttendanceSaveDto input);

    Task<ServiceResult<AttendanceStudentHistoryDto>> GetHistoryAsync(int userId, AttendanceFilter filter);
}
