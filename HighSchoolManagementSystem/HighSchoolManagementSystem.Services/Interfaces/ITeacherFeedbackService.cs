using HighSchoolManagementSystem.Services.Common;
using HighSchoolManagementSystem.Services.DTOs;

namespace HighSchoolManagementSystem.Services.Interfaces;

public interface ITeacherFeedbackService
{
    Task<ServiceResult<IEnumerable<TeacherFeedbackDto>>> GetStudentFeedbacksAsync(int studentId, int semesterId);
    Task<ServiceResult<IEnumerable<TeacherFeedbackDto>>> GetFeedbacksForClassAsync(int teacherId, int classId, int semesterId);
    Task<ServiceResult<TeacherFeedbackDto>> CreateFeedbackAsync(CreateTeacherFeedbackDto dto, int teacherId);
    Task<ServiceResult<TeacherFeedbackDto>> UpdateFeedbackAsync(UpdateTeacherFeedbackDto dto, int teacherId);
    Task<ServiceResult<bool>> DeleteFeedbackAsync(int id, int teacherId);
}
