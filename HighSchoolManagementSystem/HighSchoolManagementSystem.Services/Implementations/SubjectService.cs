using Microsoft.Extensions.Logging;
using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Repositories.Interfaces;
using HighSchoolManagementSystem.Services.Common;
using HighSchoolManagementSystem.Services.DTOs;
using HighSchoolManagementSystem.Services.Interfaces;

namespace HighSchoolManagementSystem.Services.Implementations;

public class SubjectService : ISubjectService
{
    private readonly ISubjectRepository _subjectRepository;
    private readonly ILogger<SubjectService> _logger;

    public SubjectService(ISubjectRepository subjectRepository, ILogger<SubjectService> logger)
    {
        _subjectRepository = subjectRepository;
        _logger = logger;
    }

    public async Task<ServiceResult<IEnumerable<Subject>>> GetAllSubjectsAsync(string? searchTerm = null, string? evaluationType = null, bool? isActive = null)
    {
        try
        {
            var subjects = await _subjectRepository.GetAllAsync(searchTerm, evaluationType, isActive);
            return ServiceResult<IEnumerable<Subject>>.Ok(subjects);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách môn học.");
            return ServiceResult<IEnumerable<Subject>>.Fail(ErrorCodes.DatabaseError, "Không thể tải danh sách môn học. Vui lòng thử lại sau.");
        }
    }

    public async Task<ServiceResult<Subject>> GetSubjectByIdAsync(int id)
    {
        try
        {
            var subject = await _subjectRepository.GetByIdAsync(id);
            if (subject == null)
            {
                return ServiceResult<Subject>.Fail(ErrorCodes.SubjectNotFound, "Không tìm thấy môn học yêu cầu.");
            }

            return ServiceResult<Subject>.Ok(subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy thông tin môn học Id: {Id}", id);
            return ServiceResult<Subject>.Fail(ErrorCodes.DatabaseError, "Lỗi hệ thống khi tìm kiếm môn học.");
        }
    }

    public async Task<ServiceResult<Subject>> GetSubjectDetailsAsync(int id)
    {
        try
        {
            var subject = await _subjectRepository.GetByIdWithDetailsAsync(id);
            if (subject == null)
            {
                return ServiceResult<Subject>.Fail(ErrorCodes.SubjectNotFound, "Không tìm thấy chi tiết môn học.");
            }

            return ServiceResult<Subject>.Ok(subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy chi tiết môn học Id: {Id}", id);
            return ServiceResult<Subject>.Fail(ErrorCodes.DatabaseError, "Lỗi hệ thống khi tải chi tiết môn học.");
        }
    }

    public async Task<ServiceResult<Subject>> CreateSubjectAsync(CreateSubjectDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.SubjectCode))
            {
                return ServiceResult<Subject>.Fail(ErrorCodes.ValidationFailed, "Mã môn học không được để trống.");
            }

            if (string.IsNullOrWhiteSpace(dto.SubjectName))
            {
                return ServiceResult<Subject>.Fail(ErrorCodes.SubjectNameRequired, "Tên môn học không được để trống.");
            }

            var cleanCode = dto.SubjectCode.Trim().ToUpper();
            var isDuplicate = await _subjectRepository.ExistsByCodeAsync(cleanCode);
            if (isDuplicate)
            {
                return ServiceResult<Subject>.Fail(ErrorCodes.SubjectCodeDuplicate, $"Mã môn học '{cleanCode}' đã tồn tại trong hệ thống. Vui lòng chọn mã khác.");
            }

            var evalType = dto.EvaluationType == "PassFail" ? "PassFail" : "Score";

            var subject = new Subject
            {
                SubjectCode = cleanCode,
                SubjectName = dto.SubjectName.Trim(),
                EvaluationType = evalType,
                Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
                IsActive = dto.IsActive
            };

            await _subjectRepository.AddAsync(subject);
            await _subjectRepository.SaveChangesAsync();

            _logger.LogInformation("Đã tạo mới môn học {Code} - {Name}", subject.SubjectCode, subject.SubjectName);
            return ServiceResult<Subject>.Ok(subject, "Thêm mới môn học thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tạo môn học mới.");
            return ServiceResult<Subject>.Fail(ErrorCodes.DatabaseError, "Đã xảy ra lỗi khi tạo môn học.");
        }
    }

    public async Task<ServiceResult<Subject>> UpdateSubjectAsync(UpdateSubjectDto dto)
    {
        try
        {
            var subject = await _subjectRepository.GetByIdAsync(dto.Id);
            if (subject == null)
            {
                return ServiceResult<Subject>.Fail(ErrorCodes.SubjectNotFound, "Môn học cần cập nhật không tồn tại.");
            }

            if (string.IsNullOrWhiteSpace(dto.SubjectCode))
            {
                return ServiceResult<Subject>.Fail(ErrorCodes.ValidationFailed, "Mã môn học không được để trống.");
            }

            if (string.IsNullOrWhiteSpace(dto.SubjectName))
            {
                return ServiceResult<Subject>.Fail(ErrorCodes.SubjectNameRequired, "Tên môn học không được để trống.");
            }

            var cleanCode = dto.SubjectCode.Trim().ToUpper();
            var isDuplicate = await _subjectRepository.ExistsByCodeAsync(cleanCode, dto.Id);
            if (isDuplicate)
            {
                return ServiceResult<Subject>.Fail(ErrorCodes.SubjectCodeDuplicate, $"Mã môn học '{cleanCode}' đã được sử dụng bởi môn học khác.");
            }

            var evalType = dto.EvaluationType == "PassFail" ? "PassFail" : "Score";

            if (subject.EvaluationType != evalType)
            {
                var hasScores = await _subjectRepository.HasScoresOrResultsAsync(subject.Id);
                if (hasScores)
                {
                    return ServiceResult<Subject>.Fail(ErrorCodes.ValidationFailed, "Môn học này đã phát sinh điểm số học sinh, không thể thay đổi hình thức đánh giá để bảo toàn dữ liệu học bạ.");
                }
            }

            subject.SubjectCode = cleanCode;
            subject.SubjectName = dto.SubjectName.Trim();
            subject.EvaluationType = evalType;
            subject.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
            subject.IsActive = dto.IsActive;

            await _subjectRepository.UpdateAsync(subject);
            await _subjectRepository.SaveChangesAsync();

            _logger.LogInformation("Đã cập nhật môn học Id: {Id} ({Code})", subject.Id, subject.SubjectCode);
            return ServiceResult<Subject>.Ok(subject, "Cập nhật môn học thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật môn học Id: {Id}", dto.Id);
            return ServiceResult<Subject>.Fail(ErrorCodes.DatabaseError, "Đã xảy ra lỗi khi cập nhật môn học.");
        }
    }

    public async Task<ServiceResult<bool>> ToggleSubjectStatusAsync(int id)
    {
        try
        {
            var subject = await _subjectRepository.GetByIdAsync(id);
            if (subject == null)
            {
                return ServiceResult<bool>.Fail(ErrorCodes.SubjectNotFound, "Không tìm thấy môn học.");
            }

            subject.IsActive = !subject.IsActive;
            await _subjectRepository.UpdateAsync(subject);
            await _subjectRepository.SaveChangesAsync();

            var statusStr = subject.IsActive ? "Kích hoạt" : "Vô hiệu hóa";
            _logger.LogInformation("{Action} môn học Id: {Id} ({Code})", statusStr, subject.Id, subject.SubjectCode);

            return ServiceResult<bool>.Ok(subject.IsActive, $"{statusStr} môn học '{subject.SubjectName}' thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi thay đổi trạng thái môn học Id: {Id}", id);
            return ServiceResult<bool>.Fail(ErrorCodes.DatabaseError, "Đã xảy ra lỗi khi thay đổi trạng thái môn học.");
        }
    }

    public async Task<ServiceResult<bool>> DeleteSubjectAsync(int id)
    {
        try
        {
            var subject = await _subjectRepository.GetByIdWithDetailsAsync(id);
            if (subject == null)
            {
                return ServiceResult<bool>.Fail(ErrorCodes.SubjectNotFound, "Không tìm thấy môn học.");
            }

            if (subject.TeachingAssignments.Any() || subject.SemesterSubjectResults.Any())
            {
                return ServiceResult<bool>.Fail(
                    ErrorCodes.SubjectHasAssignments, 
                    "Không thể xóa môn học này vì đã có dữ liệu phân công giảng dạy hoặc điểm số liên quan. Bạn vui lòng sử dụng chức năng 'Vô hiệu hóa' thay vì xóa.");
            }

            await _subjectRepository.DeleteAsync(subject);
            await _subjectRepository.SaveChangesAsync();

            _logger.LogInformation("Đã xóa môn học Id: {Id} ({Code})", id, subject.SubjectCode);
            return ServiceResult<bool>.Ok(true, "Xóa môn học thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xóa môn học Id: {Id}", id);
            return ServiceResult<bool>.Fail(ErrorCodes.DatabaseError, "Đã xảy ra lỗi khi xóa môn học.");
        }
    }

    public async Task<ServiceResult<IEnumerable<Class>>> GetAvailableClassesAsync()
    {
        try
        {
            var classes = await _subjectRepository.GetAllClassesAsync();
            return ServiceResult<IEnumerable<Class>>.Ok(classes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách lớp học.");
            return ServiceResult<IEnumerable<Class>>.Fail(ErrorCodes.DatabaseError, "Không thể tải danh sách lớp học.");
        }
    }

    public async Task<ServiceResult<IEnumerable<Semester>>> GetAvailableSemestersAsync()
    {
        try
        {
            var semesters = await _subjectRepository.GetAllSemestersAsync();
            return ServiceResult<IEnumerable<Semester>>.Ok(semesters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách học kỳ.");
            return ServiceResult<IEnumerable<Semester>>.Fail(ErrorCodes.DatabaseError, "Không thể tải danh sách học kỳ.");
        }
    }

    public async Task<ServiceResult<bool>> AssignTeacherAsync(AssignTeacherDto dto)
    {
        try
        {
            var subject = await _subjectRepository.GetByIdAsync(dto.SubjectId);
            if (subject == null)
            {
                return ServiceResult<bool>.Fail(ErrorCodes.SubjectNotFound, "Môn học không tồn tại.");
            }

            if (!subject.IsActive)
            {
                return ServiceResult<bool>.Fail(ErrorCodes.ValidationFailed, "Môn học đang bị vô hiệu hóa, không thể phân công.");
            }

            var hasDuplicate = await _subjectRepository.HasAssignmentAsync(dto.SubjectId, dto.ClassId, dto.SemesterId);
            if (hasDuplicate)
            {
                return ServiceResult<bool>.Fail(ErrorCodes.ValidationFailed, "Lớp học này đã có giáo viên phụ trách môn học trong học kỳ đã chọn.");
            }

            var assignment = new TeachingAssignment
            {
                SubjectId = dto.SubjectId,
                TeacherId = dto.TeacherId,
                ClassId = dto.ClassId,
                SemesterId = dto.SemesterId,
                Status = string.IsNullOrWhiteSpace(dto.Status) ? "Active" : dto.Status
            };

            await _subjectRepository.AddAssignmentAsync(assignment);
            await _subjectRepository.SaveChangesAsync();

            _logger.LogInformation("Đã phân công giáo viên Id: {TeacherId} dạy môn {SubjectId} cho lớp {ClassId}", dto.TeacherId, dto.SubjectId, dto.ClassId);
            return ServiceResult<bool>.Ok(true, "Phân công giáo viên giảng dạy thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi phân công giáo viên.");
            return ServiceResult<bool>.Fail(ErrorCodes.DatabaseError, "Đã xảy ra lỗi khi lưu thông tin phân công.");
        }
    }

    public async Task<ServiceResult<bool>> UnassignTeacherAsync(int assignmentId)
    {
        try
        {
            var assignment = await _subjectRepository.GetAssignmentByIdAsync(assignmentId);
            if (assignment == null)
            {
                return ServiceResult<bool>.Fail(ErrorCodes.ValidationFailed, "Không tìm thấy thông tin phân công giảng dạy.");
            }

            if (assignment.Assessments != null && assignment.Assessments.Any())
            {
                return ServiceResult<bool>.Fail(ErrorCodes.ValidationFailed, "Không thể hủy phân công này vì đã phát sinh dữ liệu bảng điểm đánh giá của học sinh.");
            }

            await _subjectRepository.DeleteAssignmentAsync(assignment);
            await _subjectRepository.SaveChangesAsync();

            _logger.LogInformation("Đã hủy phân công Id: {AssignmentId}", assignmentId);
            return ServiceResult<bool>.Ok(true, "Đã hủy phân công giảng dạy thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi hủy phân công giảng dạy Id: {Id}", assignmentId);
            return ServiceResult<bool>.Fail(ErrorCodes.DatabaseError, "Đã xảy ra lỗi khi hủy phân công.");
        }
    }

    public async Task<ServiceResult<bool>> CheckSubjectHasScoresAsync(int id)
    {
        try
        {
            var hasScores = await _subjectRepository.HasScoresOrResultsAsync(id);
            return ServiceResult<bool>.Ok(hasScores);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi kiểm tra dữ liệu điểm của môn học Id: {Id}", id);
            return ServiceResult<bool>.Ok(false);
        }
    }
}
