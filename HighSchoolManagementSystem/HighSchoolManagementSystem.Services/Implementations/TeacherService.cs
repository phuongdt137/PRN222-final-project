using Microsoft.Extensions.Logging;
using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Repositories.Interfaces;
using HighSchoolManagementSystem.Services.Common;
using HighSchoolManagementSystem.Services.DTOs;
using HighSchoolManagementSystem.Services.Interfaces;
using HighSchoolManagementSystem.Services.Security;

namespace HighSchoolManagementSystem.Services.Implementations;

public class TeacherService : ITeacherService
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<TeacherService> _logger;

    public TeacherService(
        ITeacherRepository teacherRepository,
        IPasswordHasher passwordHasher,
        ILogger<TeacherService> logger)
    {
        _teacherRepository = teacherRepository;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<ServiceResult<IEnumerable<Teacher>>> GetAllTeachersAsync(string? searchTerm = null, string? specialization = null, string? status = null)
    {
        try
        {
            var teachers = await _teacherRepository.GetAllAsync(searchTerm, specialization, status);
            return ServiceResult<IEnumerable<Teacher>>.Ok(teachers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách giáo viên.");
            return ServiceResult<IEnumerable<Teacher>>.Fail(ErrorCodes.DatabaseError, "Không thể tải danh sách giáo viên.");
        }
    }

    public async Task<ServiceResult<Teacher>> GetTeacherByIdAsync(int id)
    {
        try
        {
            var teacher = await _teacherRepository.GetByIdAsync(id);
            if (teacher == null)
            {
                return ServiceResult<Teacher>.Fail(ErrorCodes.TeacherNotFound, "Không tìm thấy thông tin giáo viên.");
            }

            return ServiceResult<Teacher>.Ok(teacher);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tìm giáo viên Id: {Id}", id);
            return ServiceResult<Teacher>.Fail(ErrorCodes.DatabaseError, "Lỗi hệ thống khi tìm thông tin giáo viên.");
        }
    }

    public async Task<ServiceResult<Teacher>> GetTeacherDetailsAsync(int id)
    {
        try
        {
            var teacher = await _teacherRepository.GetByIdWithDetailsAsync(id);
            if (teacher == null)
            {
                return ServiceResult<Teacher>.Fail(ErrorCodes.TeacherNotFound, "Không tìm thấy chi tiết giáo viên.");
            }

            return ServiceResult<Teacher>.Ok(teacher);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy chi tiết giáo viên Id: {Id}", id);
            return ServiceResult<Teacher>.Fail(ErrorCodes.DatabaseError, "Lỗi hệ thống khi tải chi tiết giáo viên.");
        }
    }

    public async Task<ServiceResult<Teacher>> CreateTeacherAsync(CreateTeacherDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.TeacherCode))
            {
                return ServiceResult<Teacher>.Fail(ErrorCodes.ValidationFailed, "Mã giáo viên không được để trống.");
            }

            if (string.IsNullOrWhiteSpace(dto.FullName))
            {
                return ServiceResult<Teacher>.Fail(ErrorCodes.ValidationFailed, "Họ và tên giáo viên không được để trống.");
            }

            if (string.IsNullOrWhiteSpace(dto.Username))
            {
                return ServiceResult<Teacher>.Fail(ErrorCodes.ValidationFailed, "Tên đăng nhập không được để trống.");
            }

            if (string.IsNullOrWhiteSpace(dto.Email))
            {
                return ServiceResult<Teacher>.Fail(ErrorCodes.ValidationFailed, "Email không được để trống.");
            }

            var cleanCode = dto.TeacherCode.Trim().ToUpper();
            if (await _teacherRepository.ExistsByCodeAsync(cleanCode))
            {
                return ServiceResult<Teacher>.Fail(ErrorCodes.TeacherCodeDuplicate, $"Mã giáo viên '{cleanCode}' đã tồn tại.");
            }

            var cleanUsername = dto.Username.Trim().ToLower();
            if (await _teacherRepository.ExistsByUsernameAsync(cleanUsername))
            {
                return ServiceResult<Teacher>.Fail(ErrorCodes.TeacherUsernameDuplicate, $"Tên đăng nhập '{cleanUsername}' đã được sử dụng.");
            }

            var cleanEmail = dto.Email.Trim().ToLower();
            if (await _teacherRepository.ExistsByEmailAsync(cleanEmail))
            {
                return ServiceResult<Teacher>.Fail(ErrorCodes.TeacherEmailDuplicate, $"Email '{cleanEmail}' đã được sử dụng.");
            }

            var teacherRole = await _teacherRepository.GetTeacherRoleAsync();
            if (teacherRole == null)
            {
                return ServiceResult<Teacher>.Fail(ErrorCodes.SystemError, "Không tìm thấy vai trò Giáo viên trong hệ thống.");
            }

            var rawPassword = string.IsNullOrWhiteSpace(dto.Password) ? "Teacher@123" : dto.Password;
            var hashedPassword = _passwordHasher.HashPassword(rawPassword);

            var user = new User
            {
                Username = cleanUsername,
                FullName = dto.FullName.Trim(),
                Email = cleanEmail,
                PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber.Trim(),
                Gender = dto.Gender,
                DateOfBirth = dto.DateOfBirth,
                PasswordHash = hashedPassword,
                RoleId = teacherRole.RoleId,
                IsActive = dto.Status == "Active",
                CreateAt = DateTime.UtcNow
            };

            var teacher = new Teacher
            {
                User = user,
                TeacherCode = cleanCode,
                Specialization = string.IsNullOrWhiteSpace(dto.Specialization) ? null : dto.Specialization.Trim(),
                Qualification = string.IsNullOrWhiteSpace(dto.Qualification) ? null : dto.Qualification.Trim(),
                HireDate = dto.HireDate,
                MaxPeriodsPerWeek = dto.MaxPeriodsPerWeek ?? 20,
                Status = dto.Status == "Inactive" ? "Inactive" : "Active"
            };

            await _teacherRepository.AddAsync(teacher);
            await _teacherRepository.SaveChangesAsync();

            _logger.LogInformation("Đã thêm mới giáo viên {Code} - {Name}", teacher.TeacherCode, user.FullName);
            return ServiceResult<Teacher>.Ok(teacher, "Thêm mới giáo viên thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi thêm mới giáo viên.");
            return ServiceResult<Teacher>.Fail(ErrorCodes.DatabaseError, "Đã xảy ra lỗi khi tạo hồ sơ giáo viên.");
        }
    }

    public async Task<ServiceResult<Teacher>> UpdateTeacherAsync(UpdateTeacherDto dto)
    {
        try
        {
            var teacher = await _teacherRepository.GetByIdWithDetailsAsync(dto.Id);
            if (teacher == null)
            {
                return ServiceResult<Teacher>.Fail(ErrorCodes.TeacherNotFound, "Hồ sơ giáo viên không tồn tại.");
            }

            if (string.IsNullOrWhiteSpace(dto.TeacherCode))
            {
                return ServiceResult<Teacher>.Fail(ErrorCodes.ValidationFailed, "Mã giáo viên không được để trống.");
            }

            if (string.IsNullOrWhiteSpace(dto.FullName))
            {
                return ServiceResult<Teacher>.Fail(ErrorCodes.ValidationFailed, "Họ và tên giáo viên không được để trống.");
            }

            if (string.IsNullOrWhiteSpace(dto.Email))
            {
                return ServiceResult<Teacher>.Fail(ErrorCodes.ValidationFailed, "Email không được để trống.");
            }

            var cleanCode = dto.TeacherCode.Trim().ToUpper();
            if (await _teacherRepository.ExistsByCodeAsync(cleanCode, dto.Id))
            {
                return ServiceResult<Teacher>.Fail(ErrorCodes.TeacherCodeDuplicate, $"Mã giáo viên '{cleanCode}' đã được sử dụng bởi giáo viên khác.");
            }

            var cleanEmail = dto.Email.Trim().ToLower();
            if (await _teacherRepository.ExistsByEmailAsync(cleanEmail, teacher.UserId))
            {
                return ServiceResult<Teacher>.Fail(ErrorCodes.TeacherEmailDuplicate, $"Email '{cleanEmail}' đã được sử dụng bởi tài khoản khác.");
            }

            teacher.TeacherCode = cleanCode;
            teacher.Specialization = string.IsNullOrWhiteSpace(dto.Specialization) ? null : dto.Specialization.Trim();
            teacher.Qualification = string.IsNullOrWhiteSpace(dto.Qualification) ? null : dto.Qualification.Trim();
            teacher.HireDate = dto.HireDate;
            teacher.MaxPeriodsPerWeek = dto.MaxPeriodsPerWeek;
            teacher.Status = dto.Status == "Inactive" ? "Inactive" : "Active";

            teacher.User.FullName = dto.FullName.Trim();
            teacher.User.Email = cleanEmail;
            teacher.User.PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber.Trim();
            teacher.User.Gender = dto.Gender;
            teacher.User.DateOfBirth = dto.DateOfBirth;
            teacher.User.IsActive = teacher.Status == "Active";

            await _teacherRepository.UpdateAsync(teacher);
            await _teacherRepository.SaveChangesAsync();

            _logger.LogInformation("Đã cập nhật giáo viên Id: {Id} ({Code})", teacher.Id, teacher.TeacherCode);
            return ServiceResult<Teacher>.Ok(teacher, "Cập nhật thông tin giáo viên thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật giáo viên Id: {Id}", dto.Id);
            return ServiceResult<Teacher>.Fail(ErrorCodes.DatabaseError, "Đã xảy ra lỗi khi cập nhật thông tin giáo viên.");
        }
    }

    public async Task<ServiceResult<bool>> ToggleTeacherStatusAsync(int id)
    {
        try
        {
            var teacher = await _teacherRepository.GetByIdWithDetailsAsync(id);
            if (teacher == null)
            {
                return ServiceResult<bool>.Fail(ErrorCodes.TeacherNotFound, "Không tìm thấy giáo viên.");
            }

            if (teacher.Status == "Active")
            {
                teacher.Status = "Inactive";
                teacher.User.IsActive = false;
            }
            else
            {
                teacher.Status = "Active";
                teacher.User.IsActive = true;
            }

            await _teacherRepository.UpdateAsync(teacher);
            await _teacherRepository.SaveChangesAsync();

            var statusStr = teacher.Status == "Active" ? "Kích hoạt" : "Tạm dừng hoạt động";
            _logger.LogInformation("{Action} giáo viên Id: {Id} ({Code})", statusStr, teacher.Id, teacher.TeacherCode);
            return ServiceResult<bool>.Ok(teacher.Status == "Active", $"{statusStr} giáo viên '{teacher.User.FullName}' thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi chuyển trạng thái giáo viên Id: {Id}", id);
            return ServiceResult<bool>.Fail(ErrorCodes.DatabaseError, "Đã xảy ra lỗi khi thay đổi trạng thái giáo viên.");
        }
    }

    public async Task<ServiceResult<IEnumerable<string>>> GetSpecializationsAsync()
    {
        try
        {
            var standard = new List<string>
            {
                "Toán Học",
                "Vật Lý",
                "Hóa Học",
                "Sinh Học",
                "Ngữ Văn",
                "Lịch Sử",
                "Địa Lý",
                "Tiếng Anh",
                "Tin Học",
                "Giáo Dục Công Dân",
                "Giáo Dục Thể Chất",
                "Giáo Dục Quốc Phòng - An Ninh",
                "Công Nghệ",
                "Âm Nhạc & Nghệ Thuật"
            };

            var teachers = await _teacherRepository.GetAllAsync();
            var existing = teachers
                .Where(t => !string.IsNullOrWhiteSpace(t.Specialization))
                .Select(t => t.Specialization!.Trim())
                .Distinct();

            var combined = standard.Union(existing).OrderBy(s => s).ToList();
            return ServiceResult<IEnumerable<string>>.Ok(combined);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách tổ bộ môn.");
            return ServiceResult<IEnumerable<string>>.Fail(ErrorCodes.DatabaseError, "Không thể tải danh sách bộ môn.");
        }
    }
}
