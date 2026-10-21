using HighSchoolManagementSystem.Models.Entities;
using Microsoft.EntityFrameworkCore.Storage;

namespace HighSchoolManagementSystem.Repositories.Interfaces;

public interface IAttendanceRepository
{
    IQueryable<AttendanceSession> Sessions { get; }
    IQueryable<AttendanceRecord> Records { get; }
    IQueryable<TimetableEntry> Entries { get; }
    IQueryable<Semester> Semesters { get; }
    IQueryable<Class> Classes { get; }
    IQueryable<Student> Students { get; }
    IQueryable<StudentClass> Memberships { get; }
    IQueryable<LeaveRequest> LeaveRequests { get; }
    IQueryable<AttendanceAuditLog> AuditLogs { get; }
    Task<User?> GetActorAsync(int userId);
    Task<IDbContextTransaction> BeginTransactionAsync();
    void Add(AttendanceSession session);
    void AddAudit(AttendanceAuditLog log);
    void RemoveRecords(IEnumerable<AttendanceRecord> records);
    void ExpectVersion(AttendanceSession session, byte[] version);
    Task SaveAsync();
    void ClearTracking();
}
