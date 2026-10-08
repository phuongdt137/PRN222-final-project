using Microsoft.EntityFrameworkCore;
using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Repositories.Data;
using HighSchoolManagementSystem.Repositories.Interfaces;

namespace HighSchoolManagementSystem.Repositories.Implementations;

public class AssessmentRepository : IAssessmentRepository
{
    private readonly AppDbContext _context;

    public AssessmentRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Assessment?> GetByIdAsync(int id)
    {
        return await _context.Assessments.FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<Assessment?> GetByIdWithDetailsAsync(int id)
    {
        return await _context.Assessments
            .Include(a => a.AssessmentType)
            .Include(a => a.TeachingAssignment)
                .ThenInclude(ta => ta.Class)
                    .ThenInclude(c => c.AcademicYear)
            .Include(a => a.TeachingAssignment)
                .ThenInclude(ta => ta.Class)
                    .ThenInclude(c => c.StudentClasses)
            .Include(a => a.TeachingAssignment)
                .ThenInclude(ta => ta.Subject)
            .Include(a => a.TeachingAssignment)
                .ThenInclude(ta => ta.Semester)
            .Include(a => a.TeachingAssignment)
                .ThenInclude(ta => ta.Teacher)
                    .ThenInclude(t => t.User)
            .Include(a => a.StudentScores)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<IEnumerable<Assessment>> GetByAssignmentIdAsync(int assignmentId)
    {
        return await _context.Assessments
            .AsNoTracking()
            .Include(a => a.AssessmentType)
            .Include(a => a.TeachingAssignment)
                .ThenInclude(ta => ta.Class)
                    .ThenInclude(c => c.AcademicYear)
            .Include(a => a.TeachingAssignment)
                .ThenInclude(ta => ta.Class)
                    .ThenInclude(c => c.StudentClasses)
            .Include(a => a.TeachingAssignment)
                .ThenInclude(ta => ta.Subject)
            .Include(a => a.TeachingAssignment)
                .ThenInclude(ta => ta.Semester)
            .Include(a => a.TeachingAssignment)
                .ThenInclude(ta => ta.Teacher)
                    .ThenInclude(t => t.User)
            .Include(a => a.StudentScores)
            .Where(a => a.TeachingAssignmentId == assignmentId)
            .OrderBy(a => a.AssessmentType.Weight)
            .ThenBy(a => a.ConductedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Assessment>> GetByTeacherAsync(int teacherId, int? semesterId = null, int? classId = null)
    {
        var query = _context.Assessments
            .AsNoTracking()
            .Include(a => a.AssessmentType)
            .Include(a => a.TeachingAssignment)
                .ThenInclude(ta => ta.Class)
                    .ThenInclude(c => c.AcademicYear)
            .Include(a => a.TeachingAssignment)
                .ThenInclude(ta => ta.Class)
                    .ThenInclude(c => c.StudentClasses)
            .Include(a => a.TeachingAssignment)
                .ThenInclude(ta => ta.Subject)
            .Include(a => a.TeachingAssignment)
                .ThenInclude(ta => ta.Semester)
            .Include(a => a.TeachingAssignment)
                .ThenInclude(ta => ta.Teacher)
                    .ThenInclude(t => t.User)
            .Include(a => a.StudentScores)
            .Where(a => a.TeachingAssignment.TeacherId == teacherId);

        if (semesterId.HasValue)
        {
            query = query.Where(a => a.TeachingAssignment.SemesterId == semesterId.Value);
        }

        if (classId.HasValue)
        {
            query = query.Where(a => a.TeachingAssignment.ClassId == classId.Value);
        }

        return await query
            .OrderBy(a => a.TeachingAssignment.Class.ClassName)
            .ThenBy(a => a.AssessmentType.Weight)
            .ThenBy(a => a.ConductedAt)
            .ToListAsync();
    }

    public async Task<int> CountByTypeAndAssignmentAsync(int assignmentId, int assessmentTypeId, int? excludeAssessmentId = null)
    {
        var query = _context.Assessments
            .Where(a => a.TeachingAssignmentId == assignmentId && a.AssessmentTypeId == assessmentTypeId);

        if (excludeAssessmentId.HasValue)
        {
            query = query.Where(a => a.Id != excludeAssessmentId.Value);
        }

        return await query.CountAsync();
    }

    public async Task<IEnumerable<AssessmentType>> GetAllAssessmentTypesAsync()
    {
        return await _context.AssessmentTypes
            .AsNoTracking()
            .OrderBy(at => at.Weight)
            .ToListAsync();
    }

    public async Task<AssessmentType?> GetAssessmentTypeByIdAsync(int id)
    {
        return await _context.AssessmentTypes.FirstOrDefaultAsync(at => at.Id == id);
    }

    public async Task<TeachingAssignment?> GetTeachingAssignmentByIdAsync(int assignmentId)
    {
        return await _context.TeachingAssignments
            .Include(ta => ta.Class)
                .ThenInclude(c => c.AcademicYear)
            .Include(ta => ta.Class)
                .ThenInclude(c => c.StudentClasses)
            .Include(ta => ta.Subject)
            .Include(ta => ta.Semester)
            .Include(ta => ta.Teacher)
                .ThenInclude(t => t.User)
            .Include(ta => ta.Assessments)
            .FirstOrDefaultAsync(ta => ta.Id == assignmentId);
    }

    public async Task<IEnumerable<TeachingAssignment>> GetTeachingAssignmentsByTeacherAsync(int teacherId, int? semesterId = null)
    {
        var query = _context.TeachingAssignments
            .AsNoTracking()
            .Include(ta => ta.Class)
                .ThenInclude(c => c.AcademicYear)
            .Include(ta => ta.Class)
                .ThenInclude(c => c.StudentClasses)
            .Include(ta => ta.Subject)
            .Include(ta => ta.Semester)
            .Include(ta => ta.Assessments)
            .Where(ta => ta.TeacherId == teacherId);

        if (semesterId.HasValue)
        {
            query = query.Where(ta => ta.SemesterId == semesterId.Value);
        }

        return await query
            .OrderBy(ta => ta.Class.ClassName)
            .ThenBy(ta => ta.Subject.SubjectName)
            .ToListAsync();
    }

    public async Task<bool> HasLockedScoresAsync(int assessmentId)
    {
        return await _context.StudentScores.AnyAsync(s => s.AssessmentId == assessmentId && s.IsLocked);
    }

    public async Task AddAsync(Assessment assessment)
    {
        await _context.Assessments.AddAsync(assessment);
    }

    public Task UpdateAsync(Assessment assessment)
    {
        _context.Assessments.Update(assessment);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Assessment assessment)
    {
        _context.Assessments.Remove(assessment);
        return Task.CompletedTask;
    }

    public async Task RecalculateSubjectResultsAsync(Assessment assessment)
    {
        var assignment = assessment.TeachingAssignment;
        var scores = await _context.StudentScores
            .Include(s => s.Assessment).ThenInclude(a => a.AssessmentType)
            .Where(s => s.Assessment.TeachingAssignment.SubjectId == assignment.SubjectId &&
                s.Assessment.TeachingAssignment.SemesterId == assignment.SemesterId)
            .ToListAsync();
        var results = await _context.SemesterSubjectResults
            .Where(r => r.SubjectId == assignment.SubjectId && r.SemesterId == assignment.SemesterId)
            .ToListAsync();

        foreach (var group in scores.GroupBy(s => s.StudentId))
        {
            var graded = group.Where(s => s.Score.HasValue).ToList();
            var weight = graded.Sum(s => s.Assessment.AssessmentType.Weight);
            decimal? average = weight > 0
                ? Math.Round(graded.Sum(s => s.Score!.Value * s.Assessment.AssessmentType.Weight) / weight, 1)
                : null;
            var result = results.FirstOrDefault(r => r.StudentId == group.Key);
            if (result == null)
            {
                result = new SemesterSubjectResult { StudentId = group.Key, SubjectId = assignment.SubjectId, SemesterId = assignment.SemesterId };
                await _context.SemesterSubjectResults.AddAsync(result);
            }
            result.AverageScore = average;
            result.IsPassed = average == null || average >= 5m;
        }
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
