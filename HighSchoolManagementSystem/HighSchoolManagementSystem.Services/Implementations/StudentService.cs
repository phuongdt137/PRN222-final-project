using System.ComponentModel.DataAnnotations;
using HighSchoolManagementSystem.Models;
using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Repositories.Interfaces;
using HighSchoolManagementSystem.Services.Common;
using HighSchoolManagementSystem.Services.DTOs;
using HighSchoolManagementSystem.Services.Interfaces;
using HighSchoolManagementSystem.Services.Security;
using Microsoft.Extensions.Logging;

namespace HighSchoolManagementSystem.Services.Implementations;

public class StudentService(IStudentRepository students, IUserRepository users, IPasswordHasher passwordHasher,
    ILogger<StudentService> logger) : IStudentService
{
    public const string NotFound = "STUDENT_NOT_FOUND";
    public const string DuplicateCode = "STUDENT_CODE_DUPLICATE";

    public Task<ServiceResult<PagedResult<Student>>> GetStudentsAsync(string? search = null, string? status = null, int? classId = null, bool unassignedOnly = false, int page = 1, int pageSize = 25) =>
        ExecuteAsync(async () =>
        {
            if (search?.Length > 100 || (!string.IsNullOrEmpty(status) && status is not ("Active" or "Inactive" or "Graduated"))
                || classId <= 0 || (classId.HasValue && unassignedOnly) || page <= 0 || pageSize is < 1 or > 100)
                return ServiceResult<PagedResult<Student>>.Fail(ErrorCodes.ValidationFailed, "Bộ lọc không hợp lệ: tìm kiếm tối đa 100 ký tự, trang phải là số dương và không chọn đồng thời lớp học với chưa phân lớp.");
            if (classId.HasValue && !(await students.GetCurrentClassesAsync()).Any(c => c.Id == classId))
                return ServiceResult<PagedResult<Student>>.Fail(ErrorCodes.ValidationFailed, "Lớp được chọn không thuộc năm học hiện hành.");
            return ServiceResult<PagedResult<Student>>.Ok(await students.GetAllAsync(search, status, classId, unassignedOnly, page, pageSize));
        });

    public Task<ServiceResult<Student>> GetStudentAsync(int id) => ExecuteAsync(async () =>
    {
        var student = await students.GetByIdAsync(id);
        return student == null ? ServiceResult<Student>.Fail(NotFound, "Không tìm thấy học sinh.") : ServiceResult<Student>.Ok(student);
    });

    public Task<ServiceResult<Student>> GetAcademicHistoryAsync(int id) => ExecuteAsync(async () =>
    {
        var student = await students.GetAcademicHistoryAsync(id);
        return student == null ? ServiceResult<Student>.Fail(NotFound, "Không tìm thấy học sinh.") : ServiceResult<Student>.Ok(student);
    });

    public Task<ServiceResult<Student>> CreateStudentAsync(CreateStudentDto dto) => ExecuteAsync(async () =>
    {
        var error = Validate(dto);
        if (error != null) return ServiceResult<Student>.Fail(ErrorCodes.ValidationFailed, error);
        if (await students.ExistsByCodeAsync(dto.StudentCode.Trim()))
            return ServiceResult<Student>.Fail(DuplicateCode, "Mã học sinh đã được sử dụng.");
        var username = dto.Username.Trim().ToLowerInvariant();
        if (await users.ExistsByUsernameAsync(username))
            return ServiceResult<Student>.Fail(ErrorCodes.AuthUsernameDuplicate, "Tên đăng nhập đã được sử dụng.");
        if (await users.ExistsByEmailAsync(dto.Email.Trim().ToLowerInvariant()))
            return ServiceResult<Student>.Fail(ErrorCodes.UserEmailDuplicate, "Email đã được sử dụng.");
        var role = await students.GetStudentRoleAsync();
        if (role == null) return ServiceResult<Student>.Fail(ErrorCodes.UserRoleInvalid, "Chưa cấu hình vai trò Student.");
        var student = new Student
        {
            User = new User { Username = username, PasswordHash = passwordHasher.HashPassword(dto.Password), RoleId = role.RoleId,
                Role = role, IsActive = true, CreateAt = DateTime.UtcNow }
        };
        Apply(student, dto);
        await students.AddAsync(student);
        // One SaveChanges transaction creates both the account and its student profile.
        await students.SaveChangesAsync();
        return ServiceResult<Student>.Ok(student, "Đã tạo hồ sơ và tài khoản học sinh.");
    });

    public Task<ServiceResult<Student>> UpdateStudentAsync(int id, UpdateStudentDto dto) => ExecuteAsync(async () =>
    {
        var error = Validate(dto);
        if (error != null) return ServiceResult<Student>.Fail(ErrorCodes.ValidationFailed, error);
        var student = await students.GetByIdAsync(id);
        if (student == null) return ServiceResult<Student>.Fail(NotFound, "Không tìm thấy học sinh.");
        if (await students.ExistsByCodeAsync(dto.StudentCode.Trim(), id))
            return ServiceResult<Student>.Fail(DuplicateCode, "Mã học sinh đã được sử dụng.");
        if (await users.ExistsByEmailAsync(dto.Email.Trim().ToLowerInvariant(), student.UserId))
            return ServiceResult<Student>.Fail(ErrorCodes.UserEmailDuplicate, "Email đã được sử dụng.");
        Apply(student, dto);
        await students.SaveChangesAsync();
        return ServiceResult<Student>.Ok(student, "Đã cập nhật thông tin học sinh.");
    });

    public Task<ServiceResult<List<Class>>> GetCurrentClassesAsync() =>
        ExecuteAsync(async () => ServiceResult<List<Class>>.Ok(await students.GetCurrentClassesAsync()));
    public Task<ServiceResult<AcademicYear?>> GetCurrentYearAsync() =>
        ExecuteAsync(async () => ServiceResult<AcademicYear?>.Ok(await students.GetCurrentYearAsync()));

    public async Task<ServiceResult> AssignClassAsync(int studentId, int classId)
    {
        if (studentId <= 0 || classId <= 0) return ServiceResult.Fail(ErrorCodes.ValidationFailed, "Vui lòng chọn học sinh và lớp hợp lệ.");
        try
        {
            return (await students.AssignClassAsync(studentId, classId)) switch
            {
                StudentAssignmentResult.Success => ServiceResult.Ok("Đã phân lớp cho học sinh."),
                StudentAssignmentResult.StudentNotFound => ServiceResult.Fail(NotFound, "Không tìm thấy học sinh."),
                StudentAssignmentResult.StudentInactive => ServiceResult.Fail(ErrorCodes.ValidationFailed, "Chỉ phân lớp cho học sinh có trạng thái đang học."),
                StudentAssignmentResult.ClassNotFound => ServiceResult.Fail(ErrorCodes.ClassNotFound, "Lớp học không tồn tại."),
                StudentAssignmentResult.YearNotCurrent => ServiceResult.Fail(ErrorCodes.ValidationFailed, "Chỉ phân lớp trong năm học hiện hành. Vui lòng tải lại danh sách."),
                _ => ServiceResult.Fail(ErrorCodes.ValidationFailed, "Học sinh đã có phân lớp trong năm học này. Chức năng phân lớp không thay đổi lớp đã có.")
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi phân lớp học sinh {StudentId} vào lớp {ClassId}", studentId, classId);
            return ServiceResult.Fail(ErrorCodes.DatabaseError, "Không thể phân lớp. Vui lòng tải lại trang và thử lại.");
        }
    }

    public async Task<ServiceResult> TransferClassAsync(int studentId, TransferStudentClassDto dto)
    {
        if (dto == null || studentId <= 0 || dto.ClassId <= 0 || dto.MembershipId <= 0)
            return ServiceResult.Fail(ErrorCodes.ValidationFailed, "Vui lòng chọn học sinh, phân lớp hiện tại và lớp đích hợp lệ.");
        try
        {
            return (await students.TransferClassAsync(studentId, dto.MembershipId, dto.ClassId)) switch
            {
                StudentTransferResult.Success => ServiceResult.Ok("Đã chuyển lớp và lưu lịch sử phân lớp."),
                StudentTransferResult.StudentNotFound => ServiceResult.Fail(NotFound, "Không tìm thấy học sinh."),
                StudentTransferResult.StudentInactive => ServiceResult.Fail(ErrorCodes.ValidationFailed, "Chỉ chuyển lớp cho học sinh đang học."),
                StudentTransferResult.ClassNotFound => ServiceResult.Fail(ErrorCodes.ClassNotFound, "Lớp đích không tồn tại."),
                StudentTransferResult.YearNotCurrent => ServiceResult.Fail(ErrorCodes.ValidationFailed, "Chỉ chuyển lớp trong năm học hiện hành."),
                StudentTransferResult.SameClass => ServiceResult.Fail(ErrorCodes.ValidationFailed, "Lớp đích phải khác lớp hiện tại."),
                StudentTransferResult.DifferentGrade => ServiceResult.Fail(ErrorCodes.ValidationFailed, "Lớp đích phải cùng khối với lớp hiện tại."),
                _ => ServiceResult.Fail(ErrorCodes.ValidationFailed, "Phân lớp đã thay đổi hoặc không còn hợp lệ. Vui lòng tải lại trang trước khi chuyển lớp.")
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi chuyển lớp học sinh {StudentId}", studentId);
            return ServiceResult.Fail(ErrorCodes.DatabaseError, "Không thể chuyển lớp. Vui lòng tải lại trang và thử lại.");
        }
    }

    private async Task<ServiceResult<T>> ExecuteAsync<T>(Func<Task<ServiceResult<T>>> action)
    {
        try { return await action(); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi xử lý thông tin học sinh");
            return ServiceResult<T>.Fail(ErrorCodes.DatabaseError, "Không thể xử lý dữ liệu học sinh. Vui lòng tải lại trang và thử lại.");
        }
    }

    private static string? Validate(UpdateStudentDto? dto)
    {
        if (dto == null) return "Dữ liệu học sinh không hợp lệ.";
        var errors = new List<ValidationResult>();
        Validator.TryValidateObject(dto, new ValidationContext(dto), errors, true);
        if (dto is CreateStudentDto create && System.Text.Encoding.UTF8.GetByteCount(create.Password ?? "") > 72)
            return "Mật khẩu không được vượt quá 72 byte UTF-8.";
        return errors.Count == 0 ? null : string.Join(" ", errors.Select(e => e.ErrorMessage));
    }

    private static void Apply(Student student, UpdateStudentDto dto)
    {
        student.StudentCode = dto.StudentCode.Trim().ToUpperInvariant();
        student.User.FullName = dto.FullName.Trim();
        student.User.Email = dto.Email.Trim().ToLowerInvariant();
        student.User.PhoneNumber = Clean(dto.PhoneNumber);
        student.User.Gender = Clean(dto.Gender);
        student.User.DateOfBirth = dto.DateOfBirth?.Date;
        student.AdmissionDate = dto.AdmissionDate?.Date;
        student.Address = Clean(dto.Address);
        student.Status = dto.Status;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
