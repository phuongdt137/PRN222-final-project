using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Repositories.Interfaces;
using HighSchoolManagementSystem.Services.Common;
using HighSchoolManagementSystem.Services.Interfaces;

namespace HighSchoolManagementSystem.Services.Implementations;

public class SemesterService : ISemesterService
{
    private readonly ISemesterRepository _semesterRepository;
    private readonly IAcademicYearRepository _academicYearRepository;

    public SemesterService(
        ISemesterRepository semesterRepository,
        IAcademicYearRepository academicYearRepository)
    {
        _semesterRepository = semesterRepository;
        _academicYearRepository = academicYearRepository;
    }

    public async Task<ServiceResult<IReadOnlyList<Semester>>> GetAllAsync()
    {
        try
        {
            return ServiceResult<IReadOnlyList<Semester>>.Ok(await _semesterRepository.GetAllAsync());
        }
        catch
        {
            return ServiceResult<IReadOnlyList<Semester>>.Fail(ErrorCodes.DatabaseError, "Không thể tải danh sách học kỳ.");
        }
    }

    public async Task<ServiceResult<IReadOnlyList<Semester>>> GetByAcademicYearIdAsync(int academicYearId)
    {
        try
        {
            var year = await _academicYearRepository.GetByIdAsync(academicYearId);
            if (year == null)
            {
                return ServiceResult<IReadOnlyList<Semester>>.Fail(
                    ErrorCodes.ResourceNotFound,
                    "Không tìm thấy năm học.");
            }

            var semesters = await _semesterRepository.GetByAcademicYearIdAsync(academicYearId);
            return ServiceResult<IReadOnlyList<Semester>>.Ok(semesters);
        }
        catch
        {
            return ServiceResult<IReadOnlyList<Semester>>.Fail(
                ErrorCodes.DatabaseError,
                "Không thể tải danh sách học kỳ. Vui lòng thử lại sau.");
        }
    }

    public async Task<ServiceResult<Semester>> GetByIdAsync(int id)
    {
        try
        {
            var semester = await _semesterRepository.GetByIdAsync(id);
            if (semester == null)
            {
                return ServiceResult<Semester>.Fail(
                    ErrorCodes.SemesterNotFound,
                    "Không tìm thấy học kỳ.");
            }

            return ServiceResult<Semester>.Ok(semester);
        }
        catch
        {
            return ServiceResult<Semester>.Fail(
                ErrorCodes.DatabaseError,
                "Không thể tải thông tin học kỳ. Vui lòng thử lại sau.");
        }
    }

    public async Task<ServiceResult<Semester>> CreateAsync(
        int academicYearId,
        string semesterCode,
        string semesterName,
        DateTime startDate,
        DateTime endDate)
    {
        var year = await _academicYearRepository.GetByIdAsync(academicYearId);
        if (year == null)
        {
            return ServiceResult<Semester>.Fail(
                ErrorCodes.ResourceNotFound,
                "Không tìm thấy năm học để gắn học kỳ.");
        }

        var validationError = ValidateInput(semesterCode, semesterName, startDate, endDate, year);
        if (validationError != null)
        {
            return validationError;
        }

        semesterCode = semesterCode.Trim();
        semesterName = semesterName.Trim();

        try
        {
            if (await _semesterRepository.ExistsBySemesterCodeAsync(semesterCode))
            {
                return ServiceResult<Semester>.Fail(
                    ErrorCodes.ResourceAlreadyExists,
                    $"Mã học kỳ \"{semesterCode}\" đã tồn tại trong hệ thống.");
            }

            if (await HasOverlappingSemesterAsync(academicYearId, startDate, endDate))
            {
                return ServiceResult<Semester>.Fail(
                    ErrorCodes.ValidationFailed,
                    "Khoảng thời gian học kỳ bị trùng với một học kỳ khác trong cùng năm học.");
            }

            var semester = new Semester
            {
                AcademicYearId = academicYearId,
                SemesterCode = semesterCode,
                SemesterName = semesterName,
                StartDate = startDate.Date,
                EndDate = endDate.Date,
                IsActive = false
            };

            await _semesterRepository.AddAsync(semester);
            await _semesterRepository.SaveChangesAsync();

            return ServiceResult<Semester>.Ok(semester, "Tạo học kỳ thành công.");
        }
        catch
        {
            return ServiceResult<Semester>.Fail(
                ErrorCodes.DatabaseError,
                "Không thể tạo học kỳ. Vui lòng thử lại sau.");
        }
    }

    public async Task<ServiceResult<Semester>> UpdateAsync(
        int id,
        string semesterCode,
        string semesterName,
        DateTime startDate,
        DateTime endDate)
    {
        try
        {
            var semester = await _semesterRepository.GetTrackedByIdAsync(id);
            if (semester == null)
            {
                return ServiceResult<Semester>.Fail(
                    ErrorCodes.SemesterNotFound,
                    "Không tìm thấy học kỳ cần cập nhật.");
            }

            var year = await _academicYearRepository.GetByIdAsync(semester.AcademicYearId);
            if (year == null)
            {
                return ServiceResult<Semester>.Fail(
                    ErrorCodes.ResourceNotFound,
                    "Không tìm thấy năm học của học kỳ này.");
            }

            var validationError = ValidateInput(semesterCode, semesterName, startDate, endDate, year);
            if (validationError != null)
            {
                return validationError;
            }

            semesterCode = semesterCode.Trim();
            semesterName = semesterName.Trim();

            if (await _semesterRepository.ExistsBySemesterCodeAsync(semesterCode, excludeId: id))
            {
                return ServiceResult<Semester>.Fail(
                    ErrorCodes.ResourceAlreadyExists,
                    $"Mã học kỳ \"{semesterCode}\" đã tồn tại trong hệ thống.");
            }

            if (await HasOverlappingSemesterAsync(semester.AcademicYearId, startDate, endDate, id))
            {
                return ServiceResult<Semester>.Fail(
                    ErrorCodes.ValidationFailed,
                    "Khoảng thời gian học kỳ bị trùng với một học kỳ khác trong cùng năm học.");
            }

            semester.SemesterCode = semesterCode;
            semester.SemesterName = semesterName;
            semester.StartDate = startDate.Date;
            semester.EndDate = endDate.Date;

            await _semesterRepository.SaveChangesAsync();

            return ServiceResult<Semester>.Ok(semester, "Cập nhật học kỳ thành công.");
        }
        catch
        {
            return ServiceResult<Semester>.Fail(
                ErrorCodes.DatabaseError,
                "Không thể cập nhật học kỳ. Vui lòng thử lại sau.");
        }
    }

    public async Task<ServiceResult<Semester>> ActivateAsync(int id)
    {
        try
        {
            var semester = await _semesterRepository.GetTrackedByIdAsync(id);
            if (semester == null)
            {
                return ServiceResult<Semester>.Fail(
                    ErrorCodes.SemesterNotFound,
                    "Không tìm thấy học kỳ cần kích hoạt.");
            }

            if (semester.IsActive)
            {
                return ServiceResult<Semester>.Ok(
                    semester,
                    $"Học kỳ \"{semester.SemesterName}\" đang ở trạng thái mở.");
            }

            await _semesterRepository.ClearActiveFlagsAsync();

            semester.IsActive = true;
            await _semesterRepository.SaveChangesAsync();

            return ServiceResult<Semester>.Ok(
                semester,
                $"Đã kích hoạt học kỳ \"{semester.SemesterName}\".");
        }
        catch
        {
            return ServiceResult<Semester>.Fail(
                ErrorCodes.DatabaseError,
                "Không thể kích hoạt học kỳ. Vui lòng thử lại sau.");
        }
    }

    public async Task<ServiceResult<Semester>> CloseAsync(int id)
    {
        try
        {
            var semester = await _semesterRepository.GetTrackedByIdAsync(id);
            if (semester == null)
            {
                return ServiceResult<Semester>.Fail(
                    ErrorCodes.SemesterNotFound,
                    "Không tìm thấy học kỳ cần đóng.");
            }

            if (!semester.IsActive)
            {
                return ServiceResult<Semester>.Ok(
                    semester,
                    $"Học kỳ \"{semester.SemesterName}\" đã ở trạng thái đóng.");
            }

            semester.IsActive = false;
            await _semesterRepository.SaveChangesAsync();

            return ServiceResult<Semester>.Ok(
                semester,
                $"Đã đóng học kỳ \"{semester.SemesterName}\".");
        }
        catch
        {
            return ServiceResult<Semester>.Fail(
                ErrorCodes.DatabaseError,
                "Không thể đóng học kỳ. Vui lòng thử lại sau.");
        }
    }

    private static ServiceResult<Semester>? ValidateInput(
        string semesterCode,
        string semesterName,
        DateTime startDate,
        DateTime endDate,
        AcademicYear academicYear)
    {
        if (string.IsNullOrWhiteSpace(semesterCode))
        {
            return ServiceResult<Semester>.Fail(
                ErrorCodes.ValidationFailed,
                "Vui lòng nhập mã học kỳ.");
        }

        if (semesterCode.Trim().Length > 20)
        {
            return ServiceResult<Semester>.Fail(
                ErrorCodes.ValidationFailed,
                "Mã học kỳ không được vượt quá 20 ký tự.");
        }

        if (string.IsNullOrWhiteSpace(semesterName))
        {
            return ServiceResult<Semester>.Fail(
                ErrorCodes.ValidationFailed,
                "Vui lòng nhập tên học kỳ.");
        }

        if (semesterName.Trim().Length > 50)
        {
            return ServiceResult<Semester>.Fail(
                ErrorCodes.ValidationFailed,
                "Tên học kỳ không được vượt quá 50 ký tự.");
        }

        if (startDate.Date >= endDate.Date)
        {
            return ServiceResult<Semester>.Fail(
                ErrorCodes.ValidationFailed,
                "Ngày kết thúc phải sau ngày bắt đầu.");
        }

        if (startDate.Date < academicYear.StartDate.Date || endDate.Date > academicYear.EndDate.Date)
        {
            return ServiceResult<Semester>.Fail(
                ErrorCodes.ValidationFailed,
                $"Thời gian học kỳ phải nằm trong năm học {academicYear.YearCode} " +
                $"({academicYear.StartDate:dd/MM/yyyy} - {academicYear.EndDate:dd/MM/yyyy}).");
        }

        return null;
    }

    private async Task<bool> HasOverlappingSemesterAsync(
        int academicYearId,
        DateTime startDate,
        DateTime endDate,
        int? excludeId = null)
    {
        var semesters = await _semesterRepository.GetByAcademicYearIdAsync(academicYearId);
        return semesters.Any(s =>
            (!excludeId.HasValue || s.Id != excludeId.Value) &&
            startDate.Date <= s.EndDate.Date &&
            s.StartDate.Date <= endDate.Date);
    }
}
