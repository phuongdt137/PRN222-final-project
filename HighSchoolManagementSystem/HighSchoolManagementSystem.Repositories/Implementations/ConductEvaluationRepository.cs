using Microsoft.EntityFrameworkCore;
using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Repositories.Data;
using HighSchoolManagementSystem.Repositories.Interfaces;

namespace HighSchoolManagementSystem.Repositories.Implementations;

public class ConductEvaluationRepository : IConductEvaluationRepository
{
    private readonly AppDbContext _context;

    public ConductEvaluationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ConductEvaluation?> GetByIdAsync(int id)
    {
        return await _context.ConductEvaluations
            .Include(c => c.Student).ThenInclude(s => s.User)
            .Include(c => c.HomeroomTeacher).ThenInclude(t => t.User)
            .Include(c => c.Semester).ThenInclude(sem => sem.AcademicYear)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<ConductEvaluation?> GetByStudentAndSemesterAsync(int studentId, int semesterId)
    {
        return await _context.ConductEvaluations
            .Include(c => c.Student).ThenInclude(s => s.User)
            .Include(c => c.HomeroomTeacher).ThenInclude(t => t.User)
            .Include(c => c.Semester).ThenInclude(sem => sem.AcademicYear)
            .FirstOrDefaultAsync(c => c.StudentId == studentId && c.SemesterId == semesterId);
    }

    public async Task<IEnumerable<ConductEvaluation>> GetByClassAndSemesterAsync(int classId, int semesterId)
    {
        return await _context.ConductEvaluations
            .Include(c => c.Student).ThenInclude(s => s.User)
            .Include(c => c.HomeroomTeacher).ThenInclude(t => t.User)
            .Include(c => c.Semester)
            .Where(c => c.SemesterId == semesterId &&
                        c.Student.StudentClasses.Any(sc => sc.ClassId == classId && sc.Status == "Enrolled" && sc.LeftAt == null))
            .ToListAsync();
    }

    public async Task<IEnumerable<ConductEvaluation>> GetStudentConductHistoryAsync(int studentId)
    {
        return await _context.ConductEvaluations
            .Include(c => c.Semester).ThenInclude(sem => sem.AcademicYear)
            .Include(c => c.HomeroomTeacher).ThenInclude(t => t.User)
            .Include(c => c.Student).ThenInclude(s => s.StudentClasses).ThenInclude(sc => sc.Class)
            .Where(c => c.StudentId == studentId)
            .OrderByDescending(c => c.Semester.StartDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<ConductEvaluation>> GetBySemesterAsync(int semesterId)
    {
        return await _context.ConductEvaluations
            .Include(c => c.Student).ThenInclude(s => s.User)
            .Include(c => c.HomeroomTeacher).ThenInclude(t => t.User)
            .Include(c => c.Semester)
            .Where(c => c.SemesterId == semesterId)
            .ToListAsync();
    }

    public async Task<Dictionary<int, (int Absent, int Excused, int Late)>> GetClassAttendanceStatsAsync(int classId, DateTime startDate, DateTime endDate)
    {
        try
        {
            var records = await _context.AttendanceRecords
                .AsNoTracking()
                .Where(a => a.AttendanceDate >= startDate && a.AttendanceDate <= endDate &&
                            a.Student.StudentClasses.Any(sc => sc.ClassId == classId && sc.Status == "Enrolled" && sc.LeftAt == null))
                .Select(a => new { a.StudentId, a.Status })
                .ToListAsync();

            return records
                .GroupBy(a => a.StudentId)
                .ToDictionary(
                    g => g.Key,
                    g => (
                        Absent: g.Count(x => string.Equals(x.Status, "Absent", StringComparison.OrdinalIgnoreCase)),
                        Excused: g.Count(x => string.Equals(x.Status, "Excused", StringComparison.OrdinalIgnoreCase)),
                        Late: g.Count(x => string.Equals(x.Status, "Late", StringComparison.OrdinalIgnoreCase))
                    ));
        }
        catch
        {
            return new Dictionary<int, (int Absent, int Excused, int Late)>();
        }
    }

    public async Task AddAsync(ConductEvaluation evaluation)
    {
        await _context.ConductEvaluations.AddAsync(evaluation);
    }

    public Task UpdateAsync(ConductEvaluation evaluation)
    {
        _context.ConductEvaluations.Update(evaluation);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
