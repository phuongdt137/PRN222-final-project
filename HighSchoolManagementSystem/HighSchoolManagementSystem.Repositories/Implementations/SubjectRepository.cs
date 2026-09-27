using Microsoft.EntityFrameworkCore;
using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Repositories.Data;
using HighSchoolManagementSystem.Repositories.Interfaces;

namespace HighSchoolManagementSystem.Repositories.Implementations;

public class SubjectRepository : ISubjectRepository
{
    private readonly AppDbContext _context;

    public SubjectRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Subject>> GetAllAsync(string? searchTerm = null, string? evaluationType = null, bool? isActive = null)
    {
        var query = _context.Subjects
            .Include(s => s.TeachingAssignments)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(s => s.SubjectCode.ToLower().Contains(term) || s.SubjectName.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(evaluationType) && evaluationType != "All")
        {
            query = query.Where(s => s.EvaluationType == evaluationType);
        }

        if (isActive.HasValue)
        {
            query = query.Where(s => s.IsActive == isActive.Value);
        }

        return await query.OrderBy(s => s.SubjectCode).ToListAsync();
    }

    public async Task<Subject?> GetByIdAsync(int id)
    {
        return await _context.Subjects.FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Subject?> GetByIdWithDetailsAsync(int id)
    {
        return await _context.Subjects
            .Include(s => s.TeachingAssignments)
                .ThenInclude(ta => ta.Teacher)
                    .ThenInclude(t => t.User)
            .Include(s => s.TeachingAssignments)
                .ThenInclude(ta => ta.Class)
            .Include(s => s.TeachingAssignments)
                .ThenInclude(ta => ta.Semester)
            .Include(s => s.SemesterSubjectResults)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Subject?> GetByCodeAsync(string subjectCode)
    {
        return await _context.Subjects
            .FirstOrDefaultAsync(s => s.SubjectCode.ToLower() == subjectCode.Trim().ToLower());
    }

    public async Task<bool> ExistsByCodeAsync(string subjectCode, int? excludeId = null)
    {
        var code = subjectCode.Trim().ToLower();
        return await _context.Subjects
            .AnyAsync(s => s.SubjectCode.ToLower() == code && (!excludeId.HasValue || s.Id != excludeId.Value));
    }

    public async Task AddAsync(Subject subject)
    {
        await _context.Subjects.AddAsync(subject);
    }

    public Task UpdateAsync(Subject subject)
    {
        _context.Subjects.Update(subject);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Subject subject)
    {
        _context.Subjects.Remove(subject);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Class>> GetAllClassesAsync()
    {
        return await _context.Classes
            .Include(c => c.AcademicYear)
            .Include(c => c.GradeLevel)
            .OrderBy(c => c.ClassName)
            .ToListAsync();
    }

    public async Task<IEnumerable<Semester>> GetAllSemestersAsync()
    {
        return await _context.Semesters
            .Include(s => s.AcademicYear)
            .OrderByDescending(s => s.StartDate)
            .ToListAsync();
    }

    public async Task<TeachingAssignment?> GetAssignmentByIdAsync(int id)
    {
        return await _context.TeachingAssignments
            .Include(ta => ta.Subject)
            .Include(ta => ta.Teacher)
            .Include(ta => ta.Class)
            .Include(ta => ta.Semester)
            .FirstOrDefaultAsync(ta => ta.Id == id);
    }

    public async Task<bool> HasAssignmentAsync(int subjectId, int classId, int semesterId, int? excludeAssignmentId = null)
    {
        return await _context.TeachingAssignments
            .AnyAsync(ta => ta.SubjectId == subjectId && ta.ClassId == classId && ta.SemesterId == semesterId && (!excludeAssignmentId.HasValue || ta.Id != excludeAssignmentId.Value));
    }

    public async Task AddAssignmentAsync(TeachingAssignment assignment)
    {
        await _context.TeachingAssignments.AddAsync(assignment);
    }

    public Task DeleteAssignmentAsync(TeachingAssignment assignment)
    {
        _context.TeachingAssignments.Remove(assignment);
        return Task.CompletedTask;
    }

    public async Task<bool> HasScoresOrResultsAsync(int subjectId)
    {
        var hasResults = await _context.SemesterSubjectResults.AnyAsync(r => r.SubjectId == subjectId);
        if (hasResults) return true;

        var hasScores = await _context.TeachingAssignments
            .Where(ta => ta.SubjectId == subjectId)
            .SelectMany(ta => ta.Assessments)
            .SelectMany(a => a.StudentScores)
            .AnyAsync();

        return hasScores;
    }
}
