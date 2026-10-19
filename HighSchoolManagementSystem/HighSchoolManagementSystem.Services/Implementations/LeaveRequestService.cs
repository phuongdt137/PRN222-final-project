using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Repositories.Interfaces;
using HighSchoolManagementSystem.Services.Common;
using HighSchoolManagementSystem.Services.Interfaces;

namespace HighSchoolManagementSystem.Services.Implementations;

public class LeaveRequestService : ILeaveRequestService
{
    private readonly ILeaveRequestRepository _leaveRequestRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IParentRepository _parentRepository;
    private readonly ITeacherRepository _teacherRepository;

    public LeaveRequestService(
        ILeaveRequestRepository leaveRequestRepository,
        IStudentRepository studentRepository,
        IParentRepository parentRepository,
        ITeacherRepository teacherRepository)
    {
        _leaveRequestRepository = leaveRequestRepository;
        _studentRepository = studentRepository;
        _parentRepository = parentRepository;
        _teacherRepository = teacherRepository;
    }

    public async Task<ServiceResult<IReadOnlyList<LeaveRequest>>> GetForStaffAsync(string? status)
    {
        try
        {
            var items = await _leaveRequestRepository.GetByFiltersAsync(status);
            return ServiceResult<IReadOnlyList<LeaveRequest>>.Ok(items);
        }
        catch
        {
            return ServiceResult<IReadOnlyList<LeaveRequest>>.Fail(
                ErrorCodes.DatabaseError, "Không thể tải danh sách đơn xin nghỉ.");
        }
    }

    public async Task<ServiceResult<IReadOnlyList<LeaveRequest>>> GetForTeacherAsync(int teacherUserId, string? status)
    {
        try
        {
            var teacher = await _teacherRepository.GetByUserIdAsync(teacherUserId);
            if (teacher == null)
            {
                return ServiceResult<IReadOnlyList<LeaveRequest>>.Fail(
                    ErrorCodes.TeacherNotFound, "Không tìm thấy hồ sơ giáo viên.");
            }

            var items = await _leaveRequestRepository.GetForHomeroomTeacherAsync(teacher.Id, status);
            return ServiceResult<IReadOnlyList<LeaveRequest>>.Ok(items);
        }
        catch
        {
            return ServiceResult<IReadOnlyList<LeaveRequest>>.Fail(
                ErrorCodes.DatabaseError, "Không thể tải danh sách đơn xin nghỉ của lớp chủ nhiệm.");
        }
    }

    public async Task<ServiceResult<IReadOnlyList<LeaveRequest>>> GetForStudentAsync(int studentUserId, string? status)
    {
        try
        {
            var student = await _studentRepository.GetByUserIdAsync(studentUserId);
            if (student == null)
            {
                return ServiceResult<IReadOnlyList<LeaveRequest>>.Fail(
                    ErrorCodes.StudentNotFound, "Không tìm thấy hồ sơ học sinh.");
            }

            var items = await _leaveRequestRepository.GetByFiltersAsync(status, student.Id);
            return ServiceResult<IReadOnlyList<LeaveRequest>>.Ok(items);
        }
        catch
        {
            return ServiceResult<IReadOnlyList<LeaveRequest>>.Fail(
                ErrorCodes.DatabaseError, "Không thể tải đơn xin nghỉ của bạn.");
        }
    }

    public async Task<ServiceResult<IReadOnlyList<LeaveRequest>>> GetForParentAsync(int parentUserId, string? status)
    {
        try
        {
            var parent = await _parentRepository.GetByUserIdAsync(parentUserId);
            if (parent == null)
            {
                return ServiceResult<IReadOnlyList<LeaveRequest>>.Fail(
                    ErrorCodes.ResourceNotFound, "Không tìm thấy hồ sơ phụ huynh.");
            }

            var studentIds = parent.Students.Select(s => s.Id).ToList();
            var items = await _leaveRequestRepository.GetByStudentIdsAsync(studentIds, status);
            return ServiceResult<IReadOnlyList<LeaveRequest>>.Ok(items);
        }
        catch
        {
            return ServiceResult<IReadOnlyList<LeaveRequest>>.Fail(
                ErrorCodes.DatabaseError, "Không thể tải đơn xin nghỉ của con.");
        }
    }

    public async Task<ServiceResult<LeaveRequest>> GetByIdAsync(int id)
    {
        try
        {
            var item = await _leaveRequestRepository.GetByIdWithDetailsAsync(id);
            if (item == null)
            {
                return ServiceResult<LeaveRequest>.Fail(
                    ErrorCodes.LeaveRequestNotFound, "Không tìm thấy đơn xin nghỉ.");
            }

            return ServiceResult<LeaveRequest>.Ok(item);
        }
        catch
        {
            return ServiceResult<LeaveRequest>.Fail(
                ErrorCodes.DatabaseError, "Không thể tải chi tiết đơn xin nghỉ.");
        }
    }

    public async Task<ServiceResult<LeaveRequest>> CreateAsync(
        int studentId,
        int requestByUserId,
        string reason,
        DateTime startDate,
        DateTime endDate,
        string? attachmentPath)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return ServiceResult<LeaveRequest>.Fail(ErrorCodes.ValidationFailed, "Vui lòng nhập lý do nghỉ.");
        }

        reason = reason.Trim();
        if (reason.Length > 500)
        {
            return ServiceResult<LeaveRequest>.Fail(ErrorCodes.ValidationFailed, "Lý do không được vượt quá 500 ký tự.");
        }

        var start = startDate.Date;
        var end = endDate.Date;
        if (end < start)
        {
            return ServiceResult<LeaveRequest>.Fail(ErrorCodes.ValidationFailed, "Ngày kết thúc phải từ ngày bắt đầu trở đi.");
        }

        if (start < SchoolDate.Today)
        {
            return ServiceResult<LeaveRequest>.Fail(ErrorCodes.ValidationFailed, "Ngày bắt đầu nghỉ không được trước hôm nay.");
        }

        try
        {
            var student = await _studentRepository.GetByIdAsync(studentId);
            if (student == null || !string.Equals(student.Status, "Active", StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult<LeaveRequest>.Fail(
                    ErrorCodes.StudentNotFound, "Không tìm thấy học sinh hợp lệ để tạo đơn.");
            }

            if (await _leaveRequestRepository.HasOverlappingAsync(studentId, start, end))
            {
                return ServiceResult<LeaveRequest>.Fail(
                    ErrorCodes.ValidationFailed,
                    "Học sinh đã có đơn nghỉ (chờ duyệt hoặc đã duyệt) trùng khoảng thời gian này.");
            }

            var leave = new LeaveRequest
            {
                StudentId = studentId,
                RequestBy = requestByUserId,
                Reason = reason,
                Attachment = attachmentPath,
                StartDate = start,
                EndDate = end,
                Status = "Pending",
                RequestedAt = DateTime.UtcNow
            };

            await _leaveRequestRepository.AddAsync(leave);
            await _leaveRequestRepository.SaveChangesAsync();

            var created = await _leaveRequestRepository.GetByIdWithDetailsAsync(leave.Id);
            return ServiceResult<LeaveRequest>.Ok(created!, "Gửi đơn xin nghỉ thành công.");
        }
        catch
        {
            return ServiceResult<LeaveRequest>.Fail(
                ErrorCodes.DatabaseError, "Không thể tạo đơn xin nghỉ. Vui lòng thử lại sau.");
        }
    }

    public async Task<ServiceResult<LeaveRequest>> ReviewAsync(
        int id,
        int reviewerUserId,
        bool approve,
        string? reviewNote,
        bool isStaff,
        int? teacherId)
    {
        try
        {
            var leave = await _leaveRequestRepository.GetTrackedByIdAsync(id);
            if (leave == null)
            {
                return ServiceResult<LeaveRequest>.Fail(
                    ErrorCodes.LeaveRequestNotFound, "Không tìm thấy đơn xin nghỉ.");
            }

            if (!string.Equals(leave.Status, "Pending", StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult<LeaveRequest>.Fail(
                    ErrorCodes.LeaveRequestReviewed, "Đơn nghỉ phép đã được duyệt, không thể thay đổi.");
            }

            if (!isStaff)
            {
                if (!teacherId.HasValue)
                {
                    return ServiceResult<LeaveRequest>.Fail(
                        ErrorCodes.AuthForbidden, "Bạn không có quyền duyệt đơn này.");
                }

                var detailed = await _leaveRequestRepository.GetByIdWithDetailsAsync(id);
                var isHomeroom = detailed?.Student.StudentClasses.Any(sc =>
                    sc.Status == "Enrolled" &&
                    sc.LeftAt == null &&
                    sc.Class.HomeroomTeacherId == teacherId.Value) == true;

                if (!isHomeroom)
                {
                    return ServiceResult<LeaveRequest>.Fail(
                        ErrorCodes.AuthForbidden, "Bạn chỉ được duyệt đơn của học sinh lớp chủ nhiệm.");
                }
            }

            leave.Status = approve ? "Approved" : "Rejected";
            leave.ReviewNote = string.IsNullOrWhiteSpace(reviewNote) ? null : reviewNote.Trim();
            leave.ReviewAt = DateTime.UtcNow;
            leave.ReviewedBy = reviewerUserId;

            await _leaveRequestRepository.SaveChangesAsync();

            var updated = await _leaveRequestRepository.GetByIdWithDetailsAsync(id);
            return ServiceResult<LeaveRequest>.Ok(
                updated!,
                approve ? "Đã duyệt đơn xin nghỉ." : "Đã từ chối đơn xin nghỉ.");
        }
        catch
        {
            return ServiceResult<LeaveRequest>.Fail(
                ErrorCodes.DatabaseError, "Không thể cập nhật trạng thái đơn xin nghỉ.");
        }
    }

    public async Task<ServiceResult<bool>> CancelAsync(int id, int requestByUserId)
    {
        try
        {
            var leave = await _leaveRequestRepository.GetTrackedByIdAsync(id);
            if (leave == null)
            {
                return ServiceResult<bool>.Fail(
                    ErrorCodes.LeaveRequestNotFound, "Không tìm thấy đơn xin nghỉ.");
            }

            if (leave.RequestBy != requestByUserId)
            {
                return ServiceResult<bool>.Fail(
                    ErrorCodes.AuthForbidden, "Bạn chỉ có thể hủy đơn do mình gửi.");
            }

            if (!string.Equals(leave.Status, "Pending", StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult<bool>.Fail(
                    ErrorCodes.LeaveRequestReviewed, "Đơn đã được duyệt/từ chối, không thể hủy.");
            }

            leave.Status = "Cancelled";
            leave.ReviewNote = "Người gửi đã hủy đơn.";
            leave.ReviewAt = DateTime.UtcNow;
            leave.ReviewedBy = requestByUserId;

            await _leaveRequestRepository.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true, "Đã hủy đơn xin nghỉ.");
        }
        catch
        {
            return ServiceResult<bool>.Fail(
                ErrorCodes.DatabaseError, "Không thể hủy đơn xin nghỉ.");
        }
    }

    public async Task<ServiceResult<IReadOnlyList<Student>>> GetStudentsForParentAsync(int parentUserId)
    {
        try
        {
            var parent = await _parentRepository.GetByUserIdAsync(parentUserId);
            if (parent == null)
            {
                return ServiceResult<IReadOnlyList<Student>>.Fail(
                    ErrorCodes.ResourceNotFound, "Không tìm thấy hồ sơ phụ huynh.");
            }

            var students = parent.Students
                .Where(s => string.Equals(s.Status, "Active", StringComparison.OrdinalIgnoreCase))
                .OrderBy(s => s.StudentCode)
                .ToList();

            return ServiceResult<IReadOnlyList<Student>>.Ok(students);
        }
        catch
        {
            return ServiceResult<IReadOnlyList<Student>>.Fail(
                ErrorCodes.DatabaseError, "Không thể tải danh sách học sinh của phụ huynh.");
        }
    }
}
