using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Services.Common;

namespace HighSchoolManagementSystem.Services.Interfaces;

public interface IAuthService
{
    Task<ServiceResult<User>> AuthenticateAsync(string usernameOrEmail, string password);
    Task<ServiceResult<string>> CreatePasswordResetTokenAsync(string email);
    Task<ServiceResult<User>> ValidatePasswordResetTokenAsync(string email, string token);
    Task<ServiceResult<bool>> ResetPasswordAsync(string email, string token, string newPassword);
    Task<ServiceResult<bool>> SendResetPasswordEmailAsync(string email, string resetLinkBaseUrl);
}
