using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Repositories.Interfaces;
using HighSchoolManagementSystem.Services.Common;
using HighSchoolManagementSystem.Services.Interfaces;

namespace HighSchoolManagementSystem.Services.Implementations;

public class TimeSlotService : ITimeSlotService
{
    private static readonly HashSet<string> AllowedSessions = new(StringComparer.OrdinalIgnoreCase)
    {
        "Morning", "Afternoon"
    };

    private readonly ITimeSlotRepository _timeSlotRepository;

    public TimeSlotService(ITimeSlotRepository timeSlotRepository)
    {
        _timeSlotRepository = timeSlotRepository;
    }

    public async Task<ServiceResult<IReadOnlyList<TimeSlot>>> GetByFiltersAsync(string? session)
    {
        try
        {
            var slots = await _timeSlotRepository.GetByFiltersAsync(session);
            return ServiceResult<IReadOnlyList<TimeSlot>>.Ok(slots);
        }
        catch
        {
            return ServiceResult<IReadOnlyList<TimeSlot>>.Fail(
                ErrorCodes.DatabaseError,
                "Không thể tải danh sách tiết học. Vui lòng thử lại sau.");
        }
    }

    public async Task<ServiceResult<TimeSlot>> GetByIdAsync(int id)
    {
        try
        {
            var slot = await _timeSlotRepository.GetByIdAsync(id);
            if (slot == null)
            {
                return ServiceResult<TimeSlot>.Fail(
                    ErrorCodes.TimeSlotNotFound,
                    "Không tìm thấy tiết học.");
            }

            return ServiceResult<TimeSlot>.Ok(slot);
        }
        catch
        {
            return ServiceResult<TimeSlot>.Fail(
                ErrorCodes.DatabaseError,
                "Không thể tải thông tin tiết học. Vui lòng thử lại sau.");
        }
    }

    public async Task<ServiceResult<TimeSlot>> CreateAsync(
        string slotCode,
        int periodNo,
        TimeSpan startTime,
        TimeSpan endTime,
        string session)
    {
        var validation = ValidateInput(slotCode, periodNo, startTime, endTime, session);
        if (validation != null)
        {
            return validation;
        }

        slotCode = slotCode.Trim();
        session = NormalizeSession(session);

        try
        {
            if (await _timeSlotRepository.ExistsByCodeAsync(slotCode))
            {
                return ServiceResult<TimeSlot>.Fail(
                    ErrorCodes.TimeSlotCodeDuplicate,
                    $"Mã tiết \"{slotCode}\" đã tồn tại.");
            }

            if (await _timeSlotRepository.ExistsByPeriodInSessionAsync(periodNo, session))
            {
                return ServiceResult<TimeSlot>.Fail(
                    ErrorCodes.TimeSlotPeriodDuplicate,
                    $"Tiết số {periodNo} đã tồn tại trong buổi {(session == "Morning" ? "sáng" : "chiều")}.");
            }

            var timeSlot = new TimeSlot
            {
                SlotCode = slotCode,
                PeriodNo = periodNo,
                StartTime = startTime,
                EndTime = endTime,
                Session = session
            };

            await _timeSlotRepository.AddAsync(timeSlot);
            await _timeSlotRepository.SaveChangesAsync();

            return ServiceResult<TimeSlot>.Ok(timeSlot, "Tạo tiết học thành công.");
        }
        catch
        {
            return ServiceResult<TimeSlot>.Fail(
                ErrorCodes.DatabaseError,
                "Không thể tạo tiết học. Vui lòng thử lại sau.");
        }
    }

    public async Task<ServiceResult<TimeSlot>> UpdateAsync(
        int id,
        string slotCode,
        int periodNo,
        TimeSpan startTime,
        TimeSpan endTime,
        string session)
    {
        var validation = ValidateInput(slotCode, periodNo, startTime, endTime, session);
        if (validation != null)
        {
            return validation;
        }

        slotCode = slotCode.Trim();
        session = NormalizeSession(session);

        try
        {
            var timeSlot = await _timeSlotRepository.GetTrackedByIdAsync(id);
            if (timeSlot == null)
            {
                return ServiceResult<TimeSlot>.Fail(
                    ErrorCodes.TimeSlotNotFound,
                    "Không tìm thấy tiết học cần cập nhật.");
            }

            if (await _timeSlotRepository.ExistsByCodeAsync(slotCode, excludeId: id))
            {
                return ServiceResult<TimeSlot>.Fail(
                    ErrorCodes.TimeSlotCodeDuplicate,
                    $"Mã tiết \"{slotCode}\" đã tồn tại.");
            }

            if (await _timeSlotRepository.ExistsByPeriodInSessionAsync(periodNo, session, excludeId: id))
            {
                return ServiceResult<TimeSlot>.Fail(
                    ErrorCodes.TimeSlotPeriodDuplicate,
                    $"Tiết số {periodNo} đã tồn tại trong buổi {(session == "Morning" ? "sáng" : "chiều")}.");
            }

            timeSlot.SlotCode = slotCode;
            timeSlot.PeriodNo = periodNo;
            timeSlot.StartTime = startTime;
            timeSlot.EndTime = endTime;
            timeSlot.Session = session;

            await _timeSlotRepository.SaveChangesAsync();

            return ServiceResult<TimeSlot>.Ok(timeSlot, "Cập nhật tiết học thành công.");
        }
        catch
        {
            return ServiceResult<TimeSlot>.Fail(
                ErrorCodes.DatabaseError,
                "Không thể cập nhật tiết học. Vui lòng thử lại sau.");
        }
    }

    private static ServiceResult<TimeSlot>? ValidateInput(
        string slotCode,
        int periodNo,
        TimeSpan startTime,
        TimeSpan endTime,
        string session)
    {
        if (string.IsNullOrWhiteSpace(slotCode))
        {
            return ServiceResult<TimeSlot>.Fail(ErrorCodes.ValidationFailed, "Vui lòng nhập mã tiết.");
        }

        if (slotCode.Trim().Length > 20)
        {
            return ServiceResult<TimeSlot>.Fail(ErrorCodes.ValidationFailed, "Mã tiết không được vượt quá 20 ký tự.");
        }

        if (periodNo < 1 || periodNo > 20)
        {
            return ServiceResult<TimeSlot>.Fail(ErrorCodes.ValidationFailed, "Số tiết phải từ 1 đến 20.");
        }

        if (endTime <= startTime)
        {
            return ServiceResult<TimeSlot>.Fail(ErrorCodes.ValidationFailed, "Giờ kết thúc phải sau giờ bắt đầu.");
        }

        if (string.IsNullOrWhiteSpace(session) || !AllowedSessions.Contains(session.Trim()))
        {
            return ServiceResult<TimeSlot>.Fail(ErrorCodes.ValidationFailed, "Buổi học không hợp lệ.");
        }

        return null;
    }

    private static string NormalizeSession(string session)
    {
        return AllowedSessions.First(s => s.Equals(session.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
