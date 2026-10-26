using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Repositories.Interfaces;
using HighSchoolManagementSystem.Services.Common;
using HighSchoolManagementSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HighSchoolManagementSystem.Services.Implementations;

public class TimetableService(ITimetableRepository repository) : ITimetableService
{
    public async Task<ServiceResult<IReadOnlyList<TimetableEntry>>> GetAllAsync(int? semesterId = null) => await Execute(() => repository.GetAllAsync(semesterId), "Không thể tải thời khóa biểu.");
    public async Task<ServiceResult<IReadOnlyList<TeachingAssignment>>> GetAssignmentsAsync(int? semesterId = null) => await Execute(() => repository.GetAssignmentsAsync(semesterId), "Không thể tải phân công giảng dạy.");
    public async Task<ServiceResult<IReadOnlyList<Room>>> GetRoomsAsync() => await Execute(repository.GetRoomsAsync, "Không thể tải phòng học.");
    public async Task<ServiceResult<IReadOnlyList<TimeSlot>>> GetTimeSlotsAsync() => await Execute(repository.GetTimeSlotsAsync, "Không thể tải khung giờ.");
    public Task<int?> GetActiveSemesterIdAsync() => repository.GetActiveSemesterIdAsync();

    public async Task<ServiceResult<TimetableEntry>> GetByIdAsync(int id)
    {
        var item = await repository.GetByIdAsync(id);
        return item == null ? ServiceResult<TimetableEntry>.Fail(ErrorCodes.ResourceNotFound, "Không tìm thấy tiết học.") : ServiceResult<TimetableEntry>.Ok(item);
    }

    public async Task<ServiceResult<TimetableEntry>> SaveAsync(int? id, int assignmentId, int roomId, int timeSlotId, int dayOfWeek)
    {
        if (dayOfWeek < 2 || dayOfWeek > 7) return ServiceResult<TimetableEntry>.Fail(ErrorCodes.ValidationFailed, "Ngày học phải từ Thứ 2 đến Thứ 7.");
        var assignment = await repository.GetAssignmentByIdAsync(assignmentId);
        if (assignment == null) return ServiceResult<TimetableEntry>.Fail(ErrorCodes.ResourceNotFound, "Phân công giảng dạy không tồn tại.");
        if (!await repository.RoomIsUsableAsync(roomId)) return ServiceResult<TimetableEntry>.Fail(ErrorCodes.ValidationFailed, "Phòng học không tồn tại hoặc đang bảo trì.");
        if (!await repository.TimeSlotExistsAsync(timeSlotId)) return ServiceResult<TimetableEntry>.Fail(ErrorCodes.ResourceNotFound, "Khung giờ không tồn tại.");
        if (await repository.HasConflictAsync(assignmentId, roomId, timeSlotId, dayOfWeek, id)) return ServiceResult<TimetableEntry>.Fail(ErrorCodes.ValidationFailed, "Giáo viên, lớp hoặc phòng đã có lịch vào khung giờ này.");

        var item = id.HasValue ? await repository.GetByIdAsync(id.Value) : new TimetableEntry();
        if (id.HasValue && item == null) return ServiceResult<TimetableEntry>.Fail(ErrorCodes.ResourceNotFound, "Không tìm thấy tiết học.");
        item!.TeachingAssignmentId = assignmentId;
        item.RoomId = roomId;
        item.TimeSlotId = timeSlotId;
        item.DayOfWeek = dayOfWeek;
        if (!id.HasValue) await repository.AddAsync(item);
        try { await repository.SaveChangesAsync(); return ServiceResult<TimetableEntry>.Ok(item); }
        catch (DbUpdateException) { return ServiceResult<TimetableEntry>.Fail(ErrorCodes.DatabaseError, "Không thể lưu thời khóa biểu."); }
    }

    public async Task<ServiceResult<bool>> DeleteAsync(int id)
    {
        var item = await repository.GetByIdAsync(id);
        if (item == null) return ServiceResult<bool>.Fail(ErrorCodes.ResourceNotFound, "Không tìm thấy tiết học.");
        if (await repository.HasAttendanceAsync(id)) return ServiceResult<bool>.Fail(ErrorCodes.ValidationFailed, "Không thể xóa tiết học đã có dữ liệu điểm danh.");
        try { await repository.DeleteAsync(item); await repository.SaveChangesAsync(); return ServiceResult<bool>.Ok(true); }
        catch (DbUpdateException) { return ServiceResult<bool>.Fail(ErrorCodes.DatabaseError, "Không thể xóa tiết học."); }
    }

    public async Task<ServiceResult<IReadOnlyList<TimetableEntry>>> GetForClassAsync(int classId, int? semesterId = null) => await Execute(() => repository.GetForClassAsync(classId, semesterId), "Không thể tải thời khóa biểu của lớp.");

    public async Task<ServiceResult<IReadOnlyList<TimetableEntry>>> GetForCurrentUserAsync(string role, int userId, int? semesterId = null)
    {
        semesterId ??= await repository.GetActiveSemesterIdAsync();
        if (!semesterId.HasValue) return ServiceResult<IReadOnlyList<TimetableEntry>>.Ok([]);
        if (role == "Teacher")
        {
            var teacher = await repository.GetTeacherByUserIdAsync(userId);
            if (teacher == null) return ServiceResult<IReadOnlyList<TimetableEntry>>.Ok([]);
            var teaching = await repository.GetForTeacherAsync(teacher.Id, semesterId);
            return ServiceResult<IReadOnlyList<TimetableEntry>>.Ok(teaching);
        }
        if (role == "Student")
        {
            var student = await repository.GetStudentByUserIdAsync(userId);
            return ServiceResult<IReadOnlyList<TimetableEntry>>.Ok(student == null ? [] : await repository.GetForStudentAsync(student.Id, semesterId));
        }
        if (role == "Parent")
        {
            var parent = await repository.GetParentByUserIdAsync(userId);
            return ServiceResult<IReadOnlyList<TimetableEntry>>.Ok(parent == null ? [] : await repository.GetForParentAsync(parent.ParentId, semesterId));
        }
        return ServiceResult<IReadOnlyList<TimetableEntry>>.Ok([]);
    }

    private static async Task<ServiceResult<T>> Execute<T>(Func<Task<T>> action, string message)
    {
        try { return ServiceResult<T>.Ok(await action()); }
        catch { return ServiceResult<T>.Fail(ErrorCodes.DatabaseError, message); }
    }
}
