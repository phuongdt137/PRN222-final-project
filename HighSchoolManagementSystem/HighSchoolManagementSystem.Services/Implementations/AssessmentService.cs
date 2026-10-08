using Microsoft.Extensions.Logging;
using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Repositories.Interfaces;
using HighSchoolManagementSystem.Services.Common;
using HighSchoolManagementSystem.Services.DTOs;
using HighSchoolManagementSystem.Services.Interfaces;

namespace HighSchoolManagementSystem.Services.Implementations;

public class AssessmentService : IAssessmentService
{
    private readonly IAssessmentRepository _assessmentRepository;
    private readonly ILogger<AssessmentService> _logger;

    public AssessmentService(
        IAssessmentRepository assessmentRepository,
        ILogger<AssessmentService> logger)
    {
        _assessmentRepository = assessmentRepository;
        _logger = logger;
    }

    public async Task<ServiceResult<IEnumerable<AssessmentDto>>> GetAssessmentsByAssignmentAsync(int assignmentId, int? teacherId = null)
    {
        try
        {
            var assignment = await _assessmentRepository.GetTeachingAssignmentByIdAsync(assignmentId);
            if (assignment == null)
            {
                return ServiceResult<IEnumerable<AssessmentDto>>.Fail(
                    ErrorCodes.AssessmentAssignmentNotFound,
                    "Không tìm thấy phân công giảng dạy tương ứng.");
            }

            if (teacherId.HasValue && assignment.TeacherId != teacherId.Value)
            {
                return ServiceResult<IEnumerable<AssessmentDto>>.Fail(
                    ErrorCodes.AuthForbidden,
                    "Bạn không có quyền truy cập bài kiểm tra của phân công giảng dạy này.");
            }

            var assessments = await _assessmentRepository.GetByAssignmentIdAsync(assignmentId);
            var dtos = assessments.Select(MapToDto);
            return ServiceResult<IEnumerable<AssessmentDto>>.Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách bài kiểm tra cho phân công giảng dạy Id: {AssignmentId}", assignmentId);
            return ServiceResult<IEnumerable<AssessmentDto>>.Fail(
                ErrorCodes.DatabaseError,
                "Không thể tải danh sách bài kiểm tra. Vui lòng thử lại sau.");
        }
    }

    public async Task<ServiceResult<IEnumerable<AssessmentDto>>> GetAssessmentsByTeacherAsync(int teacherId, int? semesterId = null, int? classId = null)
    {
        try
        {
            var assessments = await _assessmentRepository.GetByTeacherAsync(teacherId, semesterId, classId);
            var dtos = assessments.Select(MapToDto);
            return ServiceResult<IEnumerable<AssessmentDto>>.Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách bài kiểm tra của giáo viên Id: {TeacherId}", teacherId);
            return ServiceResult<IEnumerable<AssessmentDto>>.Fail(
                ErrorCodes.DatabaseError,
                "Không thể tải danh sách bài kiểm tra của giáo viên. Vui lòng thử lại sau.");
        }
    }

    public async Task<ServiceResult<AssessmentDto>> GetAssessmentByIdAsync(int id, int? teacherId = null)
    {
        try
        {
            var assessment = await _assessmentRepository.GetByIdWithDetailsAsync(id);
            if (assessment == null)
            {
                return ServiceResult<AssessmentDto>.Fail(
                    ErrorCodes.AssessmentNotFound,
                    "Không tìm thấy bài kiểm tra.");
            }

            if (teacherId.HasValue && assessment.TeachingAssignment?.TeacherId != teacherId.Value)
            {
                return ServiceResult<AssessmentDto>.Fail(
                    ErrorCodes.AuthForbidden,
                    "Bạn không có quyền truy cập bài kiểm tra này.");
            }

            return ServiceResult<AssessmentDto>.Ok(MapToDto(assessment));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy thông tin bài kiểm tra Id: {Id}", id);
            return ServiceResult<AssessmentDto>.Fail(
                ErrorCodes.DatabaseError,
                "Lỗi hệ thống khi tìm kiếm bài kiểm tra.");
        }
    }

    public async Task<ServiceResult<AssessmentDto>> CreateAssessmentAsync(CreateAssessmentDto dto, int? teacherId = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.AssessmentName))
            {
                return ServiceResult<AssessmentDto>.Fail(
                    ErrorCodes.AssessmentNameRequired,
                    "Tên bài kiểm tra không được để trống.");
            }

            if (dto.AssessmentName.Trim().Length > 100)
            {
                return ServiceResult<AssessmentDto>.Fail(
                    ErrorCodes.ValidationFailed,
                    "Tên bài kiểm tra không được vượt quá 100 ký tự.");
            }

            var assignment = await _assessmentRepository.GetTeachingAssignmentByIdAsync(dto.TeachingAssignmentId);
            if (assignment == null)
            {
                return ServiceResult<AssessmentDto>.Fail(
                    ErrorCodes.AssessmentAssignmentNotFound,
                    "Không tìm thấy phân công giảng dạy tương ứng.");
            }

            if (teacherId.HasValue && assignment.TeacherId != teacherId.Value)
            {
                return ServiceResult<AssessmentDto>.Fail(
                    ErrorCodes.AuthForbidden,
                    "Bạn không có quyền tạo bài kiểm tra cho lớp môn này.");
            }

            var assessmentType = await _assessmentRepository.GetAssessmentTypeByIdAsync(dto.AssessmentTypeId);
            if (assessmentType == null)
            {
                return ServiceResult<AssessmentDto>.Fail(
                    ErrorCodes.AssessmentTypeNotFound,
                    "Loại điểm đánh giá không tồn tại.");
            }

            var currentCount = await _assessmentRepository.CountByTypeAndAssignmentAsync(dto.TeachingAssignmentId, dto.AssessmentTypeId);
            if (currentCount >= assessmentType.MaxEntries)
            {
                return ServiceResult<AssessmentDto>.Fail(
                    ErrorCodes.AssessmentMaxEntriesExceeded,
                    $"Đã đạt số lượng bài kiểm tra tối đa ({assessmentType.MaxEntries}) cho loại điểm '{assessmentType.TypeName}'.");
            }

            var assessment = new Assessment
            {
                TeachingAssignmentId = dto.TeachingAssignmentId,
                AssessmentTypeId = dto.AssessmentTypeId,
                AssessmentName = dto.AssessmentName.Trim(),
                ConductedAt = dto.ConductedAt
            };

            await _assessmentRepository.AddAsync(assessment);
            await _assessmentRepository.SaveChangesAsync();

            var created = await _assessmentRepository.GetByIdWithDetailsAsync(assessment.Id);
            return ServiceResult<AssessmentDto>.Ok(MapToDto(created ?? assessment));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tạo mới bài kiểm tra cho phân công Id: {AssignmentId}", dto.TeachingAssignmentId);
            return ServiceResult<AssessmentDto>.Fail(
                ErrorCodes.DatabaseError,
                "Không thể tạo bài kiểm tra. Vui lòng thử lại sau.");
        }
    }

    public async Task<ServiceResult<AssessmentDto>> UpdateAssessmentAsync(UpdateAssessmentDto dto, int? teacherId = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.AssessmentName))
            {
                return ServiceResult<AssessmentDto>.Fail(
                    ErrorCodes.AssessmentNameRequired,
                    "Tên bài kiểm tra không được để trống.");
            }

            if (dto.AssessmentName.Trim().Length > 100)
            {
                return ServiceResult<AssessmentDto>.Fail(
                    ErrorCodes.ValidationFailed,
                    "Tên bài kiểm tra không được vượt quá 100 ký tự.");
            }

            var assessment = await _assessmentRepository.GetByIdWithDetailsAsync(dto.Id);
            if (assessment == null)
            {
                return ServiceResult<AssessmentDto>.Fail(
                    ErrorCodes.AssessmentNotFound,
                    "Không tìm thấy bài kiểm tra cần cập nhật.");
            }

            if (teacherId.HasValue && assessment.TeachingAssignment?.TeacherId != teacherId.Value)
            {
                return ServiceResult<AssessmentDto>.Fail(
                    ErrorCodes.AuthForbidden,
                    "Bạn không có quyền chỉnh sửa bài kiểm tra này.");
            }

            var newType = await _assessmentRepository.GetAssessmentTypeByIdAsync(dto.AssessmentTypeId);
            if (newType == null)
            {
                return ServiceResult<AssessmentDto>.Fail(
                    ErrorCodes.AssessmentTypeNotFound,
                    "Loại điểm đánh giá không tồn tại.");
            }

            // Nếu thay đổi loại điểm
            if (assessment.AssessmentTypeId != dto.AssessmentTypeId)
            {
                if (await _assessmentRepository.HasLockedScoresAsync(dto.Id))
                {
                    return ServiceResult<AssessmentDto>.Fail(
                        ErrorCodes.AssessmentHasLockedScores,
                        "Không thể thay đổi loại điểm khi bài kiểm tra đã có điểm số bị khóa.");
                }

                var currentCount = await _assessmentRepository.CountByTypeAndAssignmentAsync(
                    assessment.TeachingAssignmentId,
                    dto.AssessmentTypeId,
                    excludeAssessmentId: dto.Id);

                if (currentCount >= newType.MaxEntries)
                {
                    return ServiceResult<AssessmentDto>.Fail(
                        ErrorCodes.AssessmentMaxEntriesExceeded,
                        $"Đã đạt số lượng bài kiểm tra tối đa ({newType.MaxEntries}) cho loại điểm '{newType.TypeName}'.");
                }
            }

            bool typeChanged = assessment.AssessmentTypeId != dto.AssessmentTypeId;
            assessment.AssessmentName = dto.AssessmentName.Trim();
            assessment.AssessmentTypeId = dto.AssessmentTypeId;
            assessment.AssessmentType = newType;
            assessment.ConductedAt = dto.ConductedAt;

            await _assessmentRepository.UpdateAsync(assessment);
            if (typeChanged)
                await _assessmentRepository.RecalculateSubjectResultsAsync(assessment);
            await _assessmentRepository.SaveChangesAsync();

            var updated = await _assessmentRepository.GetByIdWithDetailsAsync(assessment.Id);
            return ServiceResult<AssessmentDto>.Ok(MapToDto(updated ?? assessment));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật bài kiểm tra Id: {Id}", dto.Id);
            return ServiceResult<AssessmentDto>.Fail(
                ErrorCodes.DatabaseError,
                "Không thể cập nhật bài kiểm tra. Vui lòng thử lại sau.");
        }
    }

    public async Task<ServiceResult<IEnumerable<AssessmentTypeDto>>> GetAssessmentTypesAsync()
    {
        try
        {
            var types = await _assessmentRepository.GetAllAssessmentTypesAsync();
            var dtos = types.Select(MapTypeToDto);
            return ServiceResult<IEnumerable<AssessmentTypeDto>>.Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tải danh sách loại điểm kiểm tra.");
            return ServiceResult<IEnumerable<AssessmentTypeDto>>.Fail(
                ErrorCodes.DatabaseError,
                "Không thể tải danh sách loại bài kiểm tra.");
        }
    }

    public async Task<ServiceResult<IEnumerable<TeachingAssignmentSummaryDto>>> GetTeacherAssignmentsAsync(int teacherId, int? semesterId = null)
    {
        try
        {
            var list = await _assessmentRepository.GetTeachingAssignmentsByTeacherAsync(teacherId, semesterId);
            var dtos = list.Select(MapAssignmentSummary);
            return ServiceResult<IEnumerable<TeachingAssignmentSummaryDto>>.Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tải danh sách phân công giảng dạy của giáo viên Id: {TeacherId}", teacherId);
            return ServiceResult<IEnumerable<TeachingAssignmentSummaryDto>>.Fail(
                ErrorCodes.DatabaseError,
                "Không thể tải danh sách phân công giảng dạy.");
        }
    }

    public async Task<ServiceResult<TeachingAssignmentSummaryDto>> GetTeachingAssignmentSummaryAsync(int assignmentId, int? teacherId = null)
    {
        try
        {
            var ta = await _assessmentRepository.GetTeachingAssignmentByIdAsync(assignmentId);
            if (ta == null)
            {
                return ServiceResult<TeachingAssignmentSummaryDto>.Fail(
                    ErrorCodes.AssessmentAssignmentNotFound,
                    "Không tìm thấy phân công giảng dạy tương ứng.");
            }

            if (teacherId.HasValue && ta.TeacherId != teacherId.Value)
            {
                return ServiceResult<TeachingAssignmentSummaryDto>.Fail(
                    ErrorCodes.AuthForbidden,
                    "Bạn không có quyền truy cập phân công giảng dạy này.");
            }

            return ServiceResult<TeachingAssignmentSummaryDto>.Ok(MapAssignmentSummary(ta));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy thông tin phân công giảng dạy Id: {AssignmentId}", assignmentId);
            return ServiceResult<TeachingAssignmentSummaryDto>.Fail(
                ErrorCodes.DatabaseError,
                "Lỗi hệ thống khi tải thông tin phân công.");
        }
    }

    private static AssessmentDto MapToDto(Assessment a)
    {
        var assignment = a.TeachingAssignment;
        var enrolledCount = assignment?.Class?.StudentClasses?.Count(sc => sc.Status == "Enrolled" && sc.LeftAt == null) ?? 0;
        var scores = a.StudentScores ?? new List<StudentScore>();
        var gradedCount = scores.Count(s => s.Score.HasValue);
        var lockedCount = scores.Count(s => s.IsLocked);

        return new AssessmentDto
        {
            Id = a.Id,
            TeachingAssignmentId = a.TeachingAssignmentId,
            AssessmentTypeId = a.AssessmentTypeId,
            AssessmentTypeCode = a.AssessmentType?.TypeCode ?? string.Empty,
            AssessmentTypeName = a.AssessmentType?.TypeName ?? string.Empty,
            Weight = a.AssessmentType?.Weight ?? 1.0m,
            MinEntries = a.AssessmentType?.MinEntries ?? 1,
            MaxEntries = a.AssessmentType?.MaxEntries ?? 5,
            AssessmentName = a.AssessmentName,
            ConductedAt = a.ConductedAt,

            ClassId = assignment?.ClassId ?? 0,
            ClassName = assignment?.Class?.ClassName ?? string.Empty,
            SubjectId = assignment?.SubjectId ?? 0,
            SubjectCode = assignment?.Subject?.SubjectCode ?? string.Empty,
            SubjectName = assignment?.Subject?.SubjectName ?? string.Empty,
            TeacherId = assignment?.TeacherId ?? 0,
            TeacherCode = assignment?.Teacher?.TeacherCode ?? string.Empty,
            TeacherName = assignment?.Teacher?.User?.FullName ?? string.Empty,
            SemesterId = assignment?.SemesterId ?? 0,
            SemesterName = assignment?.Semester?.SemesterName ?? string.Empty,
            AcademicYearCode = assignment?.Class?.AcademicYear?.YearCode ?? string.Empty,

            TotalStudents = enrolledCount,
            GradedStudentsCount = gradedCount,
            LockedScoresCount = lockedCount
        };
    }

    private static AssessmentTypeDto MapTypeToDto(AssessmentType t)
    {
        return new AssessmentTypeDto
        {
            Id = t.Id,
            TypeCode = t.TypeCode,
            TypeName = t.TypeName,
            Weight = t.Weight,
            MinEntries = t.MinEntries,
            MaxEntries = t.MaxEntries
        };
    }

    private static TeachingAssignmentSummaryDto MapAssignmentSummary(TeachingAssignment ta)
    {
        var enrolledCount = ta.Class?.StudentClasses?.Count(sc => sc.Status == "Enrolled" && sc.LeftAt == null) ?? 0;
        return new TeachingAssignmentSummaryDto
        {
            Id = ta.Id,
            ClassId = ta.ClassId,
            ClassName = ta.Class?.ClassName ?? string.Empty,
            SubjectId = ta.SubjectId,
            SubjectCode = ta.Subject?.SubjectCode ?? string.Empty,
            SubjectName = ta.Subject?.SubjectName ?? string.Empty,
            SemesterId = ta.SemesterId,
            SemesterName = ta.Semester?.SemesterName ?? string.Empty,
            AcademicYearCode = ta.Class?.AcademicYear?.YearCode ?? string.Empty,
            Status = ta.Status,
            TotalStudents = enrolledCount,
            AssessmentCount = ta.Assessments?.Count ?? 0
        };
    }
}
