namespace HighSchoolManagementSystem.Services.Common;

public class EmailSettings
{
    public string SmtpServer { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderName { get; set; } = "Hệ Thống Quản Lý Trường THPT";
    public string Password { get; set; } = string.Empty;
}
