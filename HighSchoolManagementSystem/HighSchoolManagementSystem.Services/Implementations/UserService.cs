using System.Text.RegularExpressions;
using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Repositories.Interfaces;
using HighSchoolManagementSystem.Services.Common;
using HighSchoolManagementSystem.Services.DTOs;
using HighSchoolManagementSystem.Services.Interfaces;
using HighSchoolManagementSystem.Services.Security;

namespace HighSchoolManagementSystem.Services.Implementations;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public UserService(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<ServiceResult<List<User>>> GetUsersAsync(string? search = null, int? roleId = null, bool? isActive = null)
    {
        return ServiceResult<List<User>>.Ok(await _userRepository.GetAllAsync(search, roleId, isActive));
    }

    public async Task<ServiceResult<List<Role>>> GetRolesAsync()
    {
        return ServiceResult<List<Role>>.Ok(await _userRepository.GetRolesAsync());
    }

    public async Task<ServiceResult<User>> GetUserByIdAsync(int userId)
    {
        if (userId <= 0)
        {
            return ServiceResult<User>.Fail(ErrorCodes.UserNotFound, "Định danh người dùng không hợp lệ.");
        }

        var user = await _userRepository.GetByIdAsync(userId);
        return user == null
            ? ServiceResult<User>.Fail(ErrorCodes.UserNotFound, "Không tìm thấy người dùng.")
            : ServiceResult<User>.Ok(user);
    }

    public async Task<ServiceResult<User>> CreateUserAsync(CreateUserDto dto)
    {
        if (dto == null)
        {
            return ServiceResult<User>.Fail(ErrorCodes.ValidationFailed, "Dữ liệu người dùng không hợp lệ.");
        }

        var username = dto.Username.Trim().ToLower();
        var email = dto.Email.Trim().ToLower();
        var validation = ValidateUserFields(dto.FullName, dto.PhoneNumber, dto.Gender, dto.DateOfBirth);
        if (validation != null)
        {
            return ServiceResult<User>.Fail(ErrorCodes.ValidationFailed, validation);
        }

        if (await _userRepository.ExistsByUsernameAsync(username))
        {
            return ServiceResult<User>.Fail(ErrorCodes.AuthUsernameDuplicate, "Tên đăng nhập đã được sử dụng.");
        }

        if (await _userRepository.ExistsByEmailAsync(email))
        {
            return ServiceResult<User>.Fail(ErrorCodes.UserEmailDuplicate, "Email đã được sử dụng.");
        }

        var role = await _userRepository.GetRoleByIdAsync(dto.RoleId);
        if (role == null)
        {
            return ServiceResult<User>.Fail(ErrorCodes.UserRoleInvalid, "Vai trò người dùng không tồn tại.");
        }

        var user = new User
        {
            Username = username,
            Email = email,
            FullName = dto.FullName.Trim(),
            PasswordHash = _passwordHasher.HashPassword(dto.Password),
            PhoneNumber = NormalizePhone(dto.PhoneNumber),
            Gender = NormalizeGender(dto.Gender),
            DateOfBirth = dto.DateOfBirth?.Date,
            RoleId = role.RoleId,
            IsActive = dto.IsActive,
            CreateAt = DateTime.UtcNow
        };

        if (string.Equals(role.RoleName, "Student", StringComparison.OrdinalIgnoreCase))
        {
            var studentCode = await _userRepository.GenerateNextStudentCodeAsync();
            user.Student = new Student
            {
                StudentCode = studentCode,
                AdmissionDate = dto.AdmissionDate?.Date ?? DateTime.Today,
                Address = dto.Address?.Trim(),
                Status = "Active"
            };
        }
        else if (string.Equals(role.RoleName, "Teacher", StringComparison.OrdinalIgnoreCase))
        {
            var teacherCode = await _userRepository.GenerateNextTeacherCodeAsync();
            user.Teacher = new Teacher
            {
                TeacherCode = teacherCode,
                Specialization = dto.Specialization?.Trim(),
                Qualification = dto.Qualification?.Trim(),
                MaxPeriodsPerWeek = dto.MaxPeriodsPerWeek is > 0 ? dto.MaxPeriodsPerWeek : 20,
                HireDate = dto.HireDate?.Date,
                Status = "Active"
            };
        }
        else if (string.Equals(role.RoleName, "Parent", StringComparison.OrdinalIgnoreCase))
        {
            user.Parent = new Parent
            {
                Occupation = dto.Occupation?.Trim(),
                EmergencyContact = dto.EmergencyContact?.Trim()
            };
        }

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();
        user.Role = role;
        return ServiceResult<User>.Ok(user, "Tạo tài khoản và hồ sơ thành công.");
    }

    public async Task<ServiceResult<User>> UpdateUserAccountAsync(int userId, UpdateUserAccountDto dto)
    {
        if (dto == null)
        {
            return ServiceResult<User>.Fail(ErrorCodes.ValidationFailed, "Dữ liệu người dùng không hợp lệ.");
        }

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return ServiceResult<User>.Fail(ErrorCodes.UserNotFound, "Không tìm thấy người dùng.");
        }

        var validation = ValidateUserFields(dto.FullName, dto.PhoneNumber, dto.Gender, dto.DateOfBirth);
        if (validation != null)
        {
            return ServiceResult<User>.Fail(ErrorCodes.ValidationFailed, validation);
        }

        var email = dto.Email.Trim().ToLower();
        if (await _userRepository.ExistsByEmailAsync(email, userId))
        {
            return ServiceResult<User>.Fail(ErrorCodes.UserEmailDuplicate, "Email đã được sử dụng.");
        }

        var role = await _userRepository.GetRoleByIdAsync(dto.RoleId);
        if (role == null)
        {
            return ServiceResult<User>.Fail(ErrorCodes.UserRoleInvalid, "Vai trò người dùng không tồn tại.");
        }

        user.Email = email;
        user.FullName = dto.FullName.Trim();
        user.PhoneNumber = NormalizePhone(dto.PhoneNumber);
        user.Gender = NormalizeGender(dto.Gender);
        user.DateOfBirth = dto.DateOfBirth?.Date;
        user.RoleId = role.RoleId;

        await _userRepository.UpdateAsync(user);
        user.Role = role;
        return ServiceResult<User>.Ok(user, "Cập nhật tài khoản thành công.");
    }

    public async Task<ServiceResult<bool>> SetUserActiveAsync(int userId, bool isActive)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return ServiceResult<bool>.Fail(ErrorCodes.UserNotFound, "Không tìm thấy người dùng.");
        }

        user.IsActive = isActive;
        await _userRepository.UpdateAsync(user);
        return ServiceResult<bool>.Ok(true, isActive ? "Đã kích hoạt tài khoản." : "Đã vô hiệu hóa tài khoản.");
    }

    public async Task<ServiceResult<bool>> ResetUserPasswordAsync(int userId, ResetUserPasswordDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 6)
        {
            return ServiceResult<bool>.Fail(ErrorCodes.AuthPasswordTooWeak, "Mật khẩu mới phải có ít nhất 6 ký tự.");
        }

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return ServiceResult<bool>.Fail(ErrorCodes.UserNotFound, "Không tìm thấy người dùng.");
        }

        user.PasswordHash = _passwordHasher.HashPassword(dto.NewPassword);
        await _userRepository.UpdateAsync(user);
        return ServiceResult<bool>.Ok(true, "Đã đặt lại mật khẩu cho tài khoản.");
    }

    private static string? ValidateUserFields(string? fullName, string? phoneNumber, string? gender, DateTime? dateOfBirth)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return "Họ và tên không được để trống.";
        }

        var cleanPhone = NormalizePhone(phoneNumber);
        if (!string.IsNullOrWhiteSpace(cleanPhone) && !Regex.IsMatch(cleanPhone, @"^0[1-9]\d{8}$"))
        {
            return "Số điện thoại không hợp lệ (phải gồm 10 chữ số và bắt đầu bằng số 0).";
        }

        if (!string.IsNullOrWhiteSpace(gender)
            && !new[] { "Nam", "Nữ", "Khác" }.Any(x => string.Equals(x, gender.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            return "Giới tính không hợp lệ.";
        }

        if (dateOfBirth.HasValue && (dateOfBirth.Value.Date >= DateTime.UtcNow.Date || dateOfBirth.Value.Year < 1920))
        {
            return "Ngày sinh không hợp lệ.";
        }

        return null;
    }

    private static string? NormalizePhone(string? phoneNumber)
    {
        return string.IsNullOrWhiteSpace(phoneNumber)
            ? null
            : phoneNumber.Trim().Replace(" ", "").Replace(".", "").Replace("-", "");
    }

    private static string? NormalizeGender(string? gender)
    {
        if (string.IsNullOrWhiteSpace(gender)) return null;
        return new[] { "Nam", "Nữ", "Khác" }.First(x => string.Equals(x, gender.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public async Task<ServiceResult<User>> GetUserProfileAsync(int userId)
    {
        if (userId <= 0)
        {
            return ServiceResult<User>.Fail(ErrorCodes.UserNotFound, "Định danh người dùng không hợp lệ.");
        }

        var user = await _userRepository.GetUserFullProfileByIdAsync(userId);
        if (user == null)
        {
            return ServiceResult<User>.Fail(ErrorCodes.UserNotFound, "Không tìm thấy thông tin người dùng.");
        }

        if (!user.IsActive)
        {
            return ServiceResult<User>.Fail(ErrorCodes.AuthAccountLocked, "Tài khoản của bạn đã bị khóa hoặc tạm dừng hoạt động.");
        }

        return ServiceResult<User>.Ok(user);
    }

    public async Task<ServiceResult<User>> UpdateProfileAsync(int userId, UpdateProfileDto dto)
    {
        if (userId <= 0)
        {
            return ServiceResult<User>.Fail(ErrorCodes.UserNotFound, "Định danh người dùng không hợp lệ.");
        }

        if (dto == null)
        {
            return ServiceResult<User>.Fail(ErrorCodes.ValidationFailed, "Dữ liệu cập nhật không hợp lệ.");
        }

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return ServiceResult<User>.Fail(ErrorCodes.UserNotFound, "Không tìm thấy tài khoản người dùng.");
        }

        if (!user.IsActive)
        {
            return ServiceResult<User>.Fail(ErrorCodes.AuthAccountLocked, "Tài khoản của bạn đã bị khóa.");
        }

        // 1. Kiểm tra số điện thoại (nếu có nhập)
        if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
        {
            var cleanPhone = dto.PhoneNumber.Trim().Replace(" ", "").Replace(".", "").Replace("-", "");
            // Quy chuẩn SĐT Việt Nam: 10 chữ số, bắt đầu bằng 0
            if (!Regex.IsMatch(cleanPhone, @"^0[1-9]\d{8}$"))
            {
                return ServiceResult<User>.Fail(ErrorCodes.UserPhoneInvalid, "Số điện thoại không hợp lệ (phải gồm 10 chữ số và bắt đầu bằng số 0).");
            }
            user.PhoneNumber = cleanPhone;
        }
        else
        {
            user.PhoneNumber = null;
        }

        // 2. Kiểm tra ngày sinh (nếu có nhập)
        if (dto.DateOfBirth.HasValue)
        {
            if (dto.DateOfBirth.Value.Date >= DateTime.UtcNow.Date)
            {
                return ServiceResult<User>.Fail(ErrorCodes.UserDobInvalid, "Ngày sinh không thể lớn hơn hoặc bằng ngày hiện tại.");
            }
            if (dto.DateOfBirth.Value.Year < 1920)
            {
                return ServiceResult<User>.Fail(ErrorCodes.UserDobInvalid, "Năm sinh không hợp lệ.");
            }
            user.DateOfBirth = dto.DateOfBirth.Value.Date;
        }
        else
        {
            user.DateOfBirth = null;
        }

        // 3. Kiểm tra giới tính (nếu có nhập)
        if (!string.IsNullOrWhiteSpace(dto.Gender))
        {
            var validGenders = new[] { "Nam", "Nữ", "Khác" };
            var matchedGender = validGenders.FirstOrDefault(g => string.Equals(g, dto.Gender.Trim(), StringComparison.OrdinalIgnoreCase));
            if (matchedGender == null)
            {
                return ServiceResult<User>.Fail(ErrorCodes.ValidationFailed, "Giới tính không hợp lệ (chỉ chấp nhận: Nam, Nữ, Khác).");
            }
            user.Gender = matchedGender;
        }
        else
        {
            user.Gender = null;
        }

        // 4. Cập nhật ảnh đại diện (nếu có đường dẫn mới)
        if (!string.IsNullOrWhiteSpace(dto.AvatarUrl))
        {
            user.AvatarUrl = dto.AvatarUrl.Trim();
        }

        try
        {
            await _userRepository.UpdateAsync(user);
            return ServiceResult<User>.Ok(user, "Cập nhật thông tin cá nhân thành công.");
        }
        catch (Exception ex)
        {
            return ServiceResult<User>.Fail(ErrorCodes.UserProfileUpdateFail, $"Lỗi cập nhật dữ liệu: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> ChangePasswordAsync(int userId, ChangePasswordDto dto)
    {
        if (userId <= 0)
        {
            return ServiceResult<bool>.Fail(ErrorCodes.UserNotFound, "Định danh người dùng không hợp lệ.");
        }

        if (dto == null || string.IsNullOrWhiteSpace(dto.CurrentPassword) || string.IsNullOrWhiteSpace(dto.NewPassword))
        {
            return ServiceResult<bool>.Fail(ErrorCodes.ValidationFailed, "Vui lòng nhập đầy đủ mật khẩu hiện tại và mật khẩu mới.");
        }

        if (dto.NewPassword.Length < 6)
        {
            return ServiceResult<bool>.Fail(ErrorCodes.AuthPasswordTooWeak, "Mật khẩu mới phải có ít nhất 6 ký tự.");
        }

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return ServiceResult<bool>.Fail(ErrorCodes.UserNotFound, "Không tìm thấy thông tin tài khoản người dùng.");
        }

        if (!user.IsActive)
        {
            return ServiceResult<bool>.Fail(ErrorCodes.AuthAccountLocked, "Tài khoản của bạn đã bị khóa hoặc tạm dừng hoạt động.");
        }

        // 1. Kiểm tra mật khẩu hiện tại có đúng không
        bool isCurrentPasswordValid = _passwordHasher.VerifyPassword(dto.CurrentPassword, user.PasswordHash);
        if (!isCurrentPasswordValid)
        {
            return ServiceResult<bool>.Fail(ErrorCodes.AuthInvalidCredentials, "Mật khẩu hiện tại không chính xác.");
        }

        // 2. Mật khẩu mới không được trùng mật khẩu cũ
        if (_passwordHasher.VerifyPassword(dto.NewPassword, user.PasswordHash))
        {
            return ServiceResult<bool>.Fail(ErrorCodes.ValidationFailed, "Mật khẩu mới không được trùng với mật khẩu hiện tại.");
        }

        user.PasswordHash = _passwordHasher.HashPassword(dto.NewPassword);

        try
        {
            await _userRepository.UpdateAsync(user);
            return ServiceResult<bool>.Ok(true, "Đổi mật khẩu thành công.");
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.Fail(ErrorCodes.DatabaseError, $"Lỗi lưu mật khẩu mới: {ex.Message}");
        }
    }
}
