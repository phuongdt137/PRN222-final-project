using System.Security.Cryptography;
using System.Text;
using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Repositories.Interfaces;
using HighSchoolManagementSystem.Services.Common;
using HighSchoolManagementSystem.Services.Interfaces;
using HighSchoolManagementSystem.Services.Security;

namespace HighSchoolManagementSystem.Services.Implementations;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailService _emailService;

    private const string SystemTokenSecret = "THPT_RESET_SECRET_SALT_2026_@!";
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(15);

    public AuthService(
        IUserRepository userRepository, 
        IPasswordHasher passwordHasher,
        IEmailService emailService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
    }

    public async Task<ServiceResult<User>> AuthenticateAsync(string usernameOrEmail, string password)
    {
        if (string.IsNullOrWhiteSpace(usernameOrEmail) || string.IsNullOrWhiteSpace(password))
        {
            return ServiceResult<User>.Fail(ErrorCodes.ValidationFailed, "Vui lòng nhập đầy đủ thông tin đăng nhập.");
        }

        var user = await _userRepository.GetByUsernameOrEmailAsync(usernameOrEmail);
        if (user == null)
        {
            return ServiceResult<User>.Fail(ErrorCodes.AuthInvalidCredentials, "Tên đăng nhập hoặc mật khẩu không chính xác.");
        }

        if (!user.IsActive)
        {
            return ServiceResult<User>.Fail(ErrorCodes.AuthAccountLocked, "Tài khoản của bạn đã bị khóa hoặc tạm dừng hoạt động. Vui lòng liên hệ nhà trường.");
        }

        bool isPasswordValid = _passwordHasher.VerifyPassword(password, user.PasswordHash);
        if (!isPasswordValid)
        {
            return ServiceResult<User>.Fail(ErrorCodes.AuthInvalidCredentials, "Tên đăng nhập hoặc mật khẩu không chính xác.");
        }

        return ServiceResult<User>.Ok(user, "Đăng nhập thành công.");
    }

    public async Task<ServiceResult<string>> CreatePasswordResetTokenAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return ServiceResult<string>.Fail(ErrorCodes.ValidationFailed, "Vui lòng nhập địa chỉ email.");
        }

        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null)
        {
            return ServiceResult<string>.Fail(ErrorCodes.ResourceNotFound, "Không tìm thấy tài khoản tương ứng với email này.");
        }

        if (!user.IsActive)
        {
            return ServiceResult<string>.Fail(ErrorCodes.AuthAccountLocked, "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ nhà trường.");
        }

        var expiresTicks = DateTime.UtcNow.Add(TokenLifetime).Ticks;
        var payload = $"{user.Id}:{expiresTicks}";
        var key = $"{SystemTokenSecret}_{user.PasswordHash}";
        var signature = ComputeHmac(payload, key);
        var token = Base64UrlEncode($"{payload}:{signature}");

        return ServiceResult<string>.Ok(token, "Tạo mã đặt lại mật khẩu thành công.");
    }

    public async Task<ServiceResult<User>> ValidatePasswordResetTokenAsync(string email, string token)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
        {
            return ServiceResult<User>.Fail(ErrorCodes.ValidationFailed, "Thông tin xác thực không đầy đủ.");
        }

        var decoded = Base64UrlDecode(token);
        if (string.IsNullOrWhiteSpace(decoded))
        {
            return ServiceResult<User>.Fail(ErrorCodes.AuthResetTokenInvalid, "Mã xác thực không hợp lệ.");
        }

        var parts = decoded.Split(':');
        if (parts.Length != 3)
        {
            return ServiceResult<User>.Fail(ErrorCodes.AuthResetTokenInvalid, "Mã xác thực không đúng cấu trúc.");
        }

        if (!int.TryParse(parts[0], out int userId) || !long.TryParse(parts[1], out long expiresTicks))
        {
            return ServiceResult<User>.Fail(ErrorCodes.AuthResetTokenInvalid, "Mã xác thực không hợp lệ.");
        }

        if (DateTime.UtcNow.Ticks > expiresTicks)
        {
            return ServiceResult<User>.Fail(ErrorCodes.AuthResetTokenExpired, "Liên kết đặt lại mật khẩu đã hết hạn (chỉ có hiệu lực trong 15 phút). Vui lòng yêu cầu lại.");
        }

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null || !string.Equals(user.Email, email.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return ServiceResult<User>.Fail(ErrorCodes.AuthResetTokenInvalid, "Liên kết đặt lại mật khẩu không khớp với tài khoản.");
        }

        if (!user.IsActive)
        {
            return ServiceResult<User>.Fail(ErrorCodes.AuthAccountLocked, "Tài khoản của bạn đã bị khóa.");
        }

        var payload = $"{user.Id}:{expiresTicks}";
        var key = $"{SystemTokenSecret}_{user.PasswordHash}";
        var expectedSignature = ComputeHmac(payload, key);

        if (!string.Equals(expectedSignature, parts[2], StringComparison.Ordinal))
        {
            return ServiceResult<User>.Fail(ErrorCodes.AuthResetTokenInvalid, "Mã xác thực không hợp lệ hoặc mật khẩu đã được thay đổi trước đó.");
        }

        return ServiceResult<User>.Ok(user);
    }

    public async Task<ServiceResult<bool>> ResetPasswordAsync(string email, string token, string newPassword)
    {
        var validateResult = await ValidatePasswordResetTokenAsync(email, token);
        if (!validateResult.Success || validateResult.Data == null)
        {
            return ServiceResult<bool>.Fail(validateResult.ErrorCode ?? ErrorCodes.AuthResetTokenInvalid, validateResult.Message ?? "Mã xác thực không hợp lệ.");
        }

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
        {
            return ServiceResult<bool>.Fail(ErrorCodes.AuthPasswordTooWeak, "Mật khẩu mới phải có ít nhất 6 ký tự.");
        }

        var user = validateResult.Data;
        user.PasswordHash = _passwordHasher.HashPassword(newPassword);

        try
        {
            await _userRepository.UpdateAsync(user);
            return ServiceResult<bool>.Ok(true, "Đặt lại mật khẩu thành công. Vui lòng đăng nhập bằng mật khẩu mới.");
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.Fail(ErrorCodes.DatabaseError, $"Lỗi lưu mật khẩu mới: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> SendResetPasswordEmailAsync(string email, string resetLinkBaseUrl)
    {
        var tokenResult = await CreatePasswordResetTokenAsync(email);
        if (!tokenResult.Success || string.IsNullOrEmpty(tokenResult.Data))
        {
            return ServiceResult<bool>.Fail(tokenResult.ErrorCode ?? ErrorCodes.OperationFailed, tokenResult.Message ?? "Tạo mã đặt lại mật khẩu thất bại.");
        }

        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null)
        {
            return ServiceResult<bool>.Fail(ErrorCodes.ResourceNotFound, "Không tìm thấy tài khoản tương ứng với email này.");
        }

        var token = tokenResult.Data;
        var fullResetLink = $"{resetLinkBaseUrl}?email={Uri.EscapeDataString(user.Email)}&token={Uri.EscapeDataString(token)}";

        Console.WriteLine($"[PasswordReset] User: {user.Email} | Link: {fullResetLink}");

        var sendResult = await _emailService.SendPasswordResetEmailAsync(user.Email, user.FullName, fullResetLink);
        return sendResult;
    }

    // =========================================================================
    // PRIVATE CRYPTOGRAPHY HELPERS (HMAC-SHA256 & Base64Url)
    // =========================================================================

    private static string ComputeHmac(string payload, string key)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToBase64String(hash);
    }

    private static string Base64UrlEncode(string input)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(input))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string? Base64UrlDecode(string input)
    {
        try
        {
            string incoming = input.Replace('-', '+').Replace('_', '/');
            switch (incoming.Length % 4)
            {
                case 2: incoming += "=="; break;
                case 3: incoming += "="; break;
            }
            var bytes = Convert.FromBase64String(incoming);
            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return null;
        }
    }
}
