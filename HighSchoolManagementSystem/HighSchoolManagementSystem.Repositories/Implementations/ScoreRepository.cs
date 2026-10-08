using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Repositories.Data;
using HighSchoolManagementSystem.Repositories.Interfaces;

namespace HighSchoolManagementSystem.Repositories.Implementations;

public class ScoreRepository : IScoreRepository
{
    private readonly AppDbContext _context;

    public ScoreRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<StudentScore?> GetScoreByIdAsync(int id)
    {
        return await _context.StudentScores.FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<StudentScore?> GetScoreWithDetailsAsync(int id)
    {
        return await _context.StudentScores
            .Include(s => s.Student)
                .ThenInclude(st => st.User)
            .Include(s => s.Assessment)
                .ThenInclude(a => a.TeachingAssignment)
            .Include(s => s.ScoreAuditLogs)
                .ThenInclude(l => l.ModifiedByUser)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<StudentScore?> GetScoreByStudentAndAssessmentAsync(int studentId, int assessmentId)
    {
        return await _context.StudentScores
            .Include(s => s.ScoreAuditLogs)
            .FirstOrDefaultAsync(s => s.StudentId == studentId && s.AssessmentId == assessmentId);
    }

    public async Task<IEnumerable<StudentScore>> GetScoresByAssessmentIdAsync(int assessmentId)
    {
        return await _context.StudentScores
            .AsNoTracking()
            .Include(s => s.Student)
                .ThenInclude(st => st.User)
            .Include(s => s.ScoreAuditLogs)
            .Where(s => s.AssessmentId == assessmentId)
            .ToListAsync();
    }

    public async Task<IEnumerable<StudentScore>> GetScoresByAssignmentIdAsync(int assignmentId)
    {
        return await _context.StudentScores
            .AsNoTracking()
            .Include(s => s.Assessment)
            .Include(s => s.Student)
                .ThenInclude(st => st.User)
            .Where(s => s.Assessment.TeachingAssignmentId == assignmentId)
            .ToListAsync();
    }

    public async Task<IEnumerable<StudentScore>> GetScoresByClassAndSemesterAsync(int classId, int semesterId)
    {
        return await _context.StudentScores
            .AsNoTracking()
            .Include(s => s.Assessment)
                .ThenInclude(a => a.TeachingAssignment)
                    .ThenInclude(ta => ta.Subject)
            .Include(s => s.Assessment)
                .ThenInclude(a => a.AssessmentType)
            .Include(s => s.Student)
                .ThenInclude(st => st.User)
            .Where(s => s.Assessment.TeachingAssignment.ClassId == classId && s.Assessment.TeachingAssignment.SemesterId == semesterId)
            .ToListAsync();
    }

    public async Task<IEnumerable<StudentScore>> GetScoresByStudentAndSemesterAsync(int studentId, int semesterId)
    {
        return await _context.StudentScores
            .AsNoTracking()
            .Include(s => s.Assessment)
                .ThenInclude(a => a.TeachingAssignment)
                    .ThenInclude(ta => ta.Subject)
            .Include(s => s.Assessment)
                .ThenInclude(a => a.TeachingAssignment)
                    .ThenInclude(ta => ta.Class)
                        .ThenInclude(c => c.AcademicYear)
            .Include(s => s.Assessment)
                .ThenInclude(a => a.TeachingAssignment)
                    .ThenInclude(ta => ta.Teacher)
                        .ThenInclude(t => t.User)
            .Include(s => s.Assessment)
                .ThenInclude(a => a.AssessmentType)
            .Where(s => s.StudentId == studentId && s.Assessment.TeachingAssignment.SemesterId == semesterId)
            .ToListAsync();
    }

    public async Task<Assessment?> GetAssessmentWithDetailsAsync(int assessmentId)
    {
        return await _context.Assessments
            .Include(a => a.AssessmentType)
            .Include(a => a.TeachingAssignment)
                .ThenInclude(ta => ta.Class)
                    .ThenInclude(c => c.AcademicYear)
            .Include(a => a.TeachingAssignment)
                .ThenInclude(ta => ta.Class)
                    .ThenInclude(c => c.StudentClasses)
                        .ThenInclude(sc => sc.Student)
                            .ThenInclude(st => st.User)
            .Include(a => a.TeachingAssignment)
                .ThenInclude(ta => ta.Subject)
            .Include(a => a.TeachingAssignment)
                .ThenInclude(ta => ta.Semester)
            .Include(a => a.TeachingAssignment)
                .ThenInclude(ta => ta.Teacher)
                    .ThenInclude(t => t.User)
            .Include(a => a.StudentScores)
                .ThenInclude(s => s.ScoreAuditLogs)
            .FirstOrDefaultAsync(a => a.Id == assessmentId);
    }

    public async Task<TeachingAssignment?> GetTeachingAssignmentWithDetailsAsync(int assignmentId)
    {
        return await _context.TeachingAssignments
            .Include(ta => ta.Class)
                .ThenInclude(c => c.AcademicYear)
            .Include(ta => ta.Class)
                .ThenInclude(c => c.StudentClasses)
                    .ThenInclude(sc => sc.Student)
                        .ThenInclude(st => st.User)
            .Include(ta => ta.Subject)
            .Include(ta => ta.Semester)
            .Include(ta => ta.Teacher)
                .ThenInclude(t => t.User)
            .Include(ta => ta.Assessments)
                .ThenInclude(a => a.AssessmentType)
            .Include(ta => ta.Assessments)
                .ThenInclude(a => a.StudentScores)
            .FirstOrDefaultAsync(ta => ta.Id == assignmentId);
    }

    public async Task<Class?> GetClassWithStudentsAsync(int classId)
    {
        return await _context.Classes
            .Include(c => c.AcademicYear)
            .Include(c => c.HomeroomTeacher)
                .ThenInclude(t => t!.User)
            .Include(c => c.StudentClasses)
                .ThenInclude(sc => sc.Student)
                    .ThenInclude(st => st.User)
            .Include(c => c.TeachingAssignments)
                .ThenInclude(ta => ta.Subject)
            .Include(c => c.TeachingAssignments)
                .ThenInclude(ta => ta.Semester)
            .FirstOrDefaultAsync(c => c.Id == classId);
    }

    public Task<Class?> GetStudentClassForSemesterAsync(int studentId, int semesterId)
    {
        return _context.Classes.AsNoTracking()
            .Include(c => c.AcademicYear)
            .Where(c => c.StudentClasses.Any(sc => sc.StudentId == studentId) &&
                _context.Semesters.Any(sem => sem.Id == semesterId && sem.AcademicYearId == c.AcademicYearId))
            .OrderByDescending(c => c.StudentClasses
                .Where(sc => sc.StudentId == studentId)
                .Max(sc => sc.JoinedAt))
            .FirstOrDefaultAsync();
    }

    public async Task<bool> CanUserViewStudentAsync(int userId, int studentId, int semesterId)
    {
        var user = await _context.Users.AsNoTracking().Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return false;
        if (user.Role.RoleName == "Staff" || user.Role.RoleName == "Principal") return true;
        if (user.Role.RoleName == "Student")
            return await _context.Students.AnyAsync(s => s.Id == studentId && s.UserId == userId);
        if (user.Role.RoleName == "Parent")
            return await _context.Students.AnyAsync(s => s.Id == studentId && s.Parent != null && s.Parent.UserId == userId);
        if (user.Role.RoleName != "Teacher") return false;

        return await _context.StudentClasses.AnyAsync(sc => sc.StudentId == studentId &&
            _context.Semesters.Any(sem => sem.Id == semesterId && sem.AcademicYearId == sc.Class.AcademicYearId) &&
            ((sc.Class.HomeroomTeacher != null && sc.Class.HomeroomTeacher.UserId == userId) ||
             sc.Class.TeachingAssignments.Any(ta => ta.Teacher.UserId == userId && ta.SemesterId == semesterId)));
    }

    public async Task<SemesterSubjectResult?> GetStudentSubjectResultAsync(int studentId, int subjectId, int semesterId)
    {
        return await _context.SemesterSubjectResults
            .FirstOrDefaultAsync(r => r.StudentId == studentId && r.SubjectId == subjectId && r.SemesterId == semesterId);
    }

    public async Task<IEnumerable<SemesterSubjectResult>> GetClassSubjectResultsAsync(int classId, int semesterId)
    {
        return await _context.SemesterSubjectResults
            .AsNoTracking()
            .Include(r => r.Subject)
            .Include(r => r.Student)
                .ThenInclude(st => st.User)
            .Where(r => r.SemesterId == semesterId && r.Student.StudentClasses.Any(sc => sc.ClassId == classId && sc.Status == "Enrolled" && sc.LeftAt == null))
            .ToListAsync();
    }

    public async Task<SemesterReport?> GetStudentSemesterReportAsync(int studentId, int semesterId)
    {
        return await _context.SemesterReports
            .FirstOrDefaultAsync(r => r.StudentId == studentId && r.SemesterId == semesterId);
    }

    public async Task<IEnumerable<SemesterReport>> GetClassSemesterReportsAsync(int classId, int semesterId)
    {
        return await _context.SemesterReports
            .AsNoTracking()
            .Include(r => r.Student)
                .ThenInclude(st => st.User)
            .Where(r => r.SemesterId == semesterId && r.Student.StudentClasses.Any(sc => sc.ClassId == classId && sc.Status == "Enrolled" && sc.LeftAt == null))
            .ToListAsync();
    }

    public async Task AddScoreAsync(StudentScore score)
    {
        await _context.StudentScores.AddAsync(score);
    }

    public Task UpdateScoreAsync(StudentScore score)
    {
        _context.StudentScores.Update(score);
        return Task.CompletedTask;
    }

    public async Task AddScoreAuditLogAsync(ScoreAuditLog log)
    {
        await _context.ScoreAuditLogs.AddAsync(log);
    }

    public async Task<IEnumerable<ScoreAuditLog>> GetScoreAuditLogsByScoreIdAsync(int scoreId)
    {
        return await _context.ScoreAuditLogs
            .AsNoTracking()
            .Include(l => l.ModifiedByUser)
            .Where(l => l.StudentScoreId == scoreId)
            .OrderByDescending(l => l.ModifiedAt)
            .ToListAsync();
    }

    public async Task AddOrUpdateSemesterSubjectResultAsync(SemesterSubjectResult result)
    {
        var existing = await _context.SemesterSubjectResults
            .FirstOrDefaultAsync(r => r.StudentId == result.StudentId && r.SubjectId == result.SubjectId && r.SemesterId == result.SemesterId);

        if (existing == null)
        {
            await _context.SemesterSubjectResults.AddAsync(result);
        }
        else
        {
            existing.AverageScore = result.AverageScore;
            existing.IsPassed = result.IsPassed;
            existing.Remarks = result.Remarks;
            _context.SemesterSubjectResults.Update(existing);
        }
    }

    public async Task AddOrUpdateSemesterReportAsync(SemesterReport report)
    {
        var existing = await _context.SemesterReports
            .FirstOrDefaultAsync(r => r.StudentId == report.StudentId && r.SemesterId == report.SemesterId);

        if (existing == null)
        {
            await _context.SemesterReports.AddAsync(report);
        }
        else
        {
            existing.Gpa = report.Gpa;
            existing.AcademicRank = report.AcademicRank;
            existing.ConductGrade = report.ConductGrade;
            existing.ClassRank = report.ClassRank;
            existing.TeacherComments = report.TeacherComments;
            _context.SemesterReports.Update(existing);
        }
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public Task<IDbContextTransaction> BeginTransactionAsync() => _context.Database.BeginTransactionAsync();
}
