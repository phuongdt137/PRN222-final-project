using System.Data;
using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Repositories.Data;
using HighSchoolManagementSystem.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace HighSchoolManagementSystem.Repositories.Implementations;

public class AttendanceRepository(AppDbContext context) : IAttendanceRepository
{
    public IQueryable<AttendanceSession> Sessions => context.AttendanceSessions
        .Include(s => s.Class).Include(s => s.Subject).Include(s => s.Semester)
        .Include(s => s.AssignedTeacher).ThenInclude(t => t.User).Include(s => s.Room);
    public IQueryable<AttendanceRecord> Records => context.AttendanceRecords;
    public IQueryable<TimetableEntry> Entries => context.TimetableEntries
        .Include(e => e.TeachingAssignment).ThenInclude(a => a.Teacher).ThenInclude(t => t.User)
        .Include(e => e.TeachingAssignment).ThenInclude(a => a.Class)
        .Include(e => e.TeachingAssignment).ThenInclude(a => a.Semester)
        .Include(e => e.TeachingAssignment).ThenInclude(a => a.Subject)
        .Include(e => e.Room).Include(e => e.TimeSlot);
    public IQueryable<Semester> Semesters => context.Semesters;
    public IQueryable<Class> Classes => context.Classes;
    public IQueryable<Student> Students => context.Students.Include(s => s.User);
    public IQueryable<StudentClass> Memberships => context.StudentClasses;
    public IQueryable<LeaveRequest> LeaveRequests => context.LeaveRequests;
    public IQueryable<AttendanceAuditLog> AuditLogs => context.AttendanceAuditLogs.Include(a => a.ActorUser);

    public Task<User?> GetActorAsync(int userId) => context.Users.AsNoTracking()
        .Include(u => u.Role).Include(u => u.Teacher).Include(u => u.Student).Include(u => u.Parent)
        .SingleOrDefaultAsync(u => u.Id == userId && u.IsActive);

    public Task<IDbContextTransaction> BeginTransactionAsync() =>
        context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
    public void Add(AttendanceSession session) => context.AttendanceSessions.Add(session);
    public void AddAudit(AttendanceAuditLog log) => context.AttendanceAuditLogs.Add(log);
    public void RemoveRecords(IEnumerable<AttendanceRecord> records) => context.AttendanceRecords.RemoveRange(records);
    public void ExpectVersion(AttendanceSession session, byte[] version) =>
        context.Entry(session).Property(s => s.RowVersion).OriginalValue = version;
    public Task SaveAsync() => context.SaveChangesAsync();
    public void ClearTracking() => context.ChangeTracker.Clear();
}
