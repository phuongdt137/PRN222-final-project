using System.Text.Json;
using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Repositories.Interfaces;
using HighSchoolManagementSystem.Services.Common;
using HighSchoolManagementSystem.Services.DTOs;
using HighSchoolManagementSystem.Services.Interfaces;

namespace HighSchoolManagementSystem.Services.Implementations;

public class AttendanceService : IAttendanceService
{
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly TimeProvider _clock;
    private readonly AttendanceAlertSender _alertSender;

    public AttendanceService(
        IAttendanceRepository attendanceRepository,
        TimeProvider clock,
        AttendanceAlertSender alertSender)
    {
        _attendanceRepository = attendanceRepository;
        _clock = clock;
        _alertSender = alertSender;
    }

    private DateTimeOffset Now => _clock.GetUtcNow();

    // Chỉ đọc thời khóa biểu và kết quả đã lưu; không tạo bảng khi mở danh sách.
    public async Task<ServiceResult<AttendanceListDto>> GetSessionsAsync(int userId, AttendanceFilter filter)
    {
        var actor = await _attendanceRepository.GetActorAsync(userId);

        if (!IsStaff(actor) && !IsTeacher(actor))
        {
            return ServiceResult<AttendanceListDto>.Fail(
                ErrorCodes.AuthForbidden, "Bạn không có quyền thực hiện thao tác này.");
        }

        NormalizeFilter(filter);
        int? teacherId = null;

        if (IsTeacher(actor))
        {
            var today = AttendanceRules.Today(Now);
            filter.FromDate = today;
            filter.ToDate = today;
            teacherId = actor!.Teacher!.Id;
        }

        var entries = await _attendanceRepository.GetEntriesAsync(filter.SemesterId, filter.ClassId, teacherId);
        var saved = await _attendanceRepository.GetSessionsAsync(filter.FromDate!.Value, filter.ToDate!.Value);
        var sizes = await _attendanceRepository.GetClassSizesAsync();
        var items = new List<AttendanceSessionDto>();

        for (var date = filter.FromDate!.Value; date <= filter.ToDate!.Value; date = date.AddDays(1))
        {
            foreach (var entry in entries)
            {
                var semester = entry.TeachingAssignment.Semester;

                if (entry.DayOfWeek != (int)date.DayOfWeek + 1 ||
                    date < semester.StartDate.Date || date > semester.EndDate.Date)
                {
                    continue;
                }

                var session = saved.FirstOrDefault(s => s.TimetableEntryId == entry.Id && s.AttendanceDate.Date == date);

                if (session == null)
                {
                    // Đối tượng tạm để hiển thị, chưa lưu xuống DB.
                    session = NewSession(entry, date);
                }

                var item = ToSessionDto(session, actor);
                item.StudentCount = sizes.GetValueOrDefault(entry.TeachingAssignment.ClassId);
                items.Add(item);
            }
        }

        if (!string.IsNullOrEmpty(filter.Status))
        {
            items = items.Where(s => s.Status == filter.Status).ToList();
        }

        filter.Page = GetValidPage(filter.Page, items.Count);

        var model = new AttendanceListDto
        {
            Filter = filter,
            IsStaff = IsStaff(actor),
            TotalCount = items.Count,
            Items = items
                .OrderByDescending(s => s.Date)
                .ThenBy(s => s.StartTime)
                .ThenBy(s => s.TimetableEntryId)
                .Skip((filter.Page - 1) * 25)
                .Take(25)
                .ToList(),
            Semesters = await GetSemesterOptionsAsync(),
            Classes = await GetClassOptionsAsync(filter.SemesterId)
        };

        return ServiceResult<AttendanceListDto>.Ok(model);
    }

    // Mở tiết đã chọn, lấy học sinh hiện tại và ghép kết quả đã lưu nếu có.
    public async Task<ServiceResult<AttendanceSheetDto>> GetSheetAsync(
        int userId,
        int sessionId = 0,
        int? timetableEntryId = null,
        DateTime? date = null)
    {
        var actor = await _attendanceRepository.GetActorAsync(userId);
        var session = await FindSessionAsync(sessionId, timetableEntryId ?? 0, date ?? AttendanceRules.Today(Now));

        if (session == null)
        {
            return ServiceResult<AttendanceSheetDto>.Fail(
                ErrorCodes.ResourceNotFound, "Không tìm thấy tiết học.");
        }

        if (!CanView(actor, session))
        {
            return ServiceResult<AttendanceSheetDto>.Fail(
                ErrorCodes.AuthForbidden, "Bạn không có quyền thực hiện thao tác này.");
        }

        var students = await _attendanceRepository.GetCurrentStudentsAsync(session.ClassId);
        var leaves = await _attendanceRepository.GetApprovedLeavesAsync(students.Select(s => s.Id).ToArray(), session.AttendanceDate);
        var model = new AttendanceSheetDto
        {
            Session = ToSessionDto(session, actor),
            IsStaff = IsStaff(actor),
            ReadOnlyReason = GetEditError(actor, session)
        };

        foreach (var student in students)
        {
            var record = session.Records.FirstOrDefault(r => r.StudentId == student.Id);
            var item = new AttendanceStudentDto
            {
                StudentId = student.Id,
                StudentCode = student.StudentCode,
                FullName = student.User.FullName,
                Status = record?.Status ?? AttendanceStatuses.NotMarked,
                Comment = record?.Comment,
                LeaveRequestId = record?.LeaveRequestId,
                ApprovedLeaves = leaves
                    .Where(l => l.StudentId == student.Id)
                    .Select(l => new AttendanceOption(l.Id, $"Đơn #{l.Id}: {l.Reason}"))
                    .ToList()
            };

            model.Students.Add(item);
        }

        if (IsStaff(actor) && session.Id > 0)
        {
            var logs = await _attendanceRepository.GetAuditAsync(session.Id);

            foreach (var log in logs)
            {
                model.Audit.Add(new AttendanceAuditDto(log.OccurredAt, log.ActorUser?.FullName ?? "Hệ thống", log.Action, log.Details));
            }
        }

        return ServiceResult<AttendanceSheetDto>.Ok(model);
    }

    // CRUD: tìm hoặc tạo bảng -> cập nhật các dòng học sinh -> SaveChanges.
    public async Task<ServiceResult> SaveAsync(int userId, AttendanceSaveDto input)
    {
        var actor = await _attendanceRepository.GetActorAsync(userId);
        var session = await FindSessionAsync(input.SessionId, input.TimetableEntryId, input.Date);

        if (session == null)
        {
            return ServiceResult.Fail(
                ErrorCodes.ValidationFailed, "Không tìm thấy tiết học.");
        }

        if (!CanView(actor, session))
        {
            return ServiceResult.Fail(
                ErrorCodes.AuthForbidden, "Bạn không có quyền thực hiện thao tác này.");
        }

        var editError = GetEditError(actor, session);

        if (editError != null)
        {
            return ServiceResult.Fail(
                ErrorCodes.ValidationFailed, editError);
        }

        var isUpdate = session.Status == AttendanceStatuses.Submitted;

        if (isUpdate && string.IsNullOrWhiteSpace(input.Reason))
        {
            return ServiceResult.Fail(
                ErrorCodes.ValidationFailed, "Vui lòng nhập lý do chỉnh sửa điểm danh.");
        }

        if (input.Reason?.Length > 500)
        {
            return ServiceResult.Fail(
                ErrorCodes.ValidationFailed, "Lý do chỉnh sửa tối đa 500 ký tự.");
        }

        var studentIds = await _attendanceRepository.GetCurrentStudentIdsAsync(session.ClassId);
        var submittedIds = input.Students.Select(r => r.StudentId).Distinct().ToList();
        var hasInvalidResult = input.Students.Any(r =>
            !studentIds.Contains(r.StudentId) || !AttendanceStatuses.IsResult(r.Status));

        if (input.Students.Count == 0 ||
            input.Students.Count != studentIds.Count ||
            submittedIds.Count != studentIds.Count || hasInvalidResult)
        {
            return ServiceResult.Fail(
                ErrorCodes.ValidationFailed, "Vui lòng chọn kết quả cho đủ danh sách học sinh.");
        }

        var leaves = await _attendanceRepository.GetApprovedLeavesAsync(studentIds.ToArray(), session.AttendanceDate);

        foreach (var row in input.Students)
        {
            if (row.Status == AttendanceStatuses.Excused && !leaves.Any(l => l.Id == row.LeaveRequestId && l.StudentId == row.StudentId))
            {
                return ServiceResult.Fail(
                    ErrorCodes.ValidationFailed, "Vắng có phép cần chọn đơn nghỉ đã duyệt của học sinh.");
            }

            if (row.Status == AttendanceStatuses.Absent && leaves.Any(l => l.StudentId == row.StudentId))
            {
                return ServiceResult.Fail(
                    ErrorCodes.ValidationFailed, "Học sinh có đơn nghỉ đã duyệt; vui lòng chọn kết quả phù hợp.");
            }
        }

        editError = GetEditError(actor, session);

        if (editError != null)
        {
            return ServiceResult.Fail(
                ErrorCodes.ValidationFailed, editError);
        }

        var utcNow = Now.UtcDateTime;
        var alertRows = new List<AttendanceSaveRowDto>();

        if (session.Id == 0)
        {
            session.CreatedByUserId = userId;
            _attendanceRepository.Add(session);
        }

        foreach (var row in input.Students)
        {
            var record = session.Records.FirstOrDefault(r => r.StudentId == row.StudentId);

            if (row.Status == AttendanceStatuses.Absent && record?.Status != AttendanceStatuses.Absent)
            {
                alertRows.Add(row);
            }

            if (record == null)
            {
                record = new AttendanceRecord
                {
                    StudentId = row.StudentId,
                    TimetableEntryId = session.TimetableEntryId,
                    AttendanceDate = session.AttendanceDate,
                    AttendanceSession = session
                };

                session.Records.Add(record);
            }

            record.Status = row.Status;
            record.Comment = string.IsNullOrWhiteSpace(row.Comment) ? null : row.Comment.Trim();
            record.LeaveRequestId = row.Status == AttendanceStatuses.Excused ? row.LeaveRequestId : null;
            record.RecordedBy ??= session.AssignedTeacherId;
            record.RecordedAt ??= utcNow;
            record.UpdatedAt = utcNow;
        }

        session.Status = AttendanceStatuses.Submitted;
        session.SubmittedAt ??= utcNow;
        session.UpdatedAt = utcNow;

        var log = new AttendanceAuditLog
        {
            AttendanceSession = session,
            ActorUserId = userId,
            Action = isUpdate ? "Update" : "Submit",
            OccurredAt = utcNow,
            Details = JsonSerializer.Serialize(new { Reason = input.Reason?.Trim() })
        };

        _attendanceRepository.AddAudit(log);
        await _attendanceRepository.SaveAsync();

        if (alertRows.Count > 0)
        {
            var delivered = await _alertSender.SendAsync(userId, session, alertRows);

            if (!delivered)
            {
                return ServiceResult.Ok("Đã lưu điểm danh. Một số thông báo/email chưa gửi được; vui lòng kiểm tra nhật ký hệ thống.");
            }
        }

        return ServiceResult.Ok("Đã lưu điểm danh.");
    }

    // Học sinh xem chính mình; phụ huynh chỉ xem các con đã liên kết.
    public async Task<ServiceResult<AttendanceStudentHistoryDto>> GetHistoryAsync(int userId, AttendanceFilter filter)
    {
        var actor = await _attendanceRepository.GetActorAsync(userId);

        if (actor == null || (actor.Role.RoleName != "Student" && actor.Role.RoleName != "Parent"))
        {
            return ServiceResult<AttendanceStudentHistoryDto>.Fail(
                ErrorCodes.AuthForbidden, "Bạn không có quyền thực hiện thao tác này.");
        }

        var model = new AttendanceStudentHistoryDto
        {
            Filter = filter,
            IsParent = actor.Role.RoleName == "Parent"
        };

        if (model.IsParent)
        {
            if (actor.Parent == null)
            {
                return ServiceResult<AttendanceStudentHistoryDto>.Fail(
                    ErrorCodes.AuthForbidden, "Bạn không có quyền thực hiện thao tác này.");
            }

            var children = await _attendanceRepository.GetChildrenAsync(actor.Parent.ParentId);
            model.Children = children.Select(s => new AttendanceOption(s.Id, s.User.FullName + " (" + s.StudentCode + ")")).ToList();

            if (filter.StudentId.HasValue && !model.Children.Any(c => c.Id == filter.StudentId))
            {
                return ServiceResult<AttendanceStudentHistoryDto>.Fail(
                    ErrorCodes.AuthForbidden, "Bạn không có quyền thực hiện thao tác này.");
            }

            if (!filter.StudentId.HasValue)
            {
                filter.StudentId = model.Children.FirstOrDefault()?.Id;
            }
        }
        else
        {
            if (actor.Student == null || (filter.StudentId.HasValue && filter.StudentId != actor.Student.Id))
            {
                return ServiceResult<AttendanceStudentHistoryDto>.Fail(
                    ErrorCodes.AuthForbidden, "Bạn không có quyền thực hiện thao tác này.");
            }

            filter.StudentId = actor.Student.Id;
        }

        NormalizeFilter(filter);
        model.Semesters = await GetSemesterOptionsAsync();

        if (!filter.StudentId.HasValue)
        {
            return ServiceResult<AttendanceStudentHistoryDto>.Ok(model);
        }

        var studentId = filter.StudentId.Value;
        model.StudentName = await _attendanceRepository.GetStudentNameAsync(studentId);

        var subjects = await _attendanceRepository.GetHistorySubjectsAsync(studentId);
        model.Subjects = subjects.Select(s => new AttendanceOption(s.Id, s.SubjectName)).ToList();

        var counts = await _attendanceRepository.GetHistoryCountsAsync(
            studentId, filter.FromDate!.Value, filter.ToDate!.Value, filter.SemesterId, filter.SubjectId, filter.Status);

        model.Present = counts.GetValueOrDefault(AttendanceStatuses.Present);
        model.Absent = counts.GetValueOrDefault(AttendanceStatuses.Absent);
        model.Late = counts.GetValueOrDefault(AttendanceStatuses.Late);
        model.Excused = counts.GetValueOrDefault(AttendanceStatuses.Excused);
        model.Pending = counts.GetValueOrDefault(AttendanceStatuses.NotMarked);
        model.TotalCount = counts.Values.Sum();
        filter.Page = GetValidPage(filter.Page, model.TotalCount);

        var rows = await _attendanceRepository.GetHistoryRecordsAsync(
            studentId, filter.FromDate!.Value, filter.ToDate!.Value, filter.SemesterId, filter.SubjectId, filter.Status, filter.Page, 25);

        foreach (var record in rows)
        {
            var item = new AttendanceHistoryRowDto
            {
                Session = ToSessionDto(record.AttendanceSession)
            };

            if (record.AttendanceSession.Status == AttendanceStatuses.Submitted)
            {
                item.Status = record.Status;
                item.Comment = record.Comment;
                item.RecordedBy = record.RecordedByTeacher?.User.FullName;
            }

            model.Items.Add(item);
        }

        return ServiceResult<AttendanceStudentHistoryDto>.Ok(model);
    }

    private static bool IsStaff(User? user)
    {
        return user?.Role.RoleName == "Staff";
    }

    private static bool IsTeacher(User? user)
    {
        return user?.Role.RoleName == "Teacher" && user.Teacher != null;
    }

    private bool CanView(User? user, AttendanceSession session)
    {
        if (IsStaff(user))
        {
            return true;
        }

        if (!IsTeacher(user))
        {
            return false;
        }

        return user!.Teacher!.Id == session.AssignedTeacherId && session.AttendanceDate.Date == AttendanceRules.Today(Now);
    }

    private string? GetEditError(User? user, AttendanceSession session)
    {
        if (session.Status == AttendanceStatuses.Cancelled)
        {
            return "Bảng điểm danh đã hủy.";
        }

        if (!session.Semester.IsActive)
        {
            return "Học kỳ đã đóng.";
        }

        if (Now < AttendanceRules.StartsAt(session.AttendanceDate, session.StartTime))
        {
            return "Chưa đến giờ bắt đầu tiết học, chưa được điểm danh.";
        }

        if (Now >= AttendanceRules.Deadline(session.AttendanceDate))
        {
            return "Đã hết hạn điểm danh (00:00 ngày kế tiếp theo giờ Việt Nam).";
        }

        if (IsStaff(user) && session.Status != AttendanceStatuses.Submitted)
        {
            return "Staff chỉ sửa sau khi giáo viên đã lưu điểm danh.";
        }

        return null;
    }

    private async Task<List<AttendanceOption>> GetSemesterOptionsAsync()
    {
        var semesters = await _attendanceRepository.GetSemestersAsync();
        var options = new List<AttendanceOption>();

        foreach (var semester in semesters)
        {
            var name = semester.SemesterName + " · " + semester.AcademicYear.YearCode +
                " (" + semester.SemesterCode + ")";

            name += semester.IsActive ? " — Đang mở" : " — Đã đóng";
            options.Add(new AttendanceOption(semester.Id, name));
        }

        return options;
    }

    private async Task<List<AttendanceOption>> GetClassOptionsAsync(int? semesterId)
    {
        var classes = await _attendanceRepository.GetClassesAsync(semesterId);

        return classes.Select(c => new AttendanceOption(c.Id, c.ClassName)).ToList();
    }

    private void NormalizeFilter(AttendanceFilter filter)
    {
        filter.FromDate = (filter.FromDate ?? AttendanceRules.Today(Now).AddDays(-30)).Date;
        filter.ToDate = (filter.ToDate ?? AttendanceRules.Today(Now)).Date;

        if (filter.ToDate < filter.FromDate)
        {
            var fromDate = filter.FromDate;
            filter.FromDate = filter.ToDate;
            filter.ToDate = fromDate;
        }
    }

    private static int GetValidPage(int page, int total)
    {
        var totalPages = (int)Math.Ceiling(total / 25m);
        totalPages = Math.Max(1, totalPages);

        return Math.Clamp(page, 1, totalPages);
    }

    private async Task<AttendanceSession?> FindSessionAsync(int id, int entryId, DateTime date)
    {
        if (id > 0)
        {
            return await _attendanceRepository.GetSessionAsync(id);
        }

        var saved = await _attendanceRepository.GetSessionByLessonAsync(entryId, date);

        if (saved != null)
        {
            return saved;
        }

        var entry = await _attendanceRepository.GetEntryAsync(entryId);

        if (entry == null)
        {
            return null;
        }

        var semester = entry.TeachingAssignment.Semester;

        if (entry.DayOfWeek != (int)date.DayOfWeek + 1 ||
            date.Date < semester.StartDate.Date || date.Date > semester.EndDate.Date)
        {
            return null;
        }

        return NewSession(entry, date);
    }

    private AttendanceSession NewSession(TimetableEntry entry, DateTime date)
    {
        var assignment = entry.TeachingAssignment;

        return new AttendanceSession
        {
            TimetableEntryId = entry.Id,
            AttendanceDate = date.Date,
            ClassId = assignment.ClassId,
            Class = assignment.Class,
            SubjectId = assignment.SubjectId,
            Subject = assignment.Subject,
            SemesterId = assignment.SemesterId,
            Semester = assignment.Semester,
            AssignedTeacherId = assignment.TeacherId,
            AssignedTeacher = assignment.Teacher,
            RoomId = entry.RoomId,
            Room = entry.Room,
            TimeSlotId = entry.TimeSlotId,
            SlotCode = entry.TimeSlot.SlotCode,
            SchoolSession = entry.TimeSlot.Session,
            PeriodNo = entry.TimeSlot.PeriodNo,
            StartTime = entry.TimeSlot.StartTime,
            EndTime = entry.TimeSlot.EndTime,
            CreatedAt = Now.UtcDateTime
        };

    }

    private AttendanceSessionDto ToSessionDto(AttendanceSession session, User? actor = null)
    {
        return new AttendanceSessionDto
        {
            Id = session.Id,
            TimetableEntryId = session.TimetableEntryId,
            Date = session.AttendanceDate,
            ClassName = session.Class.ClassName,
            SubjectName = session.Subject.SubjectName,
            TeacherName = session.AssignedTeacher.User.FullName,
            RoomName = session.Room.RoomName,
            Slot = $"{(session.SchoolSession == "Morning" ? "Sáng" : "Chiều")} · Tiết {session.PeriodNo} ({session.SlotCode})",
            StartTime = session.StartTime,
            EndTime = session.EndTime,
            Status = session.Status,
            StudentCount = session.Records.Count,
            CanWrite = CanView(actor, session) && GetEditError(actor, session) == null
        };

    }
}
