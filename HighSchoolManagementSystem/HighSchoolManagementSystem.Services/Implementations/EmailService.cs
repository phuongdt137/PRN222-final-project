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
            Console.WriteLine($"[SMTP ERROR] Không thể gửi email tới {toEmail}: {ex.Message}");
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

    public async Task<ServiceResult<bool>> SendAttendanceAlertEmailAsync(
        string toEmail,
        string recipientName,
        string studentName,
        string className,
        string subjectName,
        int slotNumber,
        DateTime date,
        string status,
        string? note)
    {
        string statusBadgeText;
        string statusColor;

        switch (status)
        {
            case "AbsentWithoutPermission":
                statusBadgeText = "VẮNG MẶT KHÔNG PHÉP";
                statusColor = "#DC2626"; // Red
                break;
            case "AbsentWithPermission":
                statusBadgeText = "VẮNG MẶT CÓ PHÉP";
                statusColor = "#D97706"; // Amber
                break;
            case "Late":
                statusBadgeText = "ĐI HỌC MUỘN";
                statusColor = "#EA580C"; // Orange
                break;
            default:
                statusBadgeText = status;
                statusColor = "#4B5563";
                break;
        }

        var subject = $"[THPT Cảnh báo chuyên cần] Học sinh {studentName} - {statusBadgeText}";

        var body = $@"
<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: 'Segoe UI', Arial, sans-serif; background-color: #F8FAFC; margin: 0; padding: 24px; color: #1E293B; }}
        .container {{ max-width: 600px; margin: 0 auto; background: #FFFFFF; border-radius: 8px; border: 1px solid #E2E8F0; overflow: hidden; }}
        .header {{ background-color: {statusColor}; color: #FFFFFF; padding: 20px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 18px; font-weight: 700; }}
        .content {{ padding: 28px 24px; font-size: 14px; line-height: 1.6; }}
        .info-card {{ background-color: #F1F5F9; border-left: 4px solid {statusColor}; border-radius: 4px; padding: 16px; margin: 20px 0; }}
        .info-row {{ margin-bottom: 8px; }}
        .info-row strong {{ color: #334155; display: inline-block; width: 140px; }}
        .badge {{ display: inline-block; padding: 4px 10px; border-radius: 4px; font-weight: 700; font-size: 13px; color: #FFFFFF; background-color: {statusColor}; }}
        .footer {{ background-color: #F8FAFC; padding: 14px 24px; font-size: 12px; color: #64748B; text-align: center; border-top: 1px solid #E2E8F0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>THÔNG BÁO CHUYÊN CẦN HỌC SINH</h1>
        </div>
        <div class='content'>
            <p>Kính gửi quý phụ huynh / người giám hộ <strong>{WebUtility.HtmlEncode(recipientName)}</strong>,</p>
            <p>Hệ thống nề nếp nhà trường xin thông báo thông tin chuyên cần của học sinh trong ngày học hôm nay như sau:</p>
            
            <div class='info-card'>
                <div class='info-row'><strong>Học sinh:</strong> <span>{WebUtility.HtmlEncode(studentName)}</span></div>
                <div class='info-row'><strong>Lớp học:</strong> <span>{WebUtility.HtmlEncode(className)}</span></div>
                <div class='info-row'><strong>Môn học:</strong> <span>{WebUtility.HtmlEncode(subjectName)}</span></div>
                <div class='info-row'><strong>Tiết học:</strong> <span>Tiết {slotNumber}</span></div>
                <div class='info-row'><strong>Ngày học:</strong> <span>{date:dd/MM/yyyy}</span></div>
                <div class='info-row'><strong>Trạng thái:</strong> <span class='badge'>{statusBadgeText}</span></div>
                {(string.IsNullOrWhiteSpace(note) ? "" : $"<div class='info-row'><strong>Ghi chú:</strong> <span>{WebUtility.HtmlEncode(note)}</span></div>")}
            </div>

            <p style='color: #475569;'>Nhà trường kính đề nghị Quý phụ huynh nắm bắt thông tin và phối hợp chặt chẽ cùng Giáo viên chủ nhiệm để đảm bảo kỷ cương học tập của học sinh.</p>
        </div>
        <div class='footer'>
            <p>&copy; {DateTime.Now.Year} Trường THPT • Hệ thống Quản trị & Điều hành học vụ.</p>
        </div>
    </div>
</body>
</html>";

        return await SendEmailAsync(toEmail, subject, body);
    }

    public async Task<ServiceResult<bool>> SendLeaveRequestStatusEmailAsync(
        string toEmail,
        string recipientName,
        string studentName,
        DateTime startDate,
        DateTime endDate,
        string status,
        string? reviewNote,
        string reviewerName)
    {
        var isApproved = string.Equals(status, "Approved", StringComparison.OrdinalIgnoreCase);
        var headerColor = isApproved ? "#16A34A" : "#DC2626";
        var statusText = isApproved ? "ĐÃ ĐƯỢC PHÊ DUYỆT" : "TỪ CHỐI DUYỆT";
        var subject = $"[THPT Kết quả đơn xin nghỉ] Đơn nghỉ của học sinh {studentName} - {statusText}";

        var body = $@"
<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: 'Segoe UI', Arial, sans-serif; background-color: #F8FAFC; margin: 0; padding: 24px; color: #1E293B; }}
        .container {{ max-width: 600px; margin: 0 auto; background: #FFFFFF; border-radius: 8px; border: 1px solid #E2E8F0; overflow: hidden; }}
        .header {{ background-color: {headerColor}; color: #FFFFFF; padding: 20px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 18px; font-weight: 700; }}
        .content {{ padding: 28px 24px; font-size: 14px; line-height: 1.6; }}
        .info-card {{ background-color: #F1F5F9; border-left: 4px solid {headerColor}; border-radius: 4px; padding: 16px; margin: 20px 0; }}
        .info-row {{ margin-bottom: 8px; }}
        .info-row strong {{ color: #334155; display: inline-block; width: 140px; }}
        .badge {{ display: inline-block; padding: 4px 10px; border-radius: 4px; font-weight: 700; font-size: 13px; color: #FFFFFF; background-color: {headerColor}; }}
        .footer {{ background-color: #F8FAFC; padding: 14px 24px; font-size: 12px; color: #64748B; text-align: center; border-top: 1px solid #E2E8F0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>KẾT QUẢ XÉT DUYỆT ĐƠN NGHỈ PHÉP</h1>
        </div>
        <div class='content'>
            <p>Kính gửi <strong>{WebUtility.HtmlEncode(recipientName)}</strong>,</p>
            <p>Đơn xin nghỉ phép của học sinh <strong>{WebUtility.HtmlEncode(studentName)}</strong> đã được xét duyệt bởi <strong>{WebUtility.HtmlEncode(reviewerName)}</strong>.</p>
            
            <div class='info-card'>
                <div class='info-row'><strong>Học sinh:</strong> <span>{WebUtility.HtmlEncode(studentName)}</span></div>
                <div class='info-row'><strong>Thời gian nghỉ:</strong> <span>Từ {startDate:dd/MM/yyyy} đến {endDate:dd/MM/yyyy}</span></div>
                <div class='info-row'><strong>Trạng thái:</strong> <span class='badge'>{statusText}</span></div>
                <div class='info-row'><strong>Người duyệt:</strong> <span>{WebUtility.HtmlEncode(reviewerName)}</span></div>
                {(string.IsNullOrWhiteSpace(reviewNote) ? "" : $"<div class='info-row'><strong>Ý kiến phê duyệt:</strong> <span>{WebUtility.HtmlEncode(reviewNote)}</span></div>")}
            </div>

            <p style='color: #475569;'>Quý phụ huynh/học sinh có thể đăng nhập vào cổng thông tin trường để theo dõi chi tiết tình trạng chuyên cần.</p>
        </div>
        <div class='footer'>
            <p>&copy; {DateTime.Now.Year} Trường THPT • Hệ thống Quản trị & Điều hành học vụ.</p>
        </div>
    </div>
</body>
</html>";

        return await SendEmailAsync(toEmail, subject, body);
    }

    public async Task<ServiceResult<bool>> SendLeaveRequestCreatedEmailAsync(
        string toEmail,
        string teacherName,
        string studentName,
        string className,
        DateTime startDate,
        DateTime endDate,
        string reason)
    {
        var subject = $"[THPT Đơn xin nghỉ phép mới] Học sinh {studentName} ({className})";

        var body = $@"
<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: 'Segoe UI', Arial, sans-serif; background-color: #F8FAFC; margin: 0; padding: 24px; color: #1E293B; }}
        .container {{ max-width: 600px; margin: 0 auto; background: #FFFFFF; border-radius: 8px; border: 1px solid #E2E8F0; overflow: hidden; }}
        .header {{ background-color: #1B5B9B; color: #FFFFFF; padding: 20px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 18px; font-weight: 700; }}
        .content {{ padding: 28px 24px; font-size: 14px; line-height: 1.6; }}
        .info-card {{ background-color: #F1F5F9; border-left: 4px solid #1B5B9B; border-radius: 4px; padding: 16px; margin: 20px 0; }}
        .info-row {{ margin-bottom: 8px; }}
        .info-row strong {{ color: #334155; display: inline-block; width: 140px; }}
        .footer {{ background-color: #F8FAFC; padding: 14px 24px; font-size: 12px; color: #64748B; text-align: center; border-top: 1px solid #E2E8F0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>THÔNG BÁO ĐƠN XIN NGHỈ MỚI</h1>
        </div>
        <div class='content'>
            <p>Kính gửi Thầy/Cô <strong>{WebUtility.HtmlEncode(teacherName)}</strong> (Giáo viên chủ nhiệm),</p>
            <p>Học sinh trong lớp chủ nhiệm vừa gửi đơn xin nghỉ phép mới cần được Thầy/Cô xem xét và duyệt:</p>
            
            <div class='info-card'>
                <div class='info-row'><strong>Học sinh:</strong> <span>{WebUtility.HtmlEncode(studentName)}</span></div>
                <div class='info-row'><strong>Lớp:</strong> <span>{WebUtility.HtmlEncode(className)}</span></div>
                <div class='info-row'><strong>Thời gian nghỉ:</strong> <span>Từ {startDate:dd/MM/yyyy} đến {endDate:dd/MM/yyyy}</span></div>
                <div class='info-row'><strong>Lý do nghỉ:</strong> <span>{WebUtility.HtmlEncode(reason)}</span></div>
            </div>

            <p style='color: #475569;'>Thầy/Cô vui lòng đăng nhập vào hệ thống THPT, mục <strong>Đơn xin nghỉ phép</strong> để phê duyệt đơn kịp thời.</p>
        </div>
        <div class='footer'>
            <p>&copy; {DateTime.Now.Year} Trường THPT • Hệ thống Quản trị & Điều hành học vụ.</p>
        </div>
    </div>
</body>
</html>";

        return await SendEmailAsync(toEmail, subject, body);
    }

    public async Task<ServiceResult<bool>> SendScoreAlertEmailAsync(
        string toEmail,
        string recipientName,
        string studentName,
        string className,
        string subjectName,
        string assessmentName,
        decimal score,
        string? remarks)
    {
        var subject = $"[THPT Cập nhật điểm] Môn {subjectName} - Học sinh {studentName}: {score:0.##} điểm";

        var body = $@"
<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: 'Segoe UI', Arial, sans-serif; background-color: #F8FAFC; margin: 0; padding: 24px; color: #1E293B; }}
        .container {{ max-width: 600px; margin: 0 auto; background: #FFFFFF; border-radius: 8px; border: 1px solid #E2E8F0; overflow: hidden; }}
        .header {{ background-color: #0284C7; color: #FFFFFF; padding: 20px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 18px; font-weight: 700; }}
        .content {{ padding: 28px 24px; font-size: 14px; line-height: 1.6; }}
        .info-card {{ background-color: #F1F5F9; border-left: 4px solid #0284C7; border-radius: 4px; padding: 16px; margin: 20px 0; }}
        .info-row {{ margin-bottom: 8px; }}
        .info-row strong {{ color: #334155; display: inline-block; width: 140px; }}
        .score-pill {{ display: inline-block; padding: 4px 12px; border-radius: 4px; font-weight: 700; font-size: 16px; color: #FFFFFF; background-color: #0284C7; }}
        .footer {{ background-color: #F8FAFC; padding: 14px 24px; font-size: 12px; color: #64748B; text-align: center; border-top: 1px solid #E2E8F0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>THÔNG BÁO ĐIỂM HỌC TẬP</h1>
        </div>
        <div class='content'>
            <p>Kính gửi <strong>{WebUtility.HtmlEncode(recipientName)}</strong>,</p>
            <p>Giáo viên bộ môn đã cập nhật kết quả bài kiểm tra của học sinh <strong>{WebUtility.HtmlEncode(studentName)}</strong>:</p>
            
            <div class='info-card'>
                <div class='info-row'><strong>Học sinh:</strong> <span>{WebUtility.HtmlEncode(studentName)}</span></div>
                <div class='info-row'><strong>Lớp:</strong> <span>{WebUtility.HtmlEncode(className)}</span></div>
                <div class='info-row'><strong>Môn học:</strong> <span>{WebUtility.HtmlEncode(subjectName)}</span></div>
                <div class='info-row'><strong>Bài kiểm tra:</strong> <span>{WebUtility.HtmlEncode(assessmentName)}</span></div>
                <div class='info-row'><strong>Điểm số:</strong> <span class='score-pill'>{score:0.##} / 10</span></div>
                {(string.IsNullOrWhiteSpace(remarks) ? "" : $"<div class='info-row'><strong>Nhận xét:</strong> <span>{WebUtility.HtmlEncode(remarks)}</span></div>")}
            </div>

            <p style='color: #475569;'>Quý phụ huynh và học sinh có thể đăng nhập vào cổng thông tin trường để xem bảng điểm chi tiết các môn.</p>
        </div>
        <div class='footer'>
            <p>&copy; {DateTime.Now.Year} Trường THPT • Hệ thống Quản trị & Điều hành học vụ.</p>
        </div>
    </div>
</body>
</html>";

        return await SendEmailAsync(toEmail, subject, body);
    }
}
