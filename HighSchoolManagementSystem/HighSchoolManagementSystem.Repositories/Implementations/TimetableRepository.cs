using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Repositories.Data;
using HighSchoolManagementSystem.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HighSchoolManagementSystem.Repositories.Implementations;

public class TimetableRepository(AppDbContext context) : ITimetableRepository
{
    private IQueryable<TimetableEntry> WithDetails() => context.TimetableEntries
        .Include(e => e.TeachingAssignment).ThenInclude(a => a.Teacher).ThenInclude(t => t.User)
        .Include(e => e.TeachingAssignment).ThenInclude(a => a.Subject)
        .Include(e => e.TeachingAssignment).ThenInclude(a => a.Class)
        .Include(e => e.TeachingAssignment).ThenInclude(a => a.Semester)
        .Include(e => e.Room)
        .Include(e => e.TimeSlot);

    public async Task<IReadOnlyList<TimetableEntry>> GetAllAsync(int? semesterId = null)
    {
        var query = WithDetails().AsNoTracking();
        if (semesterId.HasValue) query = query.Where(e => e.TeachingAssignment.SemesterId == semesterId.Value);
        return await query.OrderBy(e => e.DayOfWeek).ThenBy(e => e.TimeSlot.PeriodNo).ThenBy(e => e.TeachingAssignment.Teacher.User.FullName).ToListAsync();
    }

    public Task<TimetableEntry?> GetByIdAsync(int id) => WithDetails().FirstOrDefaultAsync(e => e.Id == id);

    public Task<TeachingAssignment?> GetAssignmentByIdAsync(int id) => context.TeachingAssignments
        .Include(a => a.Teacher).ThenInclude(t => t.User)
        .Include(a => a.Subject)
        .Include(a => a.Class)
        .Include(a => a.Semester)
        .AsNoTracking()
        .FirstOrDefaultAsync(a => a.Id == id);

    public async Task<IReadOnlyList<TeachingAssignment>> GetAssignmentsAsync(int? semesterId = null)
    {
        var query = context.TeachingAssignments.AsNoTracking()
            .Include(a => a.Teacher).ThenInclude(t => t.User)
            .Include(a => a.Subject).Include(a => a.Class).Include(a => a.Semester).AsQueryable();
        if (semesterId.HasValue) query = query.Where(a => a.SemesterId == semesterId.Value);
        return await query.OrderBy(a => a.Teacher.User.FullName).ThenBy(a => a.Class.ClassName).ThenBy(a => a.Subject.SubjectName).ToListAsync();
    }

    public async Task<IReadOnlyList<Room>> GetRoomsAsync() => await context.Rooms.AsNoTracking().Where(r => r.Status != "Maintenance").OrderBy(r => r.RoomCode).ToListAsync();
    public async Task<IReadOnlyList<TimeSlot>> GetTimeSlotsAsync() => await context.TimeSlots.AsNoTracking().OrderBy(t => t.Session).ThenBy(t => t.PeriodNo).ToListAsync();
    public Task<bool> AssignmentExistsAsync(int id) => context.TeachingAssignments.AnyAsync(a => a.Id == id);
    public Task<bool> RoomIsUsableAsync(int id) => context.Rooms.AnyAsync(r => r.Id == id && r.Status != "Maintenance");
    public Task<bool> TimeSlotExistsAsync(int id) => context.TimeSlots.AnyAsync(t => t.Id == id);
    public async Task<bool> HasAttendanceAsync(int id) =>
        await context.AttendanceSessions.AnyAsync(a => a.TimetableEntryId == id) ||
        await context.AttendanceRecords.AnyAsync(a => a.TimetableEntryId == id);
    public Task<int?> GetActiveSemesterIdAsync() => context.Semesters.Where(s => s.IsActive).Select(s => (int?)s.Id).FirstOrDefaultAsync();

    public async Task<bool> HasConflictAsync(int assignmentId, int roomId, int timeSlotId, int dayOfWeek, int? excludedId = null)
    {
        var target = await context.TeachingAssignments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == assignmentId);
        if (target == null) return true;
        var studentIds = context.StudentClasses
            .Where(sc => sc.ClassId == target.ClassId && sc.Status == "Enrolled" && sc.LeftAt == null)
            .Select(sc => sc.StudentId);
        return await context.TimetableEntries.AnyAsync(e =>
            (!excludedId.HasValue || e.Id != excludedId.Value) &&
            e.DayOfWeek == dayOfWeek && e.TimeSlotId == timeSlotId &&
            e.TeachingAssignment.SemesterId == target.SemesterId &&
            (e.RoomId == roomId || e.TeachingAssignment.TeacherId == target.TeacherId || e.TeachingAssignment.ClassId == target.ClassId ||
             e.TeachingAssignment.Class.StudentClasses.Any(sc => studentIds.Contains(sc.StudentId) && sc.Status == "Enrolled" && sc.LeftAt == null)));
    }

    public async Task AddAsync(TimetableEntry entry) => await context.TimetableEntries.AddAsync(entry);
    public Task DeleteAsync(TimetableEntry entry) { context.TimetableEntries.Remove(entry); return Task.CompletedTask; }
    public Task SaveChangesAsync() => context.SaveChangesAsync();
    public Task<Teacher?> GetTeacherByUserIdAsync(int id) => context.Teachers.AsNoTracking().FirstOrDefaultAsync(t => t.UserId == id);
    public Task<Student?> GetStudentByUserIdAsync(int id) => context.Students.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == id);
    public Task<Parent?> GetParentByUserIdAsync(int id) => context.Parents.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == id);
    public Task<IReadOnlyList<TimetableEntry>> GetForTeacherAsync(int id, int? semesterId = null) => GetByQuery(e => e.TeachingAssignment.TeacherId == id, semesterId);
    public Task<IReadOnlyList<TimetableEntry>> GetForHomeroomTeacherAsync(int id, int? semesterId = null) => GetByQuery(e => e.TeachingAssignment.Class.HomeroomTeacherId == id, semesterId);
    public Task<IReadOnlyList<TimetableEntry>> GetForClassAsync(int id, int? semesterId = null) => GetByQuery(e => e.TeachingAssignment.ClassId == id, semesterId);
    public Task<IReadOnlyList<TimetableEntry>> GetForStudentAsync(int id, int? semesterId = null) => GetByQuery(e => e.TeachingAssignment.Class.StudentClasses.Any(sc => sc.StudentId == id && sc.Status == "Enrolled" && sc.LeftAt == null), semesterId);
    public Task<IReadOnlyList<TimetableEntry>> GetForParentAsync(int id, int? semesterId = null) => GetByQuery(e => e.TeachingAssignment.Class.StudentClasses.Any(sc => sc.Student.ParentId == id && sc.Status == "Enrolled" && sc.LeftAt == null), semesterId);

    private async Task<IReadOnlyList<TimetableEntry>> GetByQuery(System.Linq.Expressions.Expression<Func<TimetableEntry, bool>> predicate, int? semesterId)
    {
        var query = WithDetails().AsNoTracking().Where(predicate);
        if (semesterId.HasValue) query = query.Where(e => e.TeachingAssignment.SemesterId == semesterId.Value);
        return await query.OrderBy(e => e.DayOfWeek).ThenBy(e => e.TimeSlot.PeriodNo).ToListAsync();
    }
}
