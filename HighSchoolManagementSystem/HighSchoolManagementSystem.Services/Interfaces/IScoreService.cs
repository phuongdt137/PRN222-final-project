using HighSchoolManagementSystem.Services.Common;
using HighSchoolManagementSystem.Services.DTOs;

namespace HighSchoolManagementSystem.Services.Interfaces;

public interface IScoreService
{
    Task<ServiceResult<IEnumerable<StudentScoreItemDto>>> GetScoresForAssessmentAsync(int assessmentId, int? teacherId = null);
    Task<ServiceResult<StudentScoreItemDto>> SaveSingleScoreAsync(SaveStudentScoreDto dto, int modifierUserId, int? teacherId = null);
    Task<ServiceResult<int>> SaveBulkScoresAsync(BulkSaveScoreDto dto, int modifierUserId, int? teacherId = null);
    Task<ServiceResult<SubjectScoreMatrixDto>> GetSubjectScoreMatrixAsync(int assignmentId, int? teacherId = null);
    Task<ServiceResult<HomeroomScoreReportDto>> GetHomeroomScoreReportAsync(int classId, int? semesterId = null, int? teacherId = null);
    Task<ServiceResult<StudentTranscriptDto>> GetStudentTranscriptAsync(int studentId, int semesterId, int? requestingUserId = null);
    Task<ServiceResult<IEnumerable<ScoreAuditLogDto>>> GetScoreAuditLogsAsync(int scoreId, int? teacherId = null);
    Task<ServiceResult<bool>> RecalculateStudentSubjectAverageAsync(int studentId, int subjectId, int semesterId);
}
