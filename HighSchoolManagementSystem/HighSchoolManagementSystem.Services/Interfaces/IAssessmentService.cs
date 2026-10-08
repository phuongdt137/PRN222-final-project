using HighSchoolManagementSystem.Services.Common;
using HighSchoolManagementSystem.Services.DTOs;

namespace HighSchoolManagementSystem.Services.Interfaces;

public interface IAssessmentService
{
    Task<ServiceResult<IEnumerable<AssessmentDto>>> GetAssessmentsByAssignmentAsync(int assignmentId, int? teacherId = null);
    Task<ServiceResult<IEnumerable<AssessmentDto>>> GetAssessmentsByTeacherAsync(int teacherId, int? semesterId = null, int? classId = null);
    Task<ServiceResult<AssessmentDto>> GetAssessmentByIdAsync(int id, int? teacherId = null);
    Task<ServiceResult<AssessmentDto>> CreateAssessmentAsync(CreateAssessmentDto dto, int? teacherId = null);
    Task<ServiceResult<AssessmentDto>> UpdateAssessmentAsync(UpdateAssessmentDto dto, int? teacherId = null);
    Task<ServiceResult<IEnumerable<AssessmentTypeDto>>> GetAssessmentTypesAsync();
    Task<ServiceResult<IEnumerable<TeachingAssignmentSummaryDto>>> GetTeacherAssignmentsAsync(int teacherId, int? semesterId = null);
    Task<ServiceResult<TeachingAssignmentSummaryDto>> GetTeachingAssignmentSummaryAsync(int assignmentId, int? teacherId = null);
}
