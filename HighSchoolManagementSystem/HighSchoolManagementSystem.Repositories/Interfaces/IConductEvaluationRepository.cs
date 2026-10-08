using HighSchoolManagementSystem.Models.Entities;

namespace HighSchoolManagementSystem.Repositories.Interfaces;

public interface IConductEvaluationRepository
{
    Task<ConductEvaluation?> GetByIdAsync(int id);
    Task<ConductEvaluation?> GetByStudentAndSemesterAsync(int studentId, int semesterId);
    Task<IEnumerable<ConductEvaluation>> GetByClassAndSemesterAsync(int classId, int semesterId);
    Task<IEnumerable<ConductEvaluation>> GetStudentConductHistoryAsync(int studentId);
    Task<IEnumerable<ConductEvaluation>> GetBySemesterAsync(int semesterId);
    Task<Dictionary<int, (int Absent, int Excused, int Late)>> GetClassAttendanceStatsAsync(int classId, DateTime startDate, DateTime endDate);
    Task AddAsync(ConductEvaluation evaluation);
    Task UpdateAsync(ConductEvaluation evaluation);
    Task SaveChangesAsync();
}
