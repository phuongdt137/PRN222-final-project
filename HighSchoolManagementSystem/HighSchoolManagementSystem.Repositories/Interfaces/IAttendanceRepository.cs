using HighSchoolManagementSystem.Models.Entities;

namespace HighSchoolManagementSystem.Repositories.Interfaces;

public interface IAttendanceRepository
{
    Task<List<TimetableEntry>> GetEntriesAsync(int? semesterId, int? classId, int? teacherId);

    Task<TimetableEntry?> GetEntryAsync(int entryId);

    Task<List<AttendanceSession>> GetSessionsAsync(DateTime fromDate, DateTime toDate);
    // Single-session reads are tracked so SaveAsync can persist record updates.
    Task<AttendanceSession?> GetSessionAsync(int sessionId);

    Task<AttendanceSession?> GetSessionByLessonAsync(int entryId, DateTime date);

    Task<Dictionary<int, int>> GetClassSizesAsync();

    Task<List<Student>> GetCurrentStudentsAsync(int classId);

    Task<List<int>> GetCurrentStudentIdsAsync(int classId);

    Task<List<LeaveRequest>> GetApprovedLeavesAsync(int[] studentIds, DateTime date);

    Task<List<Semester>> GetSemestersAsync();

    Task<List<Class>> GetClassesAsync(int? semesterId);

    Task<List<Student>> GetChildrenAsync(int parentId);

    Task<string> GetStudentNameAsync(int studentId);

    Task<List<Subject>> GetHistorySubjectsAsync(int studentId);

    Task<Dictionary<string, int>> GetHistoryCountsAsync(int studentId, DateTime fromDate, DateTime toDate,
        int? semesterId, int? subjectId, string? status);

    Task<List<AttendanceRecord>> GetHistoryRecordsAsync(int studentId, DateTime fromDate, DateTime toDate,
        int? semesterId, int? subjectId, string? status, int page, int pageSize);

    Task<List<Student>> GetAlertStudentsAsync(int[] studentIds);

    Task<User?> GetActorAsync(int userId);

    void Add(AttendanceSession session);

    void AddAudit(AttendanceAuditLog audit);

    Task<List<AttendanceAuditLog>> GetAuditAsync(int sessionId);

    Task SaveAsync();
}
