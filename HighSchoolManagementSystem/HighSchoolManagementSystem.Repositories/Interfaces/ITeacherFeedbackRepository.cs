using HighSchoolManagementSystem.Models.Entities;

namespace HighSchoolManagementSystem.Repositories.Interfaces;

public interface ITeacherFeedbackRepository
{
    Task<TeacherFeedback?> GetByIdAsync(int id);
    Task<bool> CanTeacherFeedbackAsync(int teacherId, int studentId, int semesterId);
    Task<IEnumerable<TeacherFeedback>> GetByStudentAndSemesterAsync(int studentId, int semesterId);
    Task<IEnumerable<TeacherFeedback>> GetByTeacherAndClassAsync(int teacherId, int classId, int semesterId);
    Task AddAsync(TeacherFeedback feedback);
    Task UpdateAsync(TeacherFeedback feedback);
    Task DeleteAsync(TeacherFeedback feedback);
    Task SaveChangesAsync();
}
