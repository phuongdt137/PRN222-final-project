using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Services.Common;

namespace HighSchoolManagementSystem.Services.Interfaces;

public interface ILeaveRequestService
{
    Task<ServiceResult<IReadOnlyList<LeaveRequest>>> GetForStaffAsync(string? status);
    Task<ServiceResult<IReadOnlyList<LeaveRequest>>> GetForTeacherAsync(int teacherUserId, string? status);
    Task<ServiceResult<IReadOnlyList<LeaveRequest>>> GetForStudentAsync(int studentUserId, string? status);
    Task<ServiceResult<IReadOnlyList<LeaveRequest>>> GetForParentAsync(int parentUserId, string? status);
    Task<ServiceResult<LeaveRequest>> GetByIdAsync(int id);
    Task<ServiceResult<LeaveRequest>> CreateAsync(
        int studentId,
        int requestByUserId,
        string reason,
        DateTime startDate,
        DateTime endDate,
        string? attachmentPath);
    Task<ServiceResult<LeaveRequest>> ReviewAsync(
        int id,
        int reviewerUserId,
        bool approve,
        string? reviewNote,
        bool isStaff,
        int? teacherId);
    Task<ServiceResult<bool>> CancelAsync(int id, int requestByUserId);
    Task<ServiceResult<IReadOnlyList<Student>>> GetStudentsForParentAsync(int parentUserId);
}
