using HighSchoolManagementSystem.Models.Entities;

namespace HighSchoolManagementSystem.Repositories.Interfaces;

public interface ISubjectRepository
{
    Task<IEnumerable<Subject>> GetAllAsync(string? searchTerm = null, string? evaluationType = null, bool? isActive = null);
    Task<Subject?> GetByIdAsync(int id);
    Task<Subject?> GetByIdWithDetailsAsync(int id);
    Task<Subject?> GetByCodeAsync(string subjectCode);
    Task<bool> ExistsByCodeAsync(string subjectCode, int? excludeId = null);
    Task AddAsync(Subject subject);
    Task UpdateAsync(Subject subject);
    Task DeleteAsync(Subject subject);
    Task SaveChangesAsync();
    Task<IEnumerable<Class>> GetAllClassesAsync();
    Task<IEnumerable<Semester>> GetAllSemestersAsync();
    Task<TeachingAssignment?> GetAssignmentByIdAsync(int id);
    Task<bool> HasAssignmentAsync(int subjectId, int classId, int semesterId, int? excludeAssignmentId = null);
    Task AddAssignmentAsync(TeachingAssignment assignment);
    Task DeleteAssignmentAsync(TeachingAssignment assignment);
    Task<bool> HasScoresOrResultsAsync(int subjectId);
}
