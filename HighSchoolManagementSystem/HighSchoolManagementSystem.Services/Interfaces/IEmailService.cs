using HighSchoolManagementSystem.Services.Common;

namespace HighSchoolManagementSystem.Services.Interfaces;

public interface IEmailService
{
    Task<ServiceResult<bool>> SendEmailAsync(string toEmail, string subject, string htmlMessage);
    Task<ServiceResult<bool>> SendPasswordResetEmailAsync(string toEmail, string recipientName, string resetLink);
}
