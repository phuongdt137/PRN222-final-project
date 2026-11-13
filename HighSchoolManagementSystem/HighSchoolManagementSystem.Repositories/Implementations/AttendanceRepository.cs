using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Repositories.Data;
using HighSchoolManagementSystem.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HighSchoolManagementSystem.Repositories.Implementations;

public class AttendanceRepository : IAttendanceRepository
{
    private readonly AppDbContext _context;

    public AttendanceRepository(AppDbContext context)
    {
        _context = context;
    }

    private IQueryable<AttendanceSession> SessionQuery()
    {
        return _context.AttendanceSessions
            .Include(s => s.Class)
            .Include(s => s.Subject)
            .Include(s => s.Semester)
            .Include(s => s.AssignedTeacher).ThenInclude(t => t.User)
            .Include(s => s.Room);
    }

    private IQueryable<TimetableEntry> EntryQuery()
    {
        return _context.TimetableEntries
            .Include(e => e.TeachingAssignment).ThenInclude(a => a.Teacher).ThenInclude(t => t.User)
            .Include(e => e.TeachingAssignment).ThenInclude(a => a.Class)
            .Include(e => e.TeachingAssignment).ThenInclude(a => a.Semester)
            .Include(e => e.TeachingAssignment).ThenInclude(a => a.Subject)
            .Include(e => e.Room)
            .Include(e => e.TimeSlot);
    }

    public async Task<List<TimetableEntry>> GetEntriesAsync(int? semesterId, int? classId, int? teacherId)
    {
        var query = EntryQuery().AsNoTracking();

        if (semesterId.HasValue)
        {
            query = query.Where(e => e.TeachingAssignment.SemesterId == semesterId.Value);
        }

        if (classId.HasValue)
        {
            query = query.Where(e => e.TeachingAssignment.ClassId == classId.Value);
        }

        if (teacherId.HasValue)
        {
            query = query.Where(e => e.TeachingAssignment.TeacherId == teacherId.Value);
        }

        return await query.ToListAsync();
    }

    public async Task<TimetableEntry?> GetEntryAsync(int entryId)
    {
        return await EntryQuery().SingleOrDefaultAsync(e => e.Id == entryId);
    }

    public async Task<List<AttendanceSession>> GetSessionsAsync(DateTime fromDate, DateTime toDate)
    {
        return await SessionQuery()
            .AsNoTracking()
            .Include(s => s.Records)
            .Where(s => s.AttendanceDate >= fromDate && s.AttendanceDate <= toDate)
            .ToListAsync();
    }

    public async Task<AttendanceSession?> GetSessionAsync(int sessionId)
    {
        return await SessionQuery()
            .Include(s => s.Records)
            .SingleOrDefaultAsync(s => s.Id == sessionId);
    }

    public async Task<AttendanceSession?> GetSessionByLessonAsync(int entryId, DateTime date)
    {
        return await SessionQuery()
            .Include(s => s.Records)
            .SingleOrDefaultAsync(s => s.TimetableEntryId == entryId && s.AttendanceDate == date.Date);
    }

    public async Task<Dictionary<int, int>> GetClassSizesAsync()
    {
        return await _context.StudentClasses
            .AsNoTracking()
            .Where(m => m.LeftAt == null && m.Status == "Enrolled")
            .GroupBy(m => m.ClassId)
            .Select(g => new
            {
                ClassId = g.Key,
                Count = g.Select(m => m.StudentId).Distinct().Count()
            })
            .ToDictionaryAsync(g => g.ClassId, g => g.Count);
    }

    private IQueryable<Student> CurrentStudents(int classId)
    {
        return _context.Students
            .AsNoTracking()
            .Where(s => _context.StudentClasses.Any(m =>
                m.ClassId == classId && m.StudentId == s.Id &&
                m.LeftAt == null && m.Status == "Enrolled"));
    }

    public async Task<List<Student>> GetCurrentStudentsAsync(int classId)
    {
        return await CurrentStudents(classId)
            .Include(s => s.User)
            .OrderBy(s => s.StudentCode)
            .ToListAsync();
    }

    public async Task<List<int>> GetCurrentStudentIdsAsync(int classId)
    {
        return await CurrentStudents(classId)
            .Select(s => s.Id)
            .ToListAsync();
    }

    public async Task<List<LeaveRequest>> GetApprovedLeavesAsync(int[] studentIds, DateTime date)
    {
        return await _context.LeaveRequests
            .AsNoTracking()
            .Where(l => studentIds.Contains(l.StudentId) && l.Status == "Approved" &&
                l.StartDate <= date.Date && l.EndDate >= date.Date)
            .OrderBy(l => l.Id)
            .ToListAsync();
    }

    public async Task<List<Semester>> GetSemestersAsync()
    {
        return await _context.Semesters
            .AsNoTracking()
            .Include(s => s.AcademicYear)
            .OrderByDescending(s => s.StartDate)
            .ThenByDescending(s => s.Id)
            .ToListAsync();
    }

    public async Task<List<Class>> GetClassesAsync(int? semesterId)
    {
        var query = _context.Classes.AsNoTracking();

        if (semesterId.HasValue)
        {
            query = query.Where(c => _context.Semesters.Any(s =>
                s.Id == semesterId.Value && s.AcademicYearId == c.AcademicYearId));
        }

        return await query.OrderBy(c => c.ClassName).ToListAsync();
    }

    public async Task<List<Student>> GetChildrenAsync(int parentId)
    {
        return await _context.Students
            .AsNoTracking()
            .Include(s => s.User)
            .Where(s => s.ParentId == parentId)
            .OrderBy(s => s.StudentCode)
            .ToListAsync();
    }

    public async Task<string> GetStudentNameAsync(int studentId)
    {
        return await _context.Students
            .AsNoTracking()
            .Where(s => s.Id == studentId)
            .Select(s => s.User.FullName)
            .SingleAsync();
    }

    private IQueryable<AttendanceRecord> StudentHistory(int studentId)
    {
        return _context.AttendanceRecords
            .AsNoTracking()
            .Where(r => r.StudentId == studentId && r.AttendanceSession.Status != AttendanceStatuses.Cancelled);
    }

    public async Task<List<Subject>> GetHistorySubjectsAsync(int studentId)
    {
        return await StudentHistory(studentId)
            .Select(r => r.AttendanceSession.Subject)
            .Distinct()
            .OrderBy(s => s.SubjectName)
            .ToListAsync();
    }

    private IQueryable<AttendanceRecord> FilterHistory(
        int studentId, DateTime fromDate, DateTime toDate,
        int? semesterId, int? subjectId, string? status)
    {
        var query = StudentHistory(studentId)
            .Where(r => r.AttendanceDate >= fromDate && r.AttendanceDate <= toDate);

        if (semesterId.HasValue)
        {
            query = query.Where(r => r.AttendanceSession.SemesterId == semesterId.Value);
        }

        if (subjectId.HasValue)
        {
            query = query.Where(r => r.AttendanceSession.SubjectId == subjectId.Value);
        }

        if (status == AttendanceStatuses.NotMarked)
        {
            query = query.Where(r => r.AttendanceSession.Status == AttendanceStatuses.Open);
        }
        else if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(r =>
                r.AttendanceSession.Status == AttendanceStatuses.Submitted && r.Status == status);
        }

        return query;
    }

    public async Task<Dictionary<string, int>> GetHistoryCountsAsync(
        int studentId, DateTime fromDate, DateTime toDate,
        int? semesterId, int? subjectId, string? status)
    {
        return await FilterHistory(studentId, fromDate, toDate, semesterId, subjectId, status)
            .GroupBy(r => r.AttendanceSession.Status == AttendanceStatuses.Submitted
                ? r.Status : AttendanceStatuses.NotMarked)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Status, g => g.Count);
    }

    public async Task<List<AttendanceRecord>> GetHistoryRecordsAsync(
        int studentId, DateTime fromDate, DateTime toDate,
        int? semesterId, int? subjectId, string? status, int page, int pageSize)
    {
        return await FilterHistory(studentId, fromDate, toDate, semesterId, subjectId, status)
            .Include(r => r.AttendanceSession).ThenInclude(s => s.Class)
            .Include(r => r.AttendanceSession).ThenInclude(s => s.Subject)
            .Include(r => r.AttendanceSession).ThenInclude(s => s.AssignedTeacher).ThenInclude(t => t.User)
            .Include(r => r.AttendanceSession).ThenInclude(s => s.Room)
            .Include(r => r.AttendanceSession).ThenInclude(s => s.Semester)
            .Include(r => r.RecordedByTeacher).ThenInclude(t => t!.User)
            .OrderByDescending(r => r.AttendanceDate)
            .ThenBy(r => r.AttendanceSession.StartTime)
            .ThenBy(r => r.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<List<Student>> GetAlertStudentsAsync(int[] studentIds)
    {
        return await _context.Students
            .AsNoTracking()
            .Include(s => s.User)
            .Include(s => s.Parent).ThenInclude(p => p!.User)
            .Where(s => studentIds.Contains(s.Id))
            .ToListAsync();
    }

    public async Task<User?> GetActorAsync(int userId)
    {
        return await _context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .Include(u => u.Teacher)
            .Include(u => u.Student)
            .Include(u => u.Parent)
            .SingleOrDefaultAsync(u => u.Id == userId && u.IsActive);
    }

    public void Add(AttendanceSession session)
    {
        _context.AttendanceSessions.Add(session);
    }

    public void AddAudit(AttendanceAuditLog audit)
    {
        _context.AttendanceAuditLogs.Add(audit);
    }

    public async Task<List<AttendanceAuditLog>> GetAuditAsync(int sessionId)
    {
        return await _context.AttendanceAuditLogs
            .AsNoTracking()
            .Include(a => a.ActorUser)
            .Where(a => a.AttendanceSessionId == sessionId)
            .OrderByDescending(a => a.OccurredAt)
            .ThenByDescending(a => a.Id)
            .Take(50)
            .ToListAsync();
    }

    public async Task SaveAsync()
    {
        await _context.SaveChangesAsync();
    }
}
