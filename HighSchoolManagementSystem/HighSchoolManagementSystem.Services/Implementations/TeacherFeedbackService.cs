using Microsoft.Extensions.Logging;
using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Repositories.Interfaces;
using HighSchoolManagementSystem.Services.Common;
using HighSchoolManagementSystem.Services.DTOs;
using HighSchoolManagementSystem.Services.Interfaces;

namespace HighSchoolManagementSystem.Services.Implementations;

public class TeacherFeedbackService : ITeacherFeedbackService
{
    private readonly ITeacherFeedbackRepository _feedbackRepository;
    private readonly ILogger<TeacherFeedbackService> _logger;

    public TeacherFeedbackService(
        ITeacherFeedbackRepository feedbackRepository,
        ILogger<TeacherFeedbackService> logger)
    {
        _feedbackRepository = feedbackRepository;
        _logger = logger;
    }

    public async Task<ServiceResult<IEnumerable<TeacherFeedbackDto>>> GetStudentFeedbacksAsync(int studentId, int semesterId)
    {
        try
        {
            var feedbacks = await _feedbackRepository.GetByStudentAndSemesterAsync(studentId, semesterId);
            var dtos = feedbacks.Select(MapToDto).ToList();
            return ServiceResult<IEnumerable<TeacherFeedbackDto>>.Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting feedbacks for student {StudentId}, semester {SemesterId}", studentId, semesterId);
            return ServiceResult<IEnumerable<TeacherFeedbackDto>>.Fail(
                ErrorCodes.SystemError,
                "Lỗi hệ thống khi tải nhận xét của học sinh.");
        }
    }

    public async Task<ServiceResult<IEnumerable<TeacherFeedbackDto>>> GetFeedbacksForClassAsync(int teacherId, int classId, int semesterId)
    {
        try
        {
            var feedbacks = await _feedbackRepository.GetByTeacherAndClassAsync(teacherId, classId, semesterId);
            var dtos = feedbacks.Select(MapToDto).ToList();
            return ServiceResult<IEnumerable<TeacherFeedbackDto>>.Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting feedbacks for teacher {TeacherId}, class {ClassId}, semester {SemesterId}", teacherId, classId, semesterId);
            return ServiceResult<IEnumerable<TeacherFeedbackDto>>.Fail(
                ErrorCodes.SystemError,
                "Lỗi hệ thống khi tải danh sách nhận xét.");
        }
    }

    public async Task<ServiceResult<TeacherFeedbackDto>> CreateFeedbackAsync(CreateTeacherFeedbackDto dto, int teacherId)
    {
        try
        {
            if (!await _feedbackRepository.CanTeacherFeedbackAsync(teacherId, dto.StudentId, dto.SemesterId))
                return ServiceResult<TeacherFeedbackDto>.Fail(ErrorCodes.AuthForbidden,
                    "Bạn không được phân công hoặc chủ nhiệm lớp của học sinh trong học kỳ này.");

            if (dto.Content?.Trim().Length > 1000 || !IsValidFeedbackType(dto.FeedbackType))
                return ServiceResult<TeacherFeedbackDto>.Fail(ErrorCodes.ValidationFailed,
                    "Nhận xét tối đa 1000 ký tự; loại nhận xét phải là Academic, Behavior hoặc General.");

            if (string.IsNullOrWhiteSpace(dto.Content))
            {
                return ServiceResult<TeacherFeedbackDto>.Fail(
                    ErrorCodes.FeedbackContentRequired,
                    "Nội dung nhận xét không được để trống.");
            }

            var feedback = new TeacherFeedback
            {
                TeacherId = teacherId,
                StudentId = dto.StudentId,
                SemesterId = dto.SemesterId,
                FeedbackType = string.IsNullOrWhiteSpace(dto.FeedbackType) ? "Academic" : dto.FeedbackType.Trim(),
                Content = dto.Content.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            await _feedbackRepository.AddAsync(feedback);
            await _feedbackRepository.SaveChangesAsync();

            var created = await _feedbackRepository.GetByIdAsync(feedback.Id);
            return ServiceResult<TeacherFeedbackDto>.Ok(MapToDto(created ?? feedback), "Đã thêm nhận xét thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating feedback for student {StudentId} by teacher {TeacherId}", dto.StudentId, teacherId);
            return ServiceResult<TeacherFeedbackDto>.Fail(
                ErrorCodes.SystemError,
                "Lỗi hệ thống khi lưu nhận xét.");
        }
    }

    public async Task<ServiceResult<TeacherFeedbackDto>> UpdateFeedbackAsync(UpdateTeacherFeedbackDto dto, int teacherId)
    {
        try
        {
            var feedback = await _feedbackRepository.GetByIdAsync(dto.Id);
            if (feedback == null)
            {
                return ServiceResult<TeacherFeedbackDto>.Fail(
                    ErrorCodes.FeedbackNotFound,
                    "Không tìm thấy nhận xét.");
            }

            if (feedback.TeacherId != teacherId)
            {
                return ServiceResult<TeacherFeedbackDto>.Fail(
                    ErrorCodes.AuthForbidden,
                    "Bạn không có quyền chỉnh sửa nhận xét này.");
            }

            if (!await _feedbackRepository.CanTeacherFeedbackAsync(teacherId, feedback.StudentId, feedback.SemesterId))
                return ServiceResult<TeacherFeedbackDto>.Fail(ErrorCodes.AuthForbidden, "Bạn không còn quyền chỉnh sửa nhận xét của học sinh này.");

            if (dto.Content?.Trim().Length > 1000 || !IsValidFeedbackType(dto.FeedbackType))
                return ServiceResult<TeacherFeedbackDto>.Fail(ErrorCodes.ValidationFailed,
                    "Nhận xét tối đa 1000 ký tự; loại nhận xét phải là Academic, Behavior hoặc General.");

            if (string.IsNullOrWhiteSpace(dto.Content))
            {
                return ServiceResult<TeacherFeedbackDto>.Fail(
                    ErrorCodes.FeedbackContentRequired,
                    "Nội dung nhận xét không được để trống.");
            }

            feedback.FeedbackType = string.IsNullOrWhiteSpace(dto.FeedbackType) ? feedback.FeedbackType : dto.FeedbackType.Trim();
            feedback.Content = dto.Content.Trim();

            await _feedbackRepository.UpdateAsync(feedback);
            await _feedbackRepository.SaveChangesAsync();

            return ServiceResult<TeacherFeedbackDto>.Ok(MapToDto(feedback), "Cập nhật nhận xét thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating feedback {FeedbackId}", dto.Id);
            return ServiceResult<TeacherFeedbackDto>.Fail(
                ErrorCodes.SystemError,
                "Lỗi hệ thống khi cập nhật nhận xét.");
        }
    }

    public async Task<ServiceResult<bool>> DeleteFeedbackAsync(int id, int teacherId)
    {
        try
        {
            var feedback = await _feedbackRepository.GetByIdAsync(id);
            if (feedback == null)
            {
                return ServiceResult<bool>.Fail(
                    ErrorCodes.FeedbackNotFound,
                    "Không tìm thấy nhận xét.");
            }

            if (feedback.TeacherId != teacherId)
            {
                return ServiceResult<bool>.Fail(
                    ErrorCodes.AuthForbidden,
                    "Bạn không có quyền xóa nhận xét này.");
            }

            if (!await _feedbackRepository.CanTeacherFeedbackAsync(teacherId, feedback.StudentId, feedback.SemesterId))
                return ServiceResult<bool>.Fail(ErrorCodes.AuthForbidden, "Bạn không còn quyền xóa nhận xét của học sinh này.");

            await _feedbackRepository.DeleteAsync(feedback);
            await _feedbackRepository.SaveChangesAsync();

            return ServiceResult<bool>.Ok(true, "Đã xóa nhận xét.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting feedback {FeedbackId}", id);
            return ServiceResult<bool>.Fail(
                ErrorCodes.SystemError,
                "Lỗi hệ thống khi xóa nhận xét.");
        }
    }

    private static TeacherFeedbackDto MapToDto(TeacherFeedback entity)
    {
        return new TeacherFeedbackDto
        {
            Id = entity.Id,
            StudentId = entity.StudentId,
            StudentCode = entity.Student?.StudentCode ?? string.Empty,
            StudentName = entity.Student?.User?.FullName ?? string.Empty,
            TeacherId = entity.TeacherId,
            TeacherName = entity.Teacher?.User?.FullName ?? string.Empty,
            SemesterId = entity.SemesterId,
            SemesterName = entity.Semester?.SemesterName ?? string.Empty,
            FeedbackType = entity.FeedbackType,
            Content = entity.Content,
            CreatedAt = entity.CreatedAt
        };
    }

    private static bool IsValidFeedbackType(string? type) =>
        string.IsNullOrWhiteSpace(type) || type.Trim() is "Academic" or "Behavior" or "General";
}
