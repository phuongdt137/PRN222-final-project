using System.Net;
using System.Net.Mail;
using System.Text;
using HighSchoolManagementSystem.Services.Common;
using HighSchoolManagementSystem.Services.Interfaces;

namespace HighSchoolManagementSystem.Services.Implementations;

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;

    public EmailService(EmailSettings settings)
    {
        _settings = settings;
    }

    public async Task<ServiceResult<bool>> SendEmailAsync(string toEmail, string subject, string htmlMessage)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            return ServiceResult<bool>.Fail(ErrorCodes.ValidationFailed, "Địa chỉ email người nhận không hợp lệ.");
        }

        if (string.IsNullOrWhiteSpace(_settings.SenderEmail) || string.IsNullOrWhiteSpace(_settings.Password))
        {
            Console.WriteLine($"[EmailService] Chưa cấu hình SenderEmail/Password. Bỏ qua gửi email tới: {toEmail}");
            return ServiceResult<bool>.Fail(ErrorCodes.EmailNotConfigured, 
                "Hệ thống email chưa được cấu hình SenderEmail hoặc App Password.");
        }

        try
        {
            using var mail = new MailMessage
            {
                From = new MailAddress(_settings.SenderEmail, _settings.SenderName, Encoding.UTF8),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true,
                BodyEncoding = Encoding.UTF8,
                SubjectEncoding = Encoding.UTF8
            };
            mail.To.Add(toEmail);

            using var client = new SmtpClient(_settings.SmtpServer, _settings.Port)
            {
                Credentials = new NetworkCredential(_settings.SenderEmail, _settings.Password),
                EnableSsl = _settings.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 15000 // 15 giây timeout
            };

            await client.SendMailAsync(mail);
            return ServiceResult<bool>.Ok(true, "Email đã được gửi thành công.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SMTP ERROR] Không thể gửi email: {ex.Message}");
            return ServiceResult<bool>.Fail(ErrorCodes.EmailSendFailed, $"Lỗi gửi email qua máy chủ SMTP: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> SendPasswordResetEmailAsync(string toEmail, string recipientName, string resetLink)
    {
        var subject = "[THPT] Hướng dẫn đặt lại mật khẩu tài khoản hệ thống";

        var bodyBuilder = new StringBuilder();
        bodyBuilder.Append(@"
<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='UTF-8'>
    <style>
        body { font-family: 'Segoe UI', Arial, sans-serif; background-color: #F8FAFC; margin: 0; padding: 24px; color: #1E293B; }
        .container { max-width: 600px; margin: 0 auto; background: #FFFFFF; border-radius: 8px; border: 1px solid #E2E8F0; overflow: hidden; }
        .header { background-color: #1B5B9B; color: #FFFFFF; padding: 24px; text-align: center; }
        .header h1 { margin: 0; font-size: 20px; font-weight: 700; letter-spacing: 0.5px; }
        .content { padding: 32px 24px; font-size: 14px; line-height: 1.6; }
        .btn-wrapper { text-align: center; margin: 32px 0; }
        .btn-reset { background-color: #1B5B9B; color: #FFFFFF !important; text-decoration: none; padding: 12px 28px; border-radius: 6px; font-weight: 600; display: inline-block; font-size: 14px; }
        .footer { background-color: #F1F5F9; padding: 16px 24px; font-size: 12px; color: #64748B; text-align: center; border-top: 1px solid #E2E8F0; }
        .warning-box { background-color: #FEF3C7; border: 1px solid #F59E0B; border-radius: 6px; padding: 12px 16px; margin: 20px 0; font-size: 13px; color: #92400E; }
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>HỆ THỐNG QUẢN LÝ TRƯỜNG THPT</h1>
        </div>
        <div class='content'>
            <p>Kính gửi <strong>" + WebUtility.HtmlEncode(recipientName) + @"</strong>,</p>
            <p>Hệ thống nhận được yêu cầu khôi phục mật khẩu đăng nhập cho tài khoản liên kết với địa chỉ email này.</p>
            <p>Để hoàn tất việc đặt lại mật khẩu mới, quý người dùng vui lòng bấm vào nút bên dưới:</p>
            <div class='btn-wrapper'>
                <a href='" + resetLink + @"' class='btn-reset'>ĐẶT LẠI MẬT KHẨU</a>
            </div>
            <div class='warning-box'>
                <strong>Lưu ý quan trọng:</strong>
                <ul style='margin: 6px 0 0 0; padding-left: 18px;'>
                    <li>Liên kết này chỉ có hiệu lực trong vòng <strong>15 phút</strong>.</li>
                    <li>Liên kết chỉ có thể sử dụng <strong>1 lần duy nhất</strong>.</li>
                    <li>Nếu bạn không yêu cầu hành động này, vui lòng bỏ qua email hoặc thông báo cho Ban Giám Hiệu.</li>
                </ul>
            </div>
            <p style='color: #64748B; font-size: 12.5px; word-break: break-all;'>
                Nếu không bấm được nút trên, hãy sao chép đường link sau dán vào trình duyệt:<br>
                <a href='" + resetLink + @"'>" + resetLink + @"</a>
            </p>
        </div>
        <div class='footer'>
            <p>&copy; " + DateTime.Now.Year + @" Hệ Thống Quản Lý Trường THPT. Email tự động, vui lòng không phản hồi.</p>
        </div>
    </div>
</body>
</html>");

        return await SendEmailAsync(toEmail, subject, bodyBuilder.ToString());
    }
}
