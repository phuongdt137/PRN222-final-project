using System.Data.Common;
using System.Text.Json;
using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Repositories.Interfaces;
using HighSchoolManagementSystem.Services.Common;
using HighSchoolManagementSystem.Services.DTOs;
using HighSchoolManagementSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HighSchoolManagementSystem.Services.Implementations;

public class AttendanceService(
    IAttendanceRepository repository, 
    TimeProvider clock,
    ILogger<AttendanceService> logger,
    INotificationService notificationService,
    IEmailService emailService) : IAttendanceService
{
    private DateTimeOffset Now => clock.GetUtcNow();
    private static ServiceResult Forbidden() => ServiceResult.Fail(ErrorCodes.AuthForbidden, "Bạn không có quyền thực hiện thao tác này.");
    private static ServiceResult Invalid(string message) => ServiceResult.Fail(ErrorCodes.ValidationFailed, message);
    private static ServiceResult<T> Failure<T>(ServiceResult result) =>
        ServiceResult<T>.Fail(result.ErrorCode ?? ErrorCodes.ValidationFailed, result.Message ?? "Không thể thực hiện.");
    private static bool IsStaff(User? user) => user?.Role.RoleName == "Staff";
    private static bool IsTeacher(User? user) => user?.Role.RoleName == "Teacher" && user.Teacher is { Status: "Active" };
    private static bool Owns(User? user, AttendanceSession session) =>
        IsTeacher(user) && user!.Teacher!.Id == session.AssignedTeacherId;

    public async Task<ServiceResult<AttendanceListDto>> GetSessionsAsync(int userId, AttendanceFilter filter)
    {
        var actor = await repository.GetActorAsync(userId);
        if (!IsStaff(actor) && !IsTeacher(actor)) return Failure<AttendanceListDto>(Forbidden());
        var sync = await EnsureTodaySessionsAsync();
        if (!sync.Success) return Failure<AttendanceListDto>(sync);
        Normalize(filter);
        var query = FilterSessions(repository.Sessions.AsNoTracking(), filter);
        if (!IsStaff(actor)) query = query.Where(s => s.AssignedTeacherId == actor!.Teacher!.Id);
        if (!string.IsNullOrEmpty(filter.Status)) query = query.Where(s => s.Status == filter.Status);
        var total = await query.CountAsync();
        filter.Page = ClampPage(filter.Page, total);
        var sessions = await query.Include(s => s.Records).OrderByDescending(s => s.AttendanceDate)
            .ThenBy(s => s.StartTime).ThenBy(s => s.Id).Skip((filter.Page - 1) * 25).Take(25).ToListAsync();
        return ServiceResult<AttendanceListDto>.Ok(new()
        {
            Filter = filter, IsStaff = IsStaff(actor), TotalCount = total,
            Items = sessions.Select(s => Map(s, Owns(actor, s), IsStaff(actor))).ToList(),
            Semesters = await SemesterOptions(), Classes = await ClassOptions(filter.SemesterId)
        });
    }

    /// <summary>Creates today's sheets and reconciles only unsubmitted sheets with the timetable.</summary>
    public Task<ServiceResult> EnsureTodaySessionsAsync() => Write(async () =>
    {
        var today = AttendanceRules.Today(Now);
        var day = (int)today.DayOfWeek + 1;
        var entries = await repository.Entries.Where(e => e.DayOfWeek == day &&
            e.TeachingAssignment.Semester.IsActive && e.TeachingAssignment.Semester.StartDate <= today &&
            e.TeachingAssignment.Semester.EndDate >= today).ToListAsync();
        var sessions = await repository.Sessions.Include(s => s.Records)
            .Where(s => s.AttendanceDate == today).ToListAsync();
        var candidates = new List<AttendanceSession>();
        foreach (var entry in entries)
        {
            if (ValidateEntry(entry, today) != null) continue;
            var candidate = NewSession(entry, today);
            var roster = await Roster(candidate.ClassId, today, candidate.StartTime);
            if (roster.Count == 0) continue;
            foreach (var id in roster) candidate.Records.Add(NewRecord(candidate, id));
            candidates.Add(candidate);
        }
        // Conflicting timetable entries must not produce an arbitrary attendance sheet.
        var valid = candidates.Where(c => !candidates.Any(other => !ReferenceEquals(c, other) && Overlaps(c, other)) &&
            !sessions.Any(other => other.Status == AttendanceStatuses.Submitted &&
                other.TimetableEntryId != c.TimetableEntryId && Overlaps(c, other))).ToList();
        foreach (var session in sessions.Where(s => s.Status == AttendanceStatuses.Open &&
                     !valid.Any(c => c.TimetableEntryId == s.TimetableEntryId)))
        {
            var before = Snapshot(session);
            session.Status = AttendanceStatuses.Cancelled;
            session.UpdatedAt = Now.UtcDateTime;
            Audit(session, null, "AutoCancel", new { Reason = "Lịch học hoặc danh sách học sinh không còn hợp lệ, hoặc có lịch trùng giờ.", Before = before, After = Snapshot(session) });
        }
        foreach (var candidate in valid)
        {
            var session = sessions.SingleOrDefault(s => s.TimetableEntryId == candidate.TimetableEntryId);
            if (session?.Status == AttendanceStatuses.Submitted) continue; // Preserve completed attendance and its roster.
            if (session == null)
            {
                repository.Add(candidate);
                Audit(candidate, null, "AutoCreate", new { Reason = "Tự động tạo từ lịch học trong ngày.", After = Snapshot(candidate) });
                continue;
            }
            var entry = entries.Single(e => e.Id == candidate.TimetableEntryId);
            var roster = candidate.Records.Select(r => r.StudentId).ToHashSet();
            if (session.Status == AttendanceStatuses.Open && MatchesSchedule(session, entry) &&
                roster.SetEquals(session.Records.Select(r => r.StudentId))) continue;
            var before = Snapshot(session);
            ApplySnapshot(session, entry);
            var removed = session.Records.Where(r => !roster.Contains(r.StudentId)).ToArray();
            repository.RemoveRecords(removed);
            foreach (var row in removed) session.Records.Remove(row);
            foreach (var id in roster.Except(session.Records.Select(r => r.StudentId)).ToArray())
                session.Records.Add(NewRecord(session, id));
            session.Status = AttendanceStatuses.Open;
            session.UpdatedAt = Now.UtcDateTime;
            Audit(session, null, "AutoRefresh", new { Reason = "Tự động đồng bộ lịch học/danh sách trước khi giáo viên điểm danh.", Before = before, After = Snapshot(session) });
        }
        return ServiceResult.Ok();
    });

    private static object Snapshot(AttendanceSession s) => new
    {
        s.Status, s.ClassId, s.SubjectId, s.AssignedTeacherId, s.RoomId, s.TimeSlotId,
        s.SlotCode, s.PeriodNo, s.SchoolSession, s.StartTime, s.EndTime,
        Students = s.Records.Select(r => r.StudentId).Order().ToArray()
    };

    public async Task<ServiceResult<AttendanceSheetDto>> GetSheetAsync(int userId, int sessionId)
    {
        var actor = await repository.GetActorAsync(userId);
        if (!IsStaff(actor) && !IsTeacher(actor)) return Failure<AttendanceSheetDto>(Forbidden());
        var sync = await EnsureTodaySessionsAsync();
        if (!sync.Success) return Failure<AttendanceSheetDto>(sync);
        var session = await repository.Sessions.AsNoTracking().Include(s => s.Records)
            .ThenInclude(r => r.Student).ThenInclude(s => s.User).SingleOrDefaultAsync(s => s.Id == sessionId);
        if (session == null) return ServiceResult<AttendanceSheetDto>.Fail(ErrorCodes.ResourceNotFound, "Không tìm thấy bảng điểm danh.");
        if (!IsStaff(actor) && !Owns(actor, session)) return Failure<AttendanceSheetDto>(Forbidden());
        var model = new AttendanceSheetDto { Session = Map(session, Owns(actor, session), IsStaff(actor)), IsStaff = IsStaff(actor) };
        if (session.Status == AttendanceStatuses.Open)
        {
            var roster = await Roster(session.ClassId, session.AttendanceDate, session.StartTime);
            model.RosterChanged = !roster.ToHashSet().SetEquals(session.Records.Select(r => r.StudentId));
            var entry = await repository.Entries.AsNoTracking().SingleOrDefaultAsync(e => e.Id == session.TimetableEntryId);
            model.ScheduleChanged = entry == null || !MatchesSchedule(session, entry);
        }
        if (!model.Session.CanWrite) model.ReadOnlyReason = ReadOnlyReason(session, IsStaff(actor));
        if (model.RosterChanged || model.ScheduleChanged)
        {
            model.Session.CanWrite = false;
            model.ReadOnlyReason = "Danh sách học sinh hoặc lịch học đã thay đổi. Vui lòng tải lại để hệ thống đồng bộ trước khi điểm danh.";
        }
        var ids = session.Records.Select(r => r.StudentId).ToArray();
        var leaves = await ApprovedLeaves(ids, session.AttendanceDate);
        model.Students = session.Records.OrderBy(r => r.Student.StudentCode).Select(r => new AttendanceStudentDto
        {
            StudentId = r.StudentId, StudentCode = r.Student.StudentCode, FullName = r.Student.User.FullName,
            Status = r.Status, Comment = r.Comment, LeaveRequestId = r.LeaveRequestId,
            ApprovedLeaves = leaves.Where(l => l.StudentId == r.StudentId)
                .Select(l => new AttendanceOption(l.Id, $"Đơn #{l.Id}: {l.StartDate:dd/MM/yyyy} – {l.EndDate:dd/MM/yyyy} · {l.Reason}")).ToList()
        }).ToList();
        if (model.IsStaff)
        {
            model.Audit = await repository.AuditLogs.AsNoTracking().Where(a => a.AttendanceSessionId == sessionId)
                .OrderByDescending(a => a.Id).Take(20)
                .Select(a => new AttendanceAuditDto(a.OccurredAt, a.ActorUser == null ? "Hệ thống" : a.ActorUser.FullName, a.Action, a.Details)).ToListAsync();
        }
        return ServiceResult<AttendanceSheetDto>.Ok(model);
    }

    public async Task<ServiceResult> SaveAsync(int userId, AttendanceSaveDto input)
    {
        DateTimeOffset? deadline = null;
        AttendanceSession? savedSession = null;
        List<AttendanceSaveRowDto> alertRows = [];
        var result = await Write(async () =>
        {
            var actor = await repository.GetActorAsync(userId);
            if (!IsTeacher(actor) && !IsStaff(actor)) return Forbidden();
            var session = await repository.Sessions
                .Include(s => s.Records)
                .Include(s => s.Class)
                .Include(s => s.Subject)
                .SingleOrDefaultAsync(s => s.Id == input.SessionId);
            if (session == null) return Invalid("Không tìm thấy bảng điểm danh.");
            if (!IsStaff(actor) && !Owns(actor, session)) return Forbidden();
            if (IsStaff(actor) && (session.Status != AttendanceStatuses.Submitted ||
                session.Records.Count == 0 || session.Records.Any(r => !r.RecordedBy.HasValue || !AttendanceStatuses.IsResult(r.Status))))
                return Invalid("Staff chỉ được sửa sau khi giáo viên đã điểm danh đầy đủ tiết học này.");
            deadline = AttendanceRules.Deadline(session.AttendanceDate);
            if (session.Status is not (AttendanceStatuses.Open or AttendanceStatuses.Submitted) || !session.Semester.IsActive ||
                !AttendanceRules.CanWrite(session.AttendanceDate, session.StartTime, Now))
                return Invalid(ReadOnlyReason(session, IsStaff(actor)));
            if (!CheckVersion(session, input.RowVersion)) return Invalid("Bảng đã thay đổi. Vui lòng tải lại trước khi lưu.");
            if (input.Students.Count != session.Records.Count || input.Students.Count == 0 ||
                input.Students.Select(r => r.StudentId).Distinct().Count() != input.Students.Count ||
                !input.Students.Select(r => r.StudentId).ToHashSet().SetEquals(session.Records.Select(r => r.StudentId)))
                return Invalid("Danh sách gửi lên không khớp bảng điểm danh.");
            if (session.Status == AttendanceStatuses.Open)
            {
                var entry = await repository.Entries.SingleOrDefaultAsync(e => e.Id == session.TimetableEntryId);
                if (entry == null || !MatchesSchedule(session, entry)) return Invalid("Lịch học đã thay đổi. Vui lòng tải lại để hệ thống tự đồng bộ bảng.");
                var entryError = ValidateEntry(entry, session.AttendanceDate);
                if (entryError != null) return Invalid(entryError);
                var roster = await Roster(session.ClassId, session.AttendanceDate, session.StartTime);
                if (!roster.ToHashSet().SetEquals(session.Records.Select(r => r.StudentId)))
                    return Invalid("Học sinh đã chuyển lớp hoặc danh sách đã thay đổi. Vui lòng tải lại để hệ thống tự đồng bộ bảng.");
            }
            var leaves = await ApprovedLeaves(input.Students.Select(r => r.StudentId).ToArray(), session.AttendanceDate);
            var validation = ValidateRows(input.Students, leaves);
            if (validation != null) return Invalid(validation);
            var wasSubmitted = session.Status == AttendanceStatuses.Submitted;
            var changes = input.Students.Where(row =>
            {
                var old = session.Records.Single(r => r.StudentId == row.StudentId);
                return old.Status != row.Status || old.Comment != Clean(row.Comment) || old.LeaveRequestId != row.LeaveRequestId;
            }).ToList();
            if (wasSubmitted && changes.Count == 0) return ServiceResult.Ok("Kết quả không thay đổi.");
            if (wasSubmitted && (string.IsNullOrWhiteSpace(input.Reason) || input.Reason.Length > 500))
                return Invalid("Vui lòng nhập lý do chỉnh sửa (tối đa 500 ký tự).");
            var before = session.Records.Select(r => new { r.StudentId, r.Status, r.Comment, r.LeaveRequestId }).ToArray();
            foreach (var row in changes)
            {
                var record = session.Records.Single(r => r.StudentId == row.StudentId);
                record.Status = row.Status;
                record.Comment = Clean(row.Comment);
                record.LeaveRequestId = row.LeaveRequestId;
                // Staff corrections retain the teacher who originally recorded attendance.
                if (IsTeacher(actor)) record.RecordedBy = actor!.Teacher!.Id;
                record.RecordedAt ??= Now.UtcDateTime;
                record.UpdatedAt = Now.UtcDateTime;
            }
            session.Status = AttendanceStatuses.Submitted;
            session.SubmittedAt ??= Now.UtcDateTime;
            session.UpdatedAt = Now.UtcDateTime;
            Audit(session, userId, wasSubmitted ? "Update" : "Submit", new
            {
                Before = before,
                After = session.Records.Select(r => new { r.StudentId, r.Status, r.Comment, r.LeaveRequestId }).ToArray(),
                Reason = Clean(input.Reason)
            });

            // Delivery runs only after the attendance and audit transaction commits.
            savedSession = session;
            alertRows = changes.Where(c => (c.Status is AttendanceStatuses.Absent or AttendanceStatuses.Late) &&
                before.Any(old => old.StudentId == c.StudentId && old.Status != c.Status)).ToList();
            return ServiceResult.Ok(wasSubmitted ? "Đã cập nhật điểm danh." : "Đã xác nhận điểm danh.");
        }, () => deadline);
        if (!result.Success || savedSession == null || alertRows.Count == 0) return result;
        var delivered = await SendAlerts(userId, savedSession, alertRows);
        return delivered ? result : ServiceResult.Ok(result.Message + " Một số thông báo/email chưa gửi được; vui lòng kiểm tra nhật ký hệ thống.");
    }

    private static string? ValidateRows(List<AttendanceSaveRowDto> rows, List<LeaveRequest> leaves)
    {
        foreach (var row in rows)
        {
            if (!AttendanceStatuses.IsResult(row.Status)) return "Vui lòng chọn kết quả cho tất cả học sinh.";
            if (row.Comment?.Length > 255) return "Ghi chú không được vượt quá 255 ký tự.";
            if (row.Status == AttendanceStatuses.Absent && leaves.Any(l => l.StudentId == row.StudentId))
                return "Học sinh có đơn nghỉ đã duyệt cho ngày học này; không được đánh dấu vắng không phép.";
            if (row.Status == AttendanceStatuses.Excused && !row.LeaveRequestId.HasValue)
                return "Vắng có phép bắt buộc chọn một đơn nghỉ đã duyệt phù hợp; ghi chú không thay thế đơn nghỉ.";
            if (row.LeaveRequestId.HasValue && (row.Status != AttendanceStatuses.Excused ||
                !leaves.Any(l => l.Id == row.LeaveRequestId && l.StudentId == row.StudentId)))
                return "Đơn nghỉ phải đã duyệt, thuộc đúng học sinh, bao phủ ngày học và dùng cho kết quả vắng có phép.";
        }
        return null;
    }

    private async Task<bool> SendAlerts(int userId, AttendanceSession session, List<AttendanceSaveRowDto> rows)
    {
        var delivered = true;
        try
        {
            var ids = rows.Select(r => r.StudentId).ToArray();
            var students = await repository.Students.AsNoTracking().Include(s => s.Parent).ThenInclude(p => p!.User)
                .Where(s => ids.Contains(s.Id)).ToListAsync();
            foreach (var student in students)
            {
                var row = rows.Single(r => r.StudentId == student.Id);
                var status = AttendanceStatuses.Label(row.Status);
                var title = $"Thông báo chuyên cần: {status} - Tiết {session.PeriodNo}";
                var content = $"Học sinh {student.User.FullName}: {status}, tiết {session.PeriodNo} môn {session.Subject.SubjectName}, lớp {session.Class.ClassName}, ngày {session.AttendanceDate:dd/MM/yyyy}.";
                foreach (var recipient in new int?[] { student.UserId, student.Parent?.UserId }.Where(id => id > 0).Distinct())
                {
                    try
                    {
                        var sent = await notificationService.SendNotificationAsync(userId, recipient!.Value, title, content);
                        if (!sent.Success) { delivered = false; logger.LogWarning("Attendance notification failed: session {SessionId}, student {StudentId}, recipient {RecipientId}", session.Id, student.Id, recipient); }
                    }
                    catch (Exception ex)
                    {
                        delivered = false;
                        logger.LogError(ex, "Attendance notification failed: session {SessionId}, student {StudentId}", session.Id, student.Id);
                    }
                }
                var parent = student.Parent?.User;
                if (string.IsNullOrWhiteSpace(parent?.Email))
                {
                    delivered = false;
                    logger.LogWarning("Attendance email missing parent address: session {SessionId}, student {StudentId}", session.Id, student.Id);
                    continue;
                }
                try
                {
                    var sent = await emailService.SendAttendanceAlertEmailAsync(parent.Email, parent.FullName,
                        student.User.FullName, session.Class.ClassName, session.Subject.SubjectName, session.PeriodNo,
                        // The existing shared email template uses legacy absence status names.
                        session.AttendanceDate, row.Status == AttendanceStatuses.Absent ? "AbsentWithoutPermission" : row.Status, Clean(row.Comment));
                    if (!sent.Success) { delivered = false; logger.LogWarning("Attendance email failed: session {SessionId}, student {StudentId}", session.Id, student.Id); }
                }
                catch (Exception ex)
                {
                    delivered = false;
                    logger.LogError(ex, "Attendance email failed: session {SessionId}, student {StudentId}", session.Id, student.Id);
                }
            }
        }
        catch (Exception ex)
        {
            delivered = false;
            logger.LogError(ex, "Attendance alert delivery failed for session {SessionId}", session.Id);
        }
        return delivered;
    }

    public async Task<ServiceResult<AttendanceStudentHistoryDto>> GetHistoryAsync(int userId, AttendanceFilter filter)
    {
        var actor = await repository.GetActorAsync(userId);
        if (actor?.Role.RoleName is not ("Student" or "Parent")) return Failure<AttendanceStudentHistoryDto>(Forbidden());
        var model = new AttendanceStudentHistoryDto { Filter = filter, IsParent = actor.Role.RoleName == "Parent" };
        if (model.IsParent)
        {
            if (actor.Parent == null) return Failure<AttendanceStudentHistoryDto>(Forbidden());
            model.Children = await repository.Students.AsNoTracking().Where(s => s.ParentId == actor.Parent.ParentId)
                .OrderBy(s => s.StudentCode).Select(s => new AttendanceOption(s.Id, s.User.FullName + " (" + s.StudentCode + ")")).ToListAsync();
            if (filter.StudentId.HasValue && !model.Children.Any(c => c.Id == filter.StudentId))
                return Failure<AttendanceStudentHistoryDto>(Forbidden());
            filter.StudentId ??= model.Children.FirstOrDefault()?.Id;
        }
        else
        {
            if (actor.Student == null || (filter.StudentId.HasValue && filter.StudentId != actor.Student.Id))
                return Failure<AttendanceStudentHistoryDto>(Forbidden());
            filter.StudentId = actor.Student.Id;
        }
        Normalize(filter);
        model.Semesters = await SemesterOptions();
        if (!filter.StudentId.HasValue) return ServiceResult<AttendanceStudentHistoryDto>.Ok(model);
        model.StudentName = await repository.Students.Where(s => s.Id == filter.StudentId).Select(s => s.User.FullName).SingleAsync();
        var own = repository.Records.AsNoTracking().Where(r => r.StudentId == filter.StudentId &&
            r.AttendanceSession.Status != AttendanceStatuses.Cancelled);
        var subjects = await own.Select(r => new { Id = r.AttendanceSession.SubjectId, Name = r.AttendanceSession.Subject.SubjectName })
            .Distinct().OrderBy(s => s.Name).ToListAsync();
        model.Subjects = subjects.Select(s => new AttendanceOption(s.Id, s.Name)).ToList();
        var query = own.Where(r => r.AttendanceDate >= filter.FromDate && r.AttendanceDate <= filter.ToDate &&
            (!filter.SemesterId.HasValue || r.AttendanceSession.SemesterId == filter.SemesterId) &&
            (!filter.SubjectId.HasValue || r.AttendanceSession.SubjectId == filter.SubjectId));
        if (!string.IsNullOrEmpty(filter.Status))
            query = filter.Status == AttendanceStatuses.NotMarked
                ? query.Where(r => r.AttendanceSession.Status == AttendanceStatuses.Open)
                : query.Where(r => r.AttendanceSession.Status == AttendanceStatuses.Submitted && r.Status == filter.Status);
        var counts = await query.GroupBy(r => r.AttendanceSession.Status == AttendanceStatuses.Submitted ? r.Status : AttendanceStatuses.NotMarked)
            .Select(g => new { Status = g.Key, Count = g.Count() }).ToListAsync();
        int Count(string s) => counts.Where(c => c.Status == s).Sum(c => c.Count);
        model.Present = Count(AttendanceStatuses.Present); model.Absent = Count(AttendanceStatuses.Absent);
        model.Late = Count(AttendanceStatuses.Late); model.Excused = Count(AttendanceStatuses.Excused);
        model.Pending = Count(AttendanceStatuses.NotMarked);
        model.TotalCount = counts.Sum(c => c.Count);
        filter.Page = ClampPage(filter.Page, model.TotalCount);
        var rows = await query.Include(r => r.AttendanceSession).ThenInclude(s => s.Class)
            .Include(r => r.AttendanceSession).ThenInclude(s => s.Subject)
            .Include(r => r.AttendanceSession).ThenInclude(s => s.AssignedTeacher).ThenInclude(t => t.User)
            .Include(r => r.AttendanceSession).ThenInclude(s => s.Room)
            .Include(r => r.AttendanceSession).ThenInclude(s => s.Semester)
            .Include(r => r.RecordedByTeacher).ThenInclude(t => t!.User)
            .OrderByDescending(r => r.AttendanceDate).ThenBy(r => r.AttendanceSession.StartTime).ThenBy(r => r.Id)
            .Skip((filter.Page - 1) * 25).Take(25).ToListAsync();
        model.Items = rows.Select(r => new AttendanceHistoryRowDto
        {
            Session = Map(r.AttendanceSession, false),
            Status = r.AttendanceSession.Status == AttendanceStatuses.Submitted ? r.Status : AttendanceStatuses.NotMarked,
            Comment = r.AttendanceSession.Status == AttendanceStatuses.Submitted ? r.Comment : null,
            RecordedBy = r.AttendanceSession.Status == AttendanceStatuses.Submitted ? r.RecordedByTeacher?.User.FullName : null
        }).ToList();
        return ServiceResult<AttendanceStudentHistoryDto>.Ok(model);
    }

    private string? ValidateEntry(TimetableEntry entry, DateTime date)
    {
        var a = entry.TeachingAssignment;
        if (date.Date < AttendanceRules.Today(Now)) return "Không được tạo hoặc mở lại bảng điểm danh cho ngày đã qua.";
        if (!a.Semester.IsActive || date.Date < a.Semester.StartDate.Date || date.Date > a.Semester.EndDate.Date)
            return "Ngày học phải thuộc học kỳ đang mở.";
        if (entry.DayOfWeek is < 2 or > 7 || entry.DayOfWeek != (int)date.DayOfWeek + 1)
            return "Ngày đã chọn không trùng thứ của tiết học.";
        if (a.Class.AcademicYearId != a.Semester.AcademicYearId) return "Lớp và học kỳ không thuộc cùng năm học.";
        if (a.Teacher.Status != "Active" || !a.Teacher.User.IsActive || !a.Subject.IsActive ||
            a.Status is not ("Active" or "Assigned") || entry.Room.Status == "Maintenance")
            return "Phân công, giáo viên, môn hoặc phòng học không còn hợp lệ.";
        return null;
    }

    private Task<List<int>> Roster(int classId, DateTime date, TimeSpan start)
    {
        var at = AttendanceRules.StartsAt(date, start).UtcDateTime;
        return repository.Memberships.Where(m => m.ClassId == classId && m.JoinedAt <= at &&
            (m.LeftAt == null || m.LeftAt > at)).Select(m => m.StudentId).Distinct().OrderBy(id => id).ToListAsync();
    }
    private static bool Overlaps(AttendanceSession s, AttendanceSession other) =>
        other.AttendanceDate == s.AttendanceDate && other.StartTime < s.EndTime && s.StartTime < other.EndTime &&
        (other.ClassId == s.ClassId || other.AssignedTeacherId == s.AssignedTeacherId || other.RoomId == s.RoomId);
    private Task<List<LeaveRequest>> ApprovedLeaves(int[] ids, DateTime date) =>
        repository.LeaveRequests.AsNoTracking().Where(l => ids.Contains(l.StudentId) && l.Status == "Approved" &&
            l.StartDate <= date.Date && l.EndDate >= date.Date).OrderBy(l => l.Id).ToListAsync();
    private Task<List<AttendanceOption>> SemesterOptions() => repository.Semesters.AsNoTracking()
        .OrderByDescending(s => s.StartDate).ThenByDescending(s => s.Id)
        .Select(s => new AttendanceOption(s.Id, s.SemesterName + " · " + s.AcademicYear.YearCode + " (" + s.SemesterCode + ")" + (s.IsActive ? " — Đang mở" : " — Đã đóng"))).ToListAsync();
    private Task<List<AttendanceOption>> ClassOptions(int? semesterId) => repository.Classes.AsNoTracking()
        .Where(c => !semesterId.HasValue || repository.Semesters.Any(s => s.Id == semesterId && s.AcademicYearId == c.AcademicYearId))
        .OrderBy(c => c.ClassName).Select(c => new AttendanceOption(c.Id, c.ClassName)).ToListAsync();
    private void Normalize(AttendanceFilter f)
    {
        f.FromDate = (f.FromDate ?? AttendanceRules.Today(Now).AddDays(-30)).Date;
        f.ToDate = (f.ToDate ?? AttendanceRules.Today(Now)).Date;
        if (f.ToDate < f.FromDate) (f.FromDate, f.ToDate) = (f.ToDate, f.FromDate);
    }
    private static int ClampPage(int page, int total) => Math.Clamp(page, 1, Math.Max(1, (int)Math.Ceiling(total / 25m)));
    private static IQueryable<AttendanceSession> FilterSessions(IQueryable<AttendanceSession> q, AttendanceFilter f) =>
        q.Where(s => s.AttendanceDate >= f.FromDate && s.AttendanceDate <= f.ToDate &&
            (!f.SemesterId.HasValue || s.SemesterId == f.SemesterId) &&
            (!f.ClassId.HasValue || s.ClassId == f.ClassId));
    private AttendanceSessionDto Map(AttendanceSession s, bool teacher, bool staff = false) => new()
    {
        Id = s.Id, TimetableEntryId = s.TimetableEntryId, Date = s.AttendanceDate,
        ClassName = s.Class.ClassName, SubjectName = s.Subject.SubjectName, TeacherName = s.AssignedTeacher.User.FullName,
        RoomName = s.Room.RoomName, Slot = $"{(s.SchoolSession == "Morning" ? "Sáng" : "Chiều")} · Tiết {s.PeriodNo} ({s.SlotCode})",
        StartTime = s.StartTime, EndTime = s.EndTime, Status = s.Status, StudentCount = s.Records.Count,
        CanWrite = (teacher || staff && s.Status == AttendanceStatuses.Submitted && s.Records.Count > 0 && s.Records.All(r => r.RecordedBy.HasValue && AttendanceStatuses.IsResult(r.Status))) && s.Status != AttendanceStatuses.Cancelled && s.Semester.IsActive &&
            AttendanceRules.CanWrite(s.AttendanceDate, s.StartTime, Now),
        RowVersion = Convert.ToBase64String(s.RowVersion)
    };
    private AttendanceSession NewSession(TimetableEntry e, DateTime date)
    {
        var session = new AttendanceSession { TimetableEntryId = e.Id, AttendanceDate = date.Date,
            CreatedAt = Now.UtcDateTime, UpdatedAt = Now.UtcDateTime };
        ApplySnapshot(session, e);
        return session;
    }
    private static void ApplySnapshot(AttendanceSession s, TimetableEntry e)
    {
        var a = e.TeachingAssignment;
        s.ClassId = a.ClassId; s.Class = a.Class; s.SubjectId = a.SubjectId; s.Subject = a.Subject;
        s.SemesterId = a.SemesterId; s.Semester = a.Semester; s.AssignedTeacherId = a.TeacherId; s.AssignedTeacher = a.Teacher;
        s.TimeSlotId = e.TimeSlotId; s.RoomId = e.RoomId; s.Room = e.Room;
        s.SlotCode = e.TimeSlot.SlotCode; s.PeriodNo = e.TimeSlot.PeriodNo; s.SchoolSession = e.TimeSlot.Session;
        s.StartTime = e.TimeSlot.StartTime; s.EndTime = e.TimeSlot.EndTime;
    }
    private static bool MatchesSchedule(AttendanceSession s, TimetableEntry e) =>
        s.ClassId == e.TeachingAssignment.ClassId && s.SubjectId == e.TeachingAssignment.SubjectId &&
        s.SemesterId == e.TeachingAssignment.SemesterId && s.AssignedTeacherId == e.TeachingAssignment.TeacherId &&
        s.RoomId == e.RoomId && s.TimeSlotId == e.TimeSlotId && s.StartTime == e.TimeSlot.StartTime &&
        s.EndTime == e.TimeSlot.EndTime && s.SlotCode == e.TimeSlot.SlotCode &&
        s.PeriodNo == e.TimeSlot.PeriodNo && s.SchoolSession == e.TimeSlot.Session &&
        e.DayOfWeek == (int)s.AttendanceDate.DayOfWeek + 1 &&
        e.TeachingAssignment.Status is "Active" or "Assigned";
    private static AttendanceRecord NewRecord(AttendanceSession s, int studentId) => new()
    { StudentId = studentId, TimetableEntryId = s.TimetableEntryId, AttendanceDate = s.AttendanceDate, AttendanceSession = s };
    private bool CheckVersion(AttendanceSession s, string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length > 100) return false;
        try
        {
            var version = Convert.FromBase64String(value);
            if (!version.SequenceEqual(s.RowVersion)) return false;
            repository.ExpectVersion(s, version);
            return true;
        }
        catch (FormatException) { return false; }
    }
    private string ReadOnlyReason(AttendanceSession s, bool staff)
    {
        if (staff && s.Status != AttendanceStatuses.Submitted) return "Staff chỉ được sửa sau khi giáo viên đã thực hiện điểm danh tiết học này.";
        if (s.Status == AttendanceStatuses.Cancelled) return "Bảng điểm danh đã hủy.";
        if (!s.Semester.IsActive) return "Học kỳ đã đóng, chỉ được xem kết quả.";
        if (Now >= AttendanceRules.Deadline(s.AttendanceDate)) return "Đã qua 00:00 ngày kế tiếp. Bảng điểm danh chỉ được xem và không thể chỉnh sửa.";
        return "Chưa đến giờ bắt đầu tiết học.";
    }
    private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    private void Audit(AttendanceSession session, int? actor, string action, object data) =>
        repository.AddAudit(new AttendanceAuditLog { AttendanceSession = session, ActorUserId = actor,
            Action = action, Details = JsonSerializer.Serialize(data), OccurredAt = Now.UtcDateTime });

    private async Task<ServiceResult> Write(Func<Task<ServiceResult>> action, Func<DateTimeOffset?>? deadline = null)
    {
        try
        {
            await using var transaction = await repository.BeginTransactionAsync();
            var result = await action();
            if (!result.Success) { repository.ClearTracking(); return result; }
            if (deadline?.Invoke() is { } before && Now >= before)
            { repository.ClearTracking(); return Invalid("Đã đến 00:00 ngày kế tiếp. Không thể lưu điểm danh."); }
            await repository.SaveAsync();
            if (deadline?.Invoke() is { } after && Now >= after)
            { repository.ClearTracking(); return Invalid("Đã đến 00:00 ngày kế tiếp. Thay đổi chưa được lưu."); }
            await transaction.CommitAsync();
            return result;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            repository.ClearTracking();
            logger.LogWarning(ex, "Attendance concurrent update");
            return Invalid("Bảng đã được cập nhật bởi thao tác khác. Vui lòng tải lại.");
        }
        // SQL Server's execution strategy wraps transient failures (including deadlocks)
        // in InvalidOperationException. Return a retryable form error after rollback too.
        catch (Exception ex) when (ex is DbUpdateException or DbException ||
            ex is InvalidOperationException && ex.GetBaseException() is DbException)
        {
            repository.ClearTracking();
            logger.LogWarning(ex, "Attendance transaction failed");
            return ServiceResult.Fail(ErrorCodes.DatabaseError, "Không thể lưu: dữ liệu trùng hoặc vừa thay đổi. Vui lòng tải lại và kiểm tra.");
        }
    }
}
