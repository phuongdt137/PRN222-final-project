using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Repositories.Data;
using HighSchoolManagementSystem.Repositories.Interfaces;
using HighSchoolManagementSystem.Services.Common;
using HighSchoolManagementSystem.Services.DTOs;
using HighSchoolManagementSystem.Services.Interfaces;

namespace HighSchoolManagementSystem.Services.Implementations;

public class ConductEvaluationService : IConductEvaluationService
{
    private readonly IConductEvaluationRepository _conductRepo;
    private readonly IClassRepository _classRepo;
    private readonly ISemesterRepository _semesterRepo;
    private readonly ITeacherRepository _teacherRepo;
    private readonly IStudentRepository _studentRepo;
    private readonly IParentRepository _parentRepo;
    private readonly IScoreRepository _scoreRepo;
    private readonly AppDbContext _context;
    private readonly ILogger<ConductEvaluationService> _logger;

    public ConductEvaluationService(
        IConductEvaluationRepository conductRepo,
        IClassRepository classRepo,
        ISemesterRepository semesterRepo,
        ITeacherRepository teacherRepo,
        IStudentRepository studentRepo,
        IParentRepository parentRepo,
        IScoreRepository scoreRepo,
        AppDbContext context,
        ILogger<ConductEvaluationService> logger)
    {
        _conductRepo = conductRepo;
        _classRepo = classRepo;
        _semesterRepo = semesterRepo;
        _teacherRepo = teacherRepo;
        _studentRepo = studentRepo;
        _parentRepo = parentRepo;
        _scoreRepo = scoreRepo;
        _context = context;
        _logger = logger;
    }

    public async Task<ServiceResult<ClassConductSummaryDto>> GetClassConductSummaryAsync(
        int classId, int semesterId, int? currentUserId, string userRole)
    {
        try
        {
            var classEntity = await _classRepo.GetByIdWithDetailsAsync(classId);
            if (classEntity == null)
            {
                return ServiceResult<ClassConductSummaryDto>.Fail(ErrorCodes.ClassNotFound, "Không tìm thấy thông tin lớp học.");
            }

            var semester = await _semesterRepo.GetByIdAsync(semesterId);
            if (semester == null)
            {
                return ServiceResult<ClassConductSummaryDto>.Fail(ErrorCodes.SemesterNotFound, "Không tìm thấy học kỳ được yêu cầu.");
            }

            // Kiểm tra phân quyền: Nếu là giáo viên, bắt buộc phải là GVCN của lớp
            if (userRole == "Teacher")
            {
                if (!currentUserId.HasValue)
                {
                    return ServiceResult<ClassConductSummaryDto>.Fail(ErrorCodes.AuthUnauthorized, "Không xác định được danh tính giáo viên.");
                }

                var teacher = await _teacherRepo.GetByUserIdAsync(currentUserId.Value);
                if (teacher == null || classEntity.HomeroomTeacherId != teacher.Id)
                {
                    return ServiceResult<ClassConductSummaryDto>.Fail(
                        ErrorCodes.ConductNotHomeroomTeacher, "Bạn không phải là Giáo viên chủ nhiệm của lớp này.");
                }
            }

            var enrolledStudents = classEntity.StudentClasses
                .Where(sc => sc.Status == "Enrolled" && sc.LeftAt == null)
                .Select(sc => sc.Student)
                .OrderBy(s => s.StudentCode)
                .ToList();

            var evalList = await _conductRepo.GetByClassAndSemesterAsync(classId, semesterId);
            var evaluations = evalList
                .GroupBy(e => e.StudentId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.EvaluatedAt).First());

            var attendanceStats = await _conductRepo.GetClassAttendanceStatsAsync(
                classId, semester.StartDate, semester.EndDate);

            var reportList = await _scoreRepo.GetClassSemesterReportsAsync(classId, semesterId);
            var reports = reportList
                .GroupBy(r => r.StudentId)
                .ToDictionary(g => g.Key, g => g.First());

            var studentItems = new List<ConductEvaluationItemDto>();
            foreach (var st in enrolledStudents)
            {
                evaluations.TryGetValue(st.Id, out var eval);
                reports.TryGetValue(st.Id, out var rep);
                attendanceStats.TryGetValue(st.Id, out var att);

                studentItems.Add(new ConductEvaluationItemDto
                {
                    Id = eval?.Id,
                    StudentId = st.Id,
                    StudentCode = st.StudentCode,
                    FullName = st.User?.FullName ?? "N/A",
                    DateOfBirth = st.User?.DateOfBirth,
                    Gender = st.User?.Gender,
                    Gpa = rep?.Gpa,
                    AcademicRank = rep?.AcademicRank,
                    AbsentsCount = att.Absent,
                    ExcusedCount = att.Excused,
                    LateCount = att.Late,
                    ConductGrade = eval?.ConductGrade ?? rep?.ConductGrade,
                    TeacherComment = eval?.TeacherComment ?? rep?.TeacherComments,
                    EvaluatedAt = eval?.EvaluatedAt,
                    EvaluatedByTeacherName = eval?.HomeroomTeacher?.User?.FullName ?? classEntity.HomeroomTeacher?.User?.FullName
                });
            }

            var summary = new ClassConductSummaryDto
            {
                ClassId = classEntity.Id,
                ClassName = classEntity.ClassName,
                AcademicYearCode = classEntity.AcademicYear?.YearCode ?? string.Empty,
                GradeLevelName = classEntity.GradeLevel?.LevelName ?? string.Empty,
                SemesterId = semester.Id,
                SemesterName = semester.SemesterName,
                HomeroomTeacherId = classEntity.HomeroomTeacherId ?? 0,
                HomeroomTeacherName = classEntity.HomeroomTeacher?.User?.FullName ?? "Chưa phân công",
                TotalStudents = studentItems.Count,
                EvaluatedCount = studentItems.Count(s => s.IsEvaluated),
                TotCount = studentItems.Count(s => s.ConductGrade == "Tốt"),
                KhaCount = studentItems.Count(s => s.ConductGrade == "Khá"),
                DatCount = studentItems.Count(s => s.ConductGrade == "Đạt"),
                ChuaDatCount = studentItems.Count(s => s.ConductGrade == "Chưa đạt"),
                Students = studentItems
            };

            return ServiceResult<ClassConductSummaryDto>.Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách đánh giá hạnh kiểm cho lớp {ClassId}, học kỳ {SemesterId}", classId, semesterId);
            return ServiceResult<ClassConductSummaryDto>.Fail(ErrorCodes.SystemError, "Đã có lỗi hệ thống khi tải danh sách đánh giá hạnh kiểm.");
        }
    }

    public async Task<ServiceResult<ConductEvaluationItemDto>> SaveStudentConductAsync(
        SaveStudentConductDto dto, int currentUserId, string userRole)
    {
        try
        {
            var normalizedGrade = NormalizeConductGrade(dto.ConductGrade);
            if (string.IsNullOrEmpty(normalizedGrade))
            {
                return ServiceResult<ConductEvaluationItemDto>.Fail(
                    ErrorCodes.ConductInvalidGrade, "Xếp loại hạnh kiểm không hợp lệ. Chỉ chấp nhận: Tốt, Khá, Đạt, Chưa đạt.");
            }

            var student = await _studentRepo.GetByIdAsync(dto.StudentId);
            if (student == null)
            {
                return ServiceResult<ConductEvaluationItemDto>.Fail(ErrorCodes.StudentNotFound, "Không tìm thấy học sinh.");
            }

            var semester = await _semesterRepo.GetByIdAsync(dto.SemesterId);
            if (semester == null)
            {
                return ServiceResult<ConductEvaluationItemDto>.Fail(ErrorCodes.SemesterNotFound, "Không tìm thấy học kỳ.");
            }

            // Tìm lớp hiện tại của học sinh
            var studentClass = student.StudentClasses
                .FirstOrDefault(sc => sc.Status == "Enrolled" && sc.LeftAt == null);
            if (studentClass == null)
            {
                return ServiceResult<ConductEvaluationItemDto>.Fail(
                    ErrorCodes.ConductStudentNotInClass, "Học sinh hiện chưa được ghi danh vào lớp học nào.");
            }

            var classEntity = await _classRepo.GetByIdWithDetailsAsync(studentClass.ClassId);
            if (classEntity == null)
            {
                return ServiceResult<ConductEvaluationItemDto>.Fail(ErrorCodes.ClassNotFound, "Không tìm thấy lớp học của học sinh.");
            }

            Teacher? homeroomTeacher = null;
            if (userRole == "Teacher")
            {
                homeroomTeacher = await _teacherRepo.GetByUserIdAsync(currentUserId);
                if (homeroomTeacher == null || classEntity.HomeroomTeacherId != homeroomTeacher.Id)
                {
                    return ServiceResult<ConductEvaluationItemDto>.Fail(
                        ErrorCodes.ConductNotHomeroomTeacher, "Chỉ Giáo viên chủ nhiệm mới có quyền đánh giá hạnh kiểm cho học sinh này.");
                }
            }
            else if (userRole == "Principal" || userRole == "Staff")
            {
                homeroomTeacher = classEntity.HomeroomTeacher;
                if (homeroomTeacher == null && classEntity.HomeroomTeacherId.HasValue)
                {
                    homeroomTeacher = await _teacherRepo.GetByIdAsync(classEntity.HomeroomTeacherId.Value);
                }
            }
            else
            {
                return ServiceResult<ConductEvaluationItemDto>.Fail(ErrorCodes.AuthForbidden, "Bạn không có quyền thực hiện chức năng này.");
            }

            if (homeroomTeacher == null)
            {
                return ServiceResult<ConductEvaluationItemDto>.Fail(
                    ErrorCodes.ConductNotHomeroomTeacher, "Lớp học chưa được phân công Giáo viên chủ nhiệm.");
            }

            // Upsert ConductEvaluation
            var evaluation = await _conductRepo.GetByStudentAndSemesterAsync(dto.StudentId, dto.SemesterId);
            if (evaluation == null)
            {
                evaluation = new ConductEvaluation
                {
                    StudentId = dto.StudentId,
                    SemesterId = dto.SemesterId,
                    HomeroomTeacherId = homeroomTeacher.Id,
                    ConductGrade = normalizedGrade,
                    TeacherComment = dto.TeacherComment?.Trim(),
                    EvaluatedAt = DateTime.UtcNow
                };
                await _conductRepo.AddAsync(evaluation);
            }
            else
            {
                evaluation.HomeroomTeacherId = homeroomTeacher.Id;
                evaluation.ConductGrade = normalizedGrade;
                evaluation.TeacherComment = dto.TeacherComment?.Trim();
                evaluation.EvaluatedAt = DateTime.UtcNow;
                await _conductRepo.UpdateAsync(evaluation);
            }

            await _conductRepo.SaveChangesAsync();

            // Đồng bộ sang SemesterReport
            await SyncWithSemesterReportAsync(dto.StudentId, dto.SemesterId, normalizedGrade, dto.TeacherComment?.Trim());

            var resultDto = new ConductEvaluationItemDto
            {
                Id = evaluation.Id,
                StudentId = student.Id,
                StudentCode = student.StudentCode,
                FullName = student.User?.FullName ?? "N/A",
                DateOfBirth = student.User?.DateOfBirth,
                Gender = student.User?.Gender,
                ConductGrade = evaluation.ConductGrade,
                TeacherComment = evaluation.TeacherComment,
                EvaluatedAt = evaluation.EvaluatedAt,
                EvaluatedByTeacherName = homeroomTeacher.User?.FullName
            };

            return ServiceResult<ConductEvaluationItemDto>.Ok(resultDto, "Lưu đánh giá hạnh kiểm thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lưu đánh giá hạnh kiểm cho học sinh {StudentId}, học kỳ {SemesterId}", dto.StudentId, dto.SemesterId);
            return ServiceResult<ConductEvaluationItemDto>.Fail(ErrorCodes.SystemError, "Đã có lỗi hệ thống khi lưu đánh giá hạnh kiểm.");
        }
    }

    public async Task<ServiceResult<int>> BulkSaveConductAsync(
        BulkSaveConductDto dto, int currentUserId, string userRole)
    {
        try
        {
            if (dto.Items == null || !dto.Items.Any())
            {
                return ServiceResult<int>.Fail(ErrorCodes.ValidationFailed, "Danh sách đánh giá hạnh kiểm trống.");
            }

            var classEntity = await _classRepo.GetByIdWithDetailsAsync(dto.ClassId);
            if (classEntity == null)
            {
                return ServiceResult<int>.Fail(ErrorCodes.ClassNotFound, "Không tìm thấy lớp học.");
            }

            Teacher? homeroomTeacher = null;
            if (userRole == "Teacher")
            {
                homeroomTeacher = await _teacherRepo.GetByUserIdAsync(currentUserId);
                if (homeroomTeacher == null || classEntity.HomeroomTeacherId != homeroomTeacher.Id)
                {
                    return ServiceResult<int>.Fail(
                        ErrorCodes.ConductNotHomeroomTeacher, "Chỉ Giáo viên chủ nhiệm mới có quyền đánh giá hạnh kiểm cho lớp này.");
                }
            }
            else if (userRole == "Principal" || userRole == "Staff")
            {
                homeroomTeacher = classEntity.HomeroomTeacher;
                if (homeroomTeacher == null && classEntity.HomeroomTeacherId.HasValue)
                {
                    homeroomTeacher = await _teacherRepo.GetByIdAsync(classEntity.HomeroomTeacherId.Value);
                }
            }
            else
            {
                return ServiceResult<int>.Fail(ErrorCodes.AuthForbidden, "Bạn không có quyền thực hiện chức năng này.");
            }

            if (homeroomTeacher == null)
            {
                return ServiceResult<int>.Fail(ErrorCodes.ConductNotHomeroomTeacher, "Lớp chưa được phân công Giáo viên chủ nhiệm.");
            }

            var enrolledStudentIds = classEntity.StudentClasses
                .Where(sc => sc.Status == "Enrolled" && sc.LeftAt == null)
                .Select(sc => sc.StudentId)
                .ToHashSet();

            var existingEvaluationsList = await _conductRepo.GetByClassAndSemesterAsync(dto.ClassId, dto.SemesterId);
            var existingEvaluations = existingEvaluationsList
                .GroupBy(e => e.StudentId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.EvaluatedAt).First());

            var existingReportsList = await _context.SemesterReports
                .Where(r => r.SemesterId == dto.SemesterId && enrolledStudentIds.Contains(r.StudentId))
                .ToListAsync();
            var existingReports = existingReportsList
                .GroupBy(r => r.StudentId)
                .ToDictionary(g => g.Key, g => g.First());

            int savedCount = 0;
            var now = DateTime.UtcNow;

            foreach (var item in dto.Items)
            {
                if (!enrolledStudentIds.Contains(item.StudentId))
                {
                    continue; // Bỏ qua học sinh không thuộc lớp
                }

                var normalizedGrade = NormalizeConductGrade(item.ConductGrade);
                if (string.IsNullOrEmpty(normalizedGrade))
                {
                    continue; // Bỏ qua nếu chưa chọn mức xếp loại
                }

                // 1. ConductEvaluation
                if (existingEvaluations.TryGetValue(item.StudentId, out var eval))
                {
                    eval.HomeroomTeacherId = homeroomTeacher.Id;
                    eval.ConductGrade = normalizedGrade;
                    eval.TeacherComment = item.TeacherComment?.Trim();
                    eval.EvaluatedAt = now;
                    await _conductRepo.UpdateAsync(eval);
                }
                else
                {
                    var newEval = new ConductEvaluation
                    {
                        StudentId = item.StudentId,
                        SemesterId = dto.SemesterId,
                        HomeroomTeacherId = homeroomTeacher.Id,
                        ConductGrade = normalizedGrade,
                        TeacherComment = item.TeacherComment?.Trim(),
                        EvaluatedAt = now
                    };
                    await _conductRepo.AddAsync(newEval);
                    existingEvaluations[item.StudentId] = newEval;
                }

                // 2. SemesterReport sync
                if (existingReports.TryGetValue(item.StudentId, out var rep))
                {
                    rep.ConductGrade = normalizedGrade;
                    rep.TeacherComments = item.TeacherComment?.Trim();
                    _context.SemesterReports.Update(rep);
                }
                else
                {
                    var newReport = new SemesterReport
                    {
                        StudentId = item.StudentId,
                        SemesterId = dto.SemesterId,
                        ConductGrade = normalizedGrade,
                        TeacherComments = item.TeacherComment?.Trim()
                    };
                    await _context.SemesterReports.AddAsync(newReport);
                    existingReports[item.StudentId] = newReport;
                }

                savedCount++;
            }

            await _conductRepo.SaveChangesAsync();
            await _context.SaveChangesAsync();

            return ServiceResult<int>.Ok(savedCount, $"Đã cập nhật đánh giá hạnh kiểm cho {savedCount} học sinh.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lưu đánh giá hạnh kiểm hàng loạt cho lớp {ClassId}, học kỳ {SemesterId}", dto.ClassId, dto.SemesterId);
            return ServiceResult<int>.Fail(ErrorCodes.SystemError, "Đã có lỗi hệ thống khi lưu đánh giá hạnh kiểm hàng loạt.");
        }
    }

    public async Task<ServiceResult<StudentConductDetailDto>> GetStudentConductAsync(
        int studentId, int semesterId, int currentUserId, string userRole)
    {
        try
        {
            var student = await _studentRepo.GetByIdAsync(studentId);
            if (student == null)
            {
                return ServiceResult<StudentConductDetailDto>.Fail(ErrorCodes.StudentNotFound, "Không tìm thấy thông tin học sinh.");
            }

            // Kiểm tra phân quyền truy cập
            if (userRole == "Student")
            {
                if (student.UserId != currentUserId)
                {
                    return ServiceResult<StudentConductDetailDto>.Fail(ErrorCodes.AuthForbidden, "Bạn không có quyền xem thông tin của học sinh khác.");
                }
            }
            else if (userRole == "Parent")
            {
                var parent = await _parentRepo.GetByUserIdAsync(currentUserId);
                if (parent == null || !parent.Students.Any(s => s.Id == studentId))
                {
                    return ServiceResult<StudentConductDetailDto>.Fail(ErrorCodes.AuthForbidden, "Bạn chỉ có quyền xem thông tin của con em mình.");
                }
            }

            var semester = await _semesterRepo.GetByIdAsync(semesterId);
            if (semester == null)
            {
                return ServiceResult<StudentConductDetailDto>.Fail(ErrorCodes.SemesterNotFound, "Không tìm thấy học kỳ.");
            }

            var evaluation = await _conductRepo.GetByStudentAndSemesterAsync(studentId, semesterId);
            var studentClass = student.StudentClasses.FirstOrDefault(sc => sc.Status == "Enrolled" && sc.LeftAt == null);

            var report = await _scoreRepo.GetStudentSemesterReportAsync(studentId, semesterId);

            int absent = 0, excused = 0, late = 0;
            if (studentClass != null)
            {
                var stats = await _conductRepo.GetClassAttendanceStatsAsync(studentClass.ClassId, semester.StartDate, semester.EndDate);
                if (stats.TryGetValue(studentId, out var att))
                {
                    absent = att.Absent;
                    excused = att.Excused;
                    late = att.Late;
                }
            }

            var detail = new StudentConductDetailDto
            {
                StudentId = student.Id,
                StudentCode = student.StudentCode,
                FullName = student.User?.FullName ?? "N/A",
                ClassName = studentClass?.Class?.ClassName ?? "Chưa phân lớp",
                AcademicYearCode = studentClass?.Class?.AcademicYear?.YearCode ?? semester.AcademicYear?.YearCode ?? string.Empty,
                SemesterId = semester.Id,
                SemesterName = semester.SemesterName,
                ConductGrade = evaluation?.ConductGrade ?? report?.ConductGrade,
                TeacherComment = evaluation?.TeacherComment ?? report?.TeacherComments,
                EvaluatedAt = evaluation?.EvaluatedAt,
                HomeroomTeacherName = evaluation?.HomeroomTeacher?.User?.FullName ?? studentClass?.Class?.HomeroomTeacher?.User?.FullName,
                Gpa = report?.Gpa,
                AcademicRank = report?.AcademicRank,
                AbsentsCount = absent,
                ExcusedCount = excused,
                LateCount = late
            };

            return ServiceResult<StudentConductDetailDto>.Ok(detail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy chi tiết hạnh kiểm cho học sinh {StudentId}, học kỳ {SemesterId}", studentId, semesterId);
            return ServiceResult<StudentConductDetailDto>.Fail(ErrorCodes.SystemError, "Đã có lỗi hệ thống khi tải thông tin hạnh kiểm.");
        }
    }

    public async Task<ServiceResult<IEnumerable<StudentConductHistoryItemDto>>> GetStudentConductHistoryAsync(
        int studentId, int currentUserId, string userRole)
    {
        try
        {
            var student = await _studentRepo.GetByIdAsync(studentId);
            if (student == null)
            {
                return ServiceResult<IEnumerable<StudentConductHistoryItemDto>>.Fail(ErrorCodes.StudentNotFound, "Không tìm thấy thông tin học sinh.");
            }

            if (userRole == "Student" && student.UserId != currentUserId)
            {
                return ServiceResult<IEnumerable<StudentConductHistoryItemDto>>.Fail(ErrorCodes.AuthForbidden, "Bạn không có quyền truy cập.");
            }

            if (userRole == "Parent")
            {
                var parent = await _parentRepo.GetByUserIdAsync(currentUserId);
                if (parent == null || !parent.Students.Any(s => s.Id == studentId))
                {
                    return ServiceResult<IEnumerable<StudentConductHistoryItemDto>>.Fail(ErrorCodes.AuthForbidden, "Bạn không có quyền truy cập.");
                }
            }

            var history = await _conductRepo.GetStudentConductHistoryAsync(studentId);

            var items = history.Select(h =>
            {
                var classItem = h.Student?.StudentClasses
                    .FirstOrDefault(sc => sc.Status == "Enrolled" && sc.LeftAt == null)?.Class;

                return new StudentConductHistoryItemDto
                {
                    EvaluationId = h.Id,
                    SemesterId = h.SemesterId,
                    SemesterName = h.Semester?.SemesterName ?? "N/A",
                    AcademicYearCode = h.Semester?.AcademicYear?.YearCode ?? "N/A",
                    ClassName = classItem?.ClassName ?? "N/A",
                    ConductGrade = h.ConductGrade,
                    TeacherComment = h.TeacherComment,
                    HomeroomTeacherName = h.HomeroomTeacher?.User?.FullName ?? "N/A",
                    EvaluatedAt = h.EvaluatedAt
                };
            }).ToList();

            return ServiceResult<IEnumerable<StudentConductHistoryItemDto>>.Ok(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy lịch sử hạnh kiểm của học sinh {StudentId}", studentId);
            return ServiceResult<IEnumerable<StudentConductHistoryItemDto>>.Fail(ErrorCodes.SystemError, "Đã có lỗi hệ thống khi tải lịch sử hạnh kiểm.");
        }
    }

    public async Task<ServiceResult<PrincipalConductOverviewDto>> GetPrincipalOverviewAsync(int semesterId)
    {
        try
        {
            var semester = await _semesterRepo.GetByIdAsync(semesterId);
            if (semester == null)
            {
                return ServiceResult<PrincipalConductOverviewDto>.Fail(ErrorCodes.SemesterNotFound, "Không tìm thấy học kỳ.");
            }

            var allClasses = await _classRepo.GetByFiltersAsync(semester.AcademicYearId, null);
            var allEvaluations = (await _conductRepo.GetBySemesterAsync(semesterId)).ToList();

            var progressList = new List<ClassConductProgressDto>();
            int totalSchoolStudents = 0;
            int totalSchoolEvaluated = 0;
            int schoolTot = 0, schoolKha = 0, schoolDat = 0, schoolChuaDat = 0;

            foreach (var c in allClasses)
            {
                var enrolledStudents = c.StudentClasses
                    .Where(sc => sc.Status == "Enrolled" && sc.LeftAt == null)
                    .Select(sc => sc.StudentId)
                    .ToHashSet();

                int classTotal = enrolledStudents.Count;
                totalSchoolStudents += classTotal;

                var classEvals = allEvaluations
                    .Where(e => enrolledStudents.Contains(e.StudentId))
                    .ToList();

                int evaluated = classEvals.Count;
                totalSchoolEvaluated += evaluated;

                int tot = classEvals.Count(e => e.ConductGrade == "Tốt");
                int kha = classEvals.Count(e => e.ConductGrade == "Khá");
                int dat = classEvals.Count(e => e.ConductGrade == "Đạt");
                int chuaDat = classEvals.Count(e => e.ConductGrade == "Chưa đạt");

                schoolTot += tot;
                schoolKha += kha;
                schoolDat += dat;
                schoolChuaDat += chuaDat;

                progressList.Add(new ClassConductProgressDto
                {
                    ClassId = c.Id,
                    ClassName = c.ClassName,
                    GradeLevelName = c.GradeLevel?.LevelName ?? string.Empty,
                    HomeroomTeacherName = c.HomeroomTeacher?.User?.FullName ?? "Chưa phân công",
                    TotalStudents = classTotal,
                    EvaluatedCount = evaluated,
                    TotCount = tot,
                    KhaCount = kha,
                    DatCount = dat,
                    ChuaDatCount = chuaDat
                });
            }

            var overview = new PrincipalConductOverviewDto
            {
                SemesterId = semester.Id,
                SemesterName = semester.SemesterName,
                AcademicYearCode = semester.AcademicYear?.YearCode ?? string.Empty,
                TotalClasses = allClasses.Count(),
                TotalStudents = totalSchoolStudents,
                TotalEvaluated = totalSchoolEvaluated,
                TotCount = schoolTot,
                KhaCount = schoolKha,
                DatCount = schoolDat,
                ChuaDatCount = schoolChuaDat,
                ClassProgress = progressList.OrderBy(p => p.ClassName).ToList()
            };

            return ServiceResult<PrincipalConductOverviewDto>.Ok(overview);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy tổng quan hạnh kiểm toàn trường cho học kỳ {SemesterId}", semesterId);
            return ServiceResult<PrincipalConductOverviewDto>.Fail(ErrorCodes.SystemError, "Đã có lỗi hệ thống khi tải báo cáo tổng quan hạnh kiểm.");
        }
    }

    private static string? NormalizeConductGrade(string? grade)
    {
        if (string.IsNullOrWhiteSpace(grade)) return null;

        var trimmed = grade.Trim();
        if (string.Equals(trimmed, "Tốt", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(trimmed, "Tot", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(trimmed, "Excellent", StringComparison.OrdinalIgnoreCase))
        {
            return "Tốt";
        }

        if (string.Equals(trimmed, "Khá", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(trimmed, "Kha", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(trimmed, "Good", StringComparison.OrdinalIgnoreCase))
        {
            return "Khá";
        }

        if (string.Equals(trimmed, "Đạt", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(trimmed, "Dat", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(trimmed, "Trung bình", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(trimmed, "Trung binh", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(trimmed, "Average", StringComparison.OrdinalIgnoreCase))
        {
            return "Đạt";
        }

        if (string.Equals(trimmed, "Chưa đạt", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(trimmed, "Chua dat", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(trimmed, "Yếu", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(trimmed, "Yeu", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(trimmed, "Poor", StringComparison.OrdinalIgnoreCase))
        {
            return "Chưa đạt";
        }

        return null;
    }

    private async Task SyncWithSemesterReportAsync(int studentId, int semesterId, string conductGrade, string? teacherComment)
    {
        try
        {
            var report = await _context.SemesterReports
                .FirstOrDefaultAsync(r => r.StudentId == studentId && r.SemesterId == semesterId);

            if (report != null)
            {
                report.ConductGrade = conductGrade;
                report.TeacherComments = teacherComment;
                _context.SemesterReports.Update(report);
            }
            else
            {
                var newReport = new SemesterReport
                {
                    StudentId = studentId,
                    SemesterId = semesterId,
                    ConductGrade = conductGrade,
                    TeacherComments = teacherComment
                };
                await _context.SemesterReports.AddAsync(newReport);
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể đồng bộ Hạnh kiểm sang SemesterReport cho học sinh {StudentId}", studentId);
        }
    }
}
