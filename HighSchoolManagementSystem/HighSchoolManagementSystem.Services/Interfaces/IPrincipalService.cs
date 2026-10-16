using HighSchoolManagementSystem.Services.Common;
using HighSchoolManagementSystem.Services.DTOs;

namespace HighSchoolManagementSystem.Services.Interfaces;

public interface IPrincipalService
{
    Task<ServiceResult<PrincipalDashboardDto>> GetDashboardStatsAsync(int? currentYearId = null, int? currentSemesterId = null);
    Task<ServiceResult<AcademicReviewPageDto>> GetAcademicReviewDataAsync(int semesterId, int? gradeLevelId = null, int? classId = null);
    Task<ServiceResult<bool>> FinalizeSemesterAsync(int semesterId, int principalUserId);
    Task<ServiceResult<bool>> UnlockSemesterAsync(int semesterId, int principalUserId);
    Task<ServiceResult<AcademicReportDto>> GetAcademicReportDataAsync(int semesterId, int? gradeLevelId = null, int? subjectId = null);
    Task<ServiceResult<AttendanceReportDto>> GetAttendanceReportDataAsync(int? semesterId = null, int? gradeLevelId = null, DateTime? fromDate = null, DateTime? toDate = null);
    Task<ServiceResult<byte[]>> ExportAcademicReportCsvAsync(int semesterId, int? gradeLevelId = null);
    Task<ServiceResult<byte[]>> ExportAttendanceReportCsvAsync(int? semesterId = null, int? gradeLevelId = null, DateTime? fromDate = null, DateTime? toDate = null);
    Task<ServiceResult<YearEndPromotionPageDto>> GetYearEndPromotionDataAsync(int? sourceAcademicYearId = null, int? sourceClassId = null, int? targetAcademicYearId = null);
    Task<ServiceResult<int>> ExecuteYearEndPromotionAsync(ExecuteYearEndPromotionDto dto, int principalUserId);
}
