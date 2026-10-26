using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Services.Common;

namespace HighSchoolManagementSystem.Services.Interfaces;

public interface IAssessmentConfigurationService
{
    Task<ServiceResult<IReadOnlyList<AssessmentType>>> GetAllAsync();
    Task<ServiceResult<AssessmentType>> GetByIdAsync(int id);
    Task<ServiceResult<AssessmentType>> SaveAsync(int? id, string typeCode, string typeName, decimal weight,
        int minEntries, int maxEntries, decimal minScore, decimal maxScore);
    Task<ServiceResult> DeleteAsync(int id);
}
