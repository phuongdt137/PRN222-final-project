using HighSchoolManagementSystem.Models.Entities;

namespace HighSchoolManagementSystem.Repositories.Interfaces;

public interface ITimetableRepository
{
    Task<IReadOnlyList<TimetableEntry>> GetAllAsync(int? semesterId = null);
    Task<TimetableEntry?> GetByIdAsync(int id);
    Task<TeachingAssignment?> GetAssignmentByIdAsync(int id);
    Task<IReadOnlyList<TeachingAssignment>> GetAssignmentsAsync(int? semesterId = null);
    Task<IReadOnlyList<Room>> GetRoomsAsync();
    Task<IReadOnlyList<TimeSlot>> GetTimeSlotsAsync();
    Task<bool> AssignmentExistsAsync(int assignmentId);
    Task<bool> RoomIsUsableAsync(int roomId);
    Task<bool> TimeSlotExistsAsync(int timeSlotId);
    Task<bool> HasAttendanceAsync(int timetableEntryId);
    Task<int?> GetActiveSemesterIdAsync();
    Task<bool> HasConflictAsync(int teachingAssignmentId, int roomId, int timeSlotId, int dayOfWeek, int? excludedId = null);
    Task AddAsync(TimetableEntry entry);
    Task DeleteAsync(TimetableEntry entry);
    Task SaveChangesAsync();
    Task<Teacher?> GetTeacherByUserIdAsync(int userId);
    Task<Student?> GetStudentByUserIdAsync(int userId);
    Task<Parent?> GetParentByUserIdAsync(int userId);
    Task<IReadOnlyList<TimetableEntry>> GetForTeacherAsync(int teacherId, int? semesterId = null);
    Task<IReadOnlyList<TimetableEntry>> GetForHomeroomTeacherAsync(int teacherId, int? semesterId = null);
    Task<IReadOnlyList<TimetableEntry>> GetForClassAsync(int classId, int? semesterId = null);
    Task<IReadOnlyList<TimetableEntry>> GetForStudentAsync(int studentId, int? semesterId = null);
    Task<IReadOnlyList<TimetableEntry>> GetForParentAsync(int parentId, int? semesterId = null);
}
