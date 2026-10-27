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
            Items = sessions.Select(s => Map(s, Owns(actor, s))).ToList(),
            Semesters = await SemesterOptions(), Classes = await ClassOptions(filter.SemesterId)
        });
    }

    public async Task<ServiceResult<AttendanceCreateDto>> PreviewAsync(int userId, AttendanceCreateDto input)
    {
        if (!IsStaff(await repository.GetActorAsync(userId))) return Failure<AttendanceCreateDto>(Forbidden());
        input.Date = input.Date == default ? AttendanceRules.Today(Now) : input.Date.Date;
        input.SemesterId ??= await repository.Semesters.Where(s => s.IsActive).Select(s => (int?)s.Id).FirstOrDefaultAsync();
        input.Semesters = await SemesterOptions();
        input.Classes = await ClassOptions(input.SemesterId);
        input.Entries = [];
        input.EmptyMessage = null;
        if (!input.SemesterId.HasValue)
        {
            input.EmptyMessage = "Chưa có học kỳ đang mở. Mở học kỳ trong Quản lý năm học trước khi tạo bảng điểm danh.";
            return ServiceResult<AttendanceCreateDto>.Ok(input);
        }
        var day = (int)input.Date.DayOfWeek + 1;
        var schedule = repository.Entries.AsNoTracking()
            .Where(e => e.TeachingAssignment.SemesterId == input.SemesterId
                && (!input.ClassId.HasValue || e.TeachingAssignment.ClassId == input.ClassId));
        var entries = await schedule.Where(e => e.DayOfWeek == day).ToListAsync();
        if (entries.Count == 0)
        {
            input.EmptyMessage = await schedule.AnyAsync()
                ? $"Không có tiết học vào ngày {input.Date:dd/MM/yyyy} trong học kỳ/lớp đã chọn. Chọn ngày có lịch học hoặc kiểm tra thời khóa biểu."
                : "Chưa có thời khóa biểu cho học kỳ/lớp đã chọn. Vào Quản lý thời khóa biểu, chọn giáo viên và thêm tiết học, sau đó quay lại tạo bảng điểm danh.";
            return ServiceResult<AttendanceCreateDto>.Ok(input);
        }
        var existing = await repository.Sessions.AsNoTracking().Where(s => s.AttendanceDate == input.Date)
            .ToDictionaryAsync(s => s.TimetableEntryId);
        foreach (var entry in entries.OrderBy(e => e.TimeSlot.StartTime).ThenBy(e => e.TeachingAssignment.Class.ClassName))
        {
            if (existing.TryGetValue(entry.Id, out var session))
            {
                var dto = Map(session, false);
                dto.StudentCount = await repository.Records.CountAsync(r => r.AttendanceSessionId == session.Id);
                input.Entries.Add(dto);
                continue;
            }
            var preview = NewSession(entry, input.Date, userId);
            var item = Map(preview, false);
            item.CanWrite = ValidateEntry(entry, input.Date) == null;
            item.StudentCount = (await Roster(entry.TeachingAssignment.ClassId, input.Date, entry.TimeSlot.StartTime)).Count;
            input.Entries.Add(item);
        }
        return ServiceResult<AttendanceCreateDto>.Ok(input);
    }

    public Task<ServiceResult> CreateAsync(int userId, AttendanceCreateDto input) => Write(async () =>
    {
        if (!IsStaff(await repository.GetActorAsync(userId))) return Forbidden();
        var ids = input.EntryIds.Distinct().ToArray();
        if (ids.Length == 0 || ids.Length > 100) return Invalid("Chọn từ 1 đến 100 tiết học.");
        var entries = await repository.Entries
            .Include(e => e.TeachingAssignment).ThenInclude(ta => ta.Teacher)
            .Include(e => e.TeachingAssignment).ThenInclude(ta => ta.Class)
            .Include(e => e.TeachingAssignment).ThenInclude(ta => ta.Subject)
            .Where(e => ids.Contains(e.Id))
            .ToListAsync();
        if (entries.Count != ids.Length) return Invalid("Có tiết học không còn tồn tại. Vui lòng tải lại.");
        var created = 0;
        var skipped = 0;
        var pending = new List<AttendanceSession>();
        foreach (var entry in entries)
        {
            if (input.SemesterId != entry.TeachingAssignment.SemesterId ||
                (input.ClassId.HasValue && input.ClassId != entry.TeachingAssignment.ClassId))
                return Invalid("Tiết học không thuộc học kỳ/lớp đang chọn.");
            var error = ValidateEntry(entry, input.Date);
            if (error != null) return Invalid(error);
            if (await repository.Sessions.AnyAsync(s => s.TimetableEntryId == entry.Id && s.AttendanceDate == input.Date.Date))
            { skipped++; continue; }
            var roster = await Roster(entry.TeachingAssignment.ClassId, input.Date, entry.TimeSlot.StartTime);
            if (roster.Count == 0) return Invalid("Lớp " + entry.TeachingAssignment.Class.ClassName + " chưa có học sinh tại thời điểm tiết học.");
            var session = NewSession(entry, input.Date, userId);
            if (await HasSessionConflict(session) || pending.Any(other => Overlaps(session, other)))
                return Invalid("Lớp, giáo viên hoặc phòng đã có bảng điểm danh trùng giờ trong ngày này.");
            foreach (var studentId in roster) session.Records.Add(NewRecord(session, studentId));
            repository.Add(session);
            pending.Add(session);
            Audit(session, userId, "Create", new { Students = roster });
            created++;
        }

        // Bắn thông báo tới Giáo viên được phân công điểm danh
        if (pending.Any())
        {
            foreach (var s in pending)
            {
                var matchEntry = entries.FirstOrDefault(e => e.Id == s.TimetableEntryId);
                var teacherUserId = matchEntry?.TeachingAssignment?.Teacher?.UserId;
                if (teacherUserId.HasValue && teacherUserId.Value > 0)
                {
                    var clsName = matchEntry?.TeachingAssignment?.Class?.ClassName ?? "Lớp";
                    var subName = matchEntry?.TeachingAssignment?.Subject?.SubjectName ?? "Môn học";
                    _ = notificationService.SendNotificationAsync(
                        userId,
                        teacherUserId.Value,
                        $"Bảng điểm danh mới - {subName} ({clsName})",
                        $"Bảng điểm danh lớp {clsName} tiết {s.PeriodNo} môn {subName} ngày {s.AttendanceDate:dd/MM/yyyy} đã được tạo. Thầy/Cô vui lòng vào điểm danh đúng giờ."
                    );
                }
            }
        }

        return ServiceResult.Ok($"Đã tạo {created} bảng điểm danh; {skipped} bảng đã tồn tại.");
    });

    public async Task<ServiceResult<AttendanceSheetDto>> GetSheetAsync(int userId, int sessionId)
    {
        var actor = await repository.GetActorAsync(userId);
        if (!IsStaff(actor) && !IsTeacher(actor)) return Failure<AttendanceSheetDto>(Forbidden());
        var session = await repository.Sessions.AsNoTracking().Include(s => s.Records)
            .ThenInclude(r => r.Student).ThenInclude(s => s.User).SingleOrDefaultAsync(s => s.Id == sessionId);
        if (session == null) return ServiceResult<AttendanceSheetDto>.Fail(ErrorCodes.ResourceNotFound, "Không tìm thấy bảng điểm danh.");
        if (!IsStaff(actor) && !Owns(actor, session)) return Failure<AttendanceSheetDto>(Forbidden());
        var model = new AttendanceSheetDto { Session = Map(session, Owns(actor, session)), IsStaff = IsStaff(actor) };
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
            model.ReadOnlyReason = "Danh sách học sinh hoặc lịch học đã thay đổi. Staff cần đồng bộ bảng trước khi điểm danh.";
        }
        var ids = session.Records.Select(r => r.StudentId).ToArray();
        var leaves = await ApprovedLeaves(ids, session.AttendanceDate);
        model.Students = session.Records.OrderBy(r => r.Student.StudentCode).Select(r => new AttendanceStudentDto
        {
            StudentId = r.StudentId, StudentCode = r.Student.StudentCode, FullName = r.Student.User.FullName,
            Status = r.Status, Comment = r.Comment, LeaveRequestId = r.LeaveRequestId,
            ApprovedLeaveId = leaves.FirstOrDefault(l => l.StudentId == r.StudentId)?.Id
        }).ToList();
        model.Audit = await repository.AuditLogs.AsNoTracking().Where(a => a.AttendanceSessionId == sessionId)
            .OrderByDescending(a => a.Id).Take(20)
            .Select(a => new AttendanceAuditDto(a.OccurredAt, a.ActorUser.FullName, a.Action)).ToListAsync();
        return ServiceResult<AttendanceSheetDto>.Ok(model);
    }

    public Task<ServiceResult> SaveAsync(int userId, AttendanceSaveDto input)
    {
        DateTimeOffset? deadline = null;
        return Write(async () =>
        {
            var actor = await repository.GetActorAsync(userId);
            if (!IsTeacher(actor)) return Forbidden();
            var session = await repository.Sessions
                .Include(s => s.Records)
                .Include(s => s.Class)
                .Include(s => s.Subject)
                .SingleOrDefaultAsync(s => s.Id == input.SessionId);
            if (session == null) return Invalid("Không tìm thấy bảng điểm danh.");
            if (!Owns(actor, session)) return Forbidden();
            deadline = AttendanceRules.Deadline(session.AttendanceDate);
            if (session.Status == AttendanceStatuses.Cancelled || !session.Semester.IsActive ||
                !AttendanceRules.CanWrite(session.AttendanceDate, session.StartTime, Now))
                return Invalid(ReadOnlyReason(session, false));
            if (!CheckVersion(session, input.RowVersion)) return Invalid("Bảng đã thay đổi. Vui lòng tải lại trước khi lưu.");
            if (input.Students.Count != session.Records.Count || input.Students.Count == 0 ||
                input.Students.Select(r => r.StudentId).Distinct().Count() != input.Students.Count ||
                !input.Students.Select(r => r.StudentId).ToHashSet().SetEquals(session.Records.Select(r => r.StudentId)))
                return Invalid("Danh sách gửi lên không khớp bảng điểm danh.");
            if (session.Status == AttendanceStatuses.Open)
            {
                var entry = await repository.Entries.SingleOrDefaultAsync(e => e.Id == session.TimetableEntryId);
                if (entry == null || !MatchesSchedule(session, entry)) return Invalid("Lịch học đã thay đổi. Staff cần đồng bộ bảng.");
                var roster = await Roster(session.ClassId, session.AttendanceDate, session.StartTime);
                if (!roster.ToHashSet().SetEquals(session.Records.Select(r => r.StudentId)))
                    return Invalid("Học sinh đã chuyển lớp hoặc danh sách đã thay đổi. Staff cần đồng bộ bảng.");
            }
            var leaves = await ApprovedLeaves(input.Students.Select(r => r.StudentId).ToArray(), session.AttendanceDate);
            foreach (var row in input.Students)
            {
                if (!AttendanceStatuses.IsResult(row.Status)) return Invalid("Vui lòng chọn kết quả cho tất cả học sinh.");
                if (row.Comment?.Length > 255) return Invalid("Ghi chú không được vượt quá 255 ký tự.");
                if (row.LeaveRequestId.HasValue && (row.Status != AttendanceStatuses.Excused ||
                    !leaves.Any(l => l.Id == row.LeaveRequestId && l.StudentId == row.StudentId)))
                    return Invalid("Đơn nghỉ phải đã được duyệt, thuộc đúng học sinh/ngày và kết quả vắng có phép.");
                if (row.Status == AttendanceStatuses.Excused && !row.LeaveRequestId.HasValue && string.IsNullOrWhiteSpace(row.Comment))
                    return Invalid("Vắng có phép chưa có đơn đã duyệt cần ghi chú lý do.");
            }
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
                record.RecordedBy = actor!.Teacher!.Id;
                record.RecordedAt ??= Now.UtcDateTime;
                record.UpdatedAt = Now.UtcDateTime;
            }
            session.Status = AttendanceStatuses.Submitted;
            session.SubmittedAt ??= Now.UtcDateTime;
            session.UpdatedAt = Now.UtcDateTime;
            Audit(session, userId, wasSubmitted ? "Update" : "Submit", new { Before = before, After = input.Students, Reason = Clean(input.Reason) });

            // Bắn thông báo & Email khi có học sinh Vắng không phép hoặc Đi muộn
            var alertRows = changes
                .Where(c => c.Status is AttendanceStatuses.Absent or AttendanceStatuses.Late)
                .ToList();

            if (alertRows.Any())
            {
                var alertIds = alertRows.Select(c => c.StudentId).ToList();
                var alertStudents = await repository.Students
                    .AsNoTracking()
                    .Include(s => s.User)
                    .Include(s => s.Parent).ThenInclude(p => p!.User)
                    .Where(s => alertIds.Contains(s.Id))
                    .ToListAsync();

                foreach (var st in alertStudents)
                {
                    var row = alertRows.First(c => c.StudentId == st.Id);
                    var statusVi = row.Status == AttendanceStatuses.Absent ? "Vắng không phép" : "Đi muộn";
                    var title = $"Thông báo chuyên cần: {statusVi} - Tiết {session.PeriodNo}";
                    var content = $"Học sinh {st.User.FullName} đã được ghi nhận: {statusVi} trong tiết {session.PeriodNo} (môn {session.Subject?.SubjectName ?? "Môn học"}) ngày {session.AttendanceDate:dd/MM/yyyy}.{(string.IsNullOrWhiteSpace(row.Comment) ? "" : $" Ghi chú: {row.Comment}")}";

                    if (st.UserId > 0)
                    {
                        _ = notificationService.SendNotificationAsync(userId, st.UserId, title, content);
                    }

                    if (st.Parent?.UserId > 0)
                    {
                        _ = notificationService.SendNotificationAsync(userId, st.Parent.UserId, title, content);
                    }

                    var parentEmail = st.Parent?.User?.Email;
                    if (!string.IsNullOrWhiteSpace(parentEmail))
                    {
                        _ = emailService.SendAttendanceAlertEmailAsync(
                            parentEmail,
                            st.Parent?.User?.FullName ?? "Quý phụ huynh",
                            st.User.FullName,
                            session.Class?.ClassName ?? "Lớp",
                            session.Subject?.SubjectName ?? "Môn học",
                            session.PeriodNo,
                            session.AttendanceDate,
                            row.Status,
                            row.Comment
                        );
                    }
                }
            }

            return ServiceResult.Ok(wasSubmitted ? "Đã cập nhật điểm danh." : "Đã xác nhận điểm danh.");
        }, () => deadline);
    }

    public Task<ServiceResult> ManageSessionAsync(int userId, int sessionId, string rowVersion, string action, string? reason) => Write(async () =>
    {
        if (!IsStaff(await repository.GetActorAsync(userId))) return Forbidden();
        if (action is not ("Refresh" or "Cancel" or "Restore")) return Invalid("Thao tác không hợp lệ.");
        if (string.IsNullOrWhiteSpace(reason) || reason.Length > 500) return Invalid("Nhập lý do (tối đa 500 ký tự).");
        var session = await repository.Sessions.Include(s => s.Records).SingleOrDefaultAsync(s => s.Id == sessionId);
        if (session == null) return Invalid("Không tìm thấy bảng điểm danh.");
        if (!CheckVersion(session, rowVersion)) return Invalid("Bảng đã thay đổi. Vui lòng tải lại.");
        if (session.Status == AttendanceStatuses.Submitted || Now >= AttendanceRules.Deadline(session.AttendanceDate))
            return Invalid("Bảng đã chốt hoặc đã qua 00:00 ngày kế tiếp, chỉ được xem.");
        if (!session.Semester.IsActive) return Invalid("Học kỳ đã đóng.");
        var oldIds = session.Records.Select(r => r.StudentId).Order().ToArray();
        if (action == "Cancel")
        {
            if (session.Status != AttendanceStatuses.Open) return Invalid("Chỉ hủy bảng chưa điểm danh.");
            session.Status = AttendanceStatuses.Cancelled;
        }
        else
        {
            if ((action == "Restore") != (session.Status == AttendanceStatuses.Cancelled))
                return Invalid("Trạng thái bảng không phù hợp.");
            var entry = await repository.Entries.SingleOrDefaultAsync(e => e.Id == session.TimetableEntryId);
            if (entry == null) return Invalid("Tiết học không còn tồn tại.");
            var error = ValidateEntry(entry, session.AttendanceDate);
            if (error != null) return Invalid(error);
            var ids = await Roster(entry.TeachingAssignment.ClassId, session.AttendanceDate, entry.TimeSlot.StartTime);
            if (ids.Count == 0) return Invalid("Lớp chưa có học sinh tại thời điểm tiết học.");
            ApplySnapshot(session, entry);
            if (await HasSessionConflict(session))
                return Invalid("Lịch mới trùng giờ với bảng điểm danh khác của lớp, giáo viên hoặc phòng.");
            var removed = session.Records.Where(r => !ids.Contains(r.StudentId)).ToArray();
            repository.RemoveRecords(removed);
            foreach (var row in removed) session.Records.Remove(row);
            foreach (var id in ids.Except(session.Records.Select(r => r.StudentId)).ToArray()) session.Records.Add(NewRecord(session, id));
            session.Status = AttendanceStatuses.Open;
        }
        session.UpdatedAt = Now.UtcDateTime;
        Audit(session, userId, action, new { Reason = reason.Trim(), Before = oldIds, After = session.Records.Select(r => r.StudentId).ToArray() });
        return ServiceResult.Ok("Đã cập nhật bảng điểm danh.");
    });

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
    private Task<bool> HasSessionConflict(AttendanceSession s) => repository.Sessions.AnyAsync(other =>
        other.Id != s.Id && other.AttendanceDate == s.AttendanceDate && other.Status != AttendanceStatuses.Cancelled &&
        other.StartTime < s.EndTime && s.StartTime < other.EndTime &&
        (other.ClassId == s.ClassId || other.AssignedTeacherId == s.AssignedTeacherId || other.RoomId == s.RoomId));
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
    private AttendanceSessionDto Map(AttendanceSession s, bool teacher) => new()
    {
        Id = s.Id, TimetableEntryId = s.TimetableEntryId, Date = s.AttendanceDate,
        ClassName = s.Class.ClassName, SubjectName = s.Subject.SubjectName, TeacherName = s.AssignedTeacher.User.FullName,
        RoomName = s.Room.RoomName, Slot = $"{(s.SchoolSession == "Morning" ? "Sáng" : "Chiều")} · Tiết {s.PeriodNo} ({s.SlotCode})",
        StartTime = s.StartTime, EndTime = s.EndTime, Status = s.Status, StudentCount = s.Records.Count,
        CanWrite = teacher && s.Status != AttendanceStatuses.Cancelled && s.Semester.IsActive &&
            AttendanceRules.CanWrite(s.AttendanceDate, s.StartTime, Now),
        RowVersion = Convert.ToBase64String(s.RowVersion)
    };
    private AttendanceSession NewSession(TimetableEntry e, DateTime date, int userId)
    {
        var session = new AttendanceSession { TimetableEntryId = e.Id, AttendanceDate = date.Date,
            CreatedByUserId = userId, CreatedAt = Now.UtcDateTime, UpdatedAt = Now.UtcDateTime };
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
        s.EndTime == e.TimeSlot.EndTime && e.DayOfWeek == (int)s.AttendanceDate.DayOfWeek + 1 &&
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
        if (staff) return "Staff quản lý bảng; giáo viên phụ trách thực hiện điểm danh.";
        if (s.Status == AttendanceStatuses.Cancelled) return "Bảng điểm danh đã hủy.";
        if (!s.Semester.IsActive) return "Học kỳ đã đóng, chỉ được xem kết quả.";
        if (Now >= AttendanceRules.Deadline(s.AttendanceDate)) return "Đã qua 00:00 ngày kế tiếp. Bảng điểm danh chỉ được xem và không thể chỉnh sửa.";
        return "Chưa đến giờ bắt đầu tiết học.";
    }
    private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    private void Audit(AttendanceSession session, int actor, string action, object data) =>
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
