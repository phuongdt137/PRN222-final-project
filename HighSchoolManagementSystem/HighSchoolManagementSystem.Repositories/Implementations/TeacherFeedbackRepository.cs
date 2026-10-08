using Microsoft.EntityFrameworkCore;
using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Repositories.Data;
using HighSchoolManagementSystem.Repositories.Interfaces;

namespace HighSchoolManagementSystem.Repositories.Implementations;

public class TeacherFeedbackRepository : ITeacherFeedbackRepository
{
    private readonly AppDbContext _context;

    public TeacherFeedbackRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<TeacherFeedback?> GetByIdAsync(int id)
    {
        return await _context.TeacherFeedbacks
            .Include(f => f.Teacher)
                .ThenInclude(t => t.User)
            .Include(f => f.Student)
                .ThenInclude(s => s.User)
            .Include(f => f.Semester)
            .FirstOrDefaultAsync(f => f.Id == id);
    }

    public Task<bool> CanTeacherFeedbackAsync(int teacherId, int studentId, int semesterId)
    {
        return _context.StudentClasses.AnyAsync(sc =>
            sc.StudentId == studentId &&
            _context.Semesters.Any(sem => sem.Id == semesterId && sem.AcademicYearId == sc.Class.AcademicYearId) &&
            (sc.Class.HomeroomTeacherId == teacherId ||
             sc.Class.TeachingAssignments.Any(ta => ta.TeacherId == teacherId && ta.SemesterId == semesterId)));
    }

    public async Task<IEnumerable<TeacherFeedback>> GetByStudentAndSemesterAsync(int studentId, int semesterId)
    {
        return await _context.TeacherFeedbacks
            .AsNoTracking()
            .Include(f => f.Teacher)
                .ThenInclude(t => t.User)
            .Include(f => f.Semester)
            .Where(f => f.StudentId == studentId && f.SemesterId == semesterId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<TeacherFeedback>> GetByTeacherAndClassAsync(int teacherId, int classId, int semesterId)
    {
        return await _context.TeacherFeedbacks
            .AsNoTracking()
            .Include(f => f.Student)
                .ThenInclude(s => s.User)
            .Where(f => f.TeacherId == teacherId && f.SemesterId == semesterId && f.Student.StudentClasses.Any(sc => sc.ClassId == classId && sc.Status == "Enrolled" && sc.LeftAt == null))
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(TeacherFeedback feedback)
    {
        await _context.TeacherFeedbacks.AddAsync(feedback);
    }

    public Task UpdateAsync(TeacherFeedback feedback)
    {
        _context.TeacherFeedbacks.Update(feedback);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(TeacherFeedback feedback)
    {
        _context.TeacherFeedbacks.Remove(feedback);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
