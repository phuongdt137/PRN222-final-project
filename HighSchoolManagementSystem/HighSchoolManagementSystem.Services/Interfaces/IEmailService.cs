using HighSchoolManagementSystem.Services.Common;

namespace HighSchoolManagementSystem.Services.Interfaces;

public interface IEmailService
{
    Task<ServiceResult<bool>> SendEmailAsync(string toEmail, string subject, string htmlMessage);
    Task<ServiceResult<bool>> SendPasswordResetEmailAsync(string toEmail, string recipientName, string resetLink);
    Task<ServiceResult<bool>> SendAttendanceAlertEmailAsync(
        string toEmail, 
        string recipientName, 
        string studentName, 
        string className, 
        string subjectName, 
        int slotNumber, 
        DateTime date, 
        string status, 
        string? note);

    Task<ServiceResult<bool>> SendLeaveRequestStatusEmailAsync(
        string toEmail, 
        string recipientName, 
        string studentName, 
        DateTime startDate, 
        DateTime endDate, 
        string status, 
        string? reviewNote, 
        string reviewerName);

    Task<ServiceResult<bool>> SendLeaveRequestCreatedEmailAsync(
        string toEmail, 
        string teacherName, 
        string studentName, 
        string className, 
        DateTime startDate, 
        DateTime endDate, 
        string reason);

    Task<ServiceResult<bool>> SendScoreAlertEmailAsync(
        string toEmail, 
        string recipientName, 
        string studentName, 
        string className, 
        string subjectName, 
        string assessmentName, 
        decimal score, 
        string? remarks);
}
