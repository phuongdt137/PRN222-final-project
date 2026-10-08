using HighSchoolManagementSystem.Models.Entities;
using Microsoft.EntityFrameworkCore.Storage;

namespace HighSchoolManagementSystem.Repositories.Interfaces;

public interface IScoreRepository
{
    Task<StudentScore?> GetScoreByIdAsync(int id);
    Task<StudentScore?> GetScoreWithDetailsAsync(int id);
    Task<StudentScore?> GetScoreByStudentAndAssessmentAsync(int studentId, int assessmentId);
    Task<IEnumerable<StudentScore>> GetScoresByAssessmentIdAsync(int assessmentId);
    Task<IEnumerable<StudentScore>> GetScoresByAssignmentIdAsync(int assignmentId);
    Task<IEnumerable<StudentScore>> GetScoresByClassAndSemesterAsync(int classId, int semesterId);
    Task<IEnumerable<StudentScore>> GetScoresByStudentAndSemesterAsync(int studentId, int semesterId);
    
    Task<Assessment?> GetAssessmentWithDetailsAsync(int assessmentId);
    Task<TeachingAssignment?> GetTeachingAssignmentWithDetailsAsync(int assignmentId);
    Task<Class?> GetClassWithStudentsAsync(int classId);
    Task<Class?> GetStudentClassForSemesterAsync(int studentId, int semesterId);
    Task<bool> CanUserViewStudentAsync(int userId, int studentId, int semesterId);
    
    Task<SemesterSubjectResult?> GetStudentSubjectResultAsync(int studentId, int subjectId, int semesterId);
    Task<IEnumerable<SemesterSubjectResult>> GetClassSubjectResultsAsync(int classId, int semesterId);
    Task<SemesterReport?> GetStudentSemesterReportAsync(int studentId, int semesterId);
    Task<IEnumerable<SemesterReport>> GetClassSemesterReportsAsync(int classId, int semesterId);
    
    Task AddScoreAsync(StudentScore score);
    Task UpdateScoreAsync(StudentScore score);
    Task AddScoreAuditLogAsync(ScoreAuditLog log);
    Task<IEnumerable<ScoreAuditLog>> GetScoreAuditLogsByScoreIdAsync(int scoreId);
    
    Task AddOrUpdateSemesterSubjectResultAsync(SemesterSubjectResult result);
    Task AddOrUpdateSemesterReportAsync(SemesterReport report);
    Task<IDbContextTransaction> BeginTransactionAsync();
    Task SaveChangesAsync();
}
