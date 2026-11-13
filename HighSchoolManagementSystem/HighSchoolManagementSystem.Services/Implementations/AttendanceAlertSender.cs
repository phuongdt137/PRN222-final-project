using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Repositories.Interfaces;
using HighSchoolManagementSystem.Services.DTOs;
using HighSchoolManagementSystem.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace HighSchoolManagementSystem.Services.Implementations;

// Gửi cảnh báo sau khi điểm danh đã lưu thành công.
public class AttendanceAlertSender
{
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly INotificationService _notificationService;
    private readonly IEmailService _emailService;
    private readonly ILogger<AttendanceAlertSender> _logger;

    public AttendanceAlertSender(
        IAttendanceRepository attendanceRepository,
        INotificationService notificationService,
        IEmailService emailService,
        ILogger<AttendanceAlertSender> logger)
    {
        _attendanceRepository = attendanceRepository;
        _notificationService = notificationService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<bool> SendAsync(int userId, AttendanceSession session, List<AttendanceSaveRowDto> rows)
    {
        var delivered = true;

        try
        {
            var studentIds = rows.Select(r => r.StudentId).ToArray();
            var students = await _attendanceRepository.GetAlertStudentsAsync(studentIds);

            foreach (var student in students)
            {
                var row = rows.First(r => r.StudentId == student.Id);
                var status = AttendanceStatuses.Label(row.Status);
                var title = $"Thông báo chuyên cần: {status} - Tiết {session.PeriodNo}";
                var content = $"Học sinh {student.User.FullName}: {status}, tiết {session.PeriodNo} " +
                    $"môn {session.Subject.SubjectName}, lớp {session.Class.ClassName}, " +
                    $"ngày {session.AttendanceDate:dd/MM/yyyy}.";

                var recipientIds = new List<int>();

                if (student.UserId > 0)
                {
                    recipientIds.Add(student.UserId);
                }

                if (student.Parent != null && student.Parent.UserId > 0 &&
                    !recipientIds.Contains(student.Parent.UserId))
                {
                    recipientIds.Add(student.Parent.UserId);
                }

                foreach (var recipientId in recipientIds)
                {
                    try
                    {
                        var result = await _notificationService.SendNotificationAsync(
                            userId, recipientId, title, content);

                        if (!result.Success)
                        {
                            delivered = false;
                            _logger.LogWarning(
                                "Attendance notification failed: session {SessionId}, recipient {RecipientId}",
                                session.Id, recipientId);
                        }
                    }
                    catch (Exception ex)
                    {
                        delivered = false;
                        _logger.LogError(ex,
                            "Attendance notification failed: session {SessionId}, recipient {RecipientId}",
                            session.Id, recipientId);
                    }
                }

                var parent = student.Parent?.User;

                if (string.IsNullOrWhiteSpace(parent?.Email))
                {
                    delivered = false;
                    _logger.LogWarning(
                        "Attendance email missing parent address: session {SessionId}, student {StudentId}",
                        session.Id, student.Id);
                    continue;
                }

                try
                {
                    // Mẫu email dùng tên trạng thái cũ cho trường hợp vắng không phép.
                    var emailStatus = row.Status == AttendanceStatuses.Absent
                        ? "AbsentWithoutPermission" : row.Status;
                    var comment = string.IsNullOrWhiteSpace(row.Comment) ? null : row.Comment.Trim();

                    var result = await _emailService.SendAttendanceAlertEmailAsync(
                        parent.Email,
                        parent.FullName,
                        student.User.FullName,
                        session.Class.ClassName,
                        session.Subject.SubjectName,
                        session.PeriodNo,
                        session.AttendanceDate,
                        emailStatus,
                        comment);

                    if (!result.Success)
                    {
                        delivered = false;
                        _logger.LogWarning(
                            "Attendance email failed: session {SessionId}, student {StudentId}",
                            session.Id, student.Id);
                    }
                }
                catch (Exception ex)
                {
                    delivered = false;
                    _logger.LogError(ex,
                        "Attendance email failed: session {SessionId}, student {StudentId}",
                        session.Id, student.Id);
                }
            }
        }
        catch (Exception ex)
        {
            delivered = false;
            _logger.LogError(ex, "Attendance alert delivery failed for session {SessionId}", session.Id);
        }

        return delivered;
    }
}
