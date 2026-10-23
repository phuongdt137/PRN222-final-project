using Microsoft.Extensions.Logging;
using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Repositories.Interfaces;
using HighSchoolManagementSystem.Services.Common;
using HighSchoolManagementSystem.Services.DTOs;
using HighSchoolManagementSystem.Services.Interfaces;

namespace HighSchoolManagementSystem.Services.Implementations;

public class ScoreService : IScoreService
{
    private readonly IScoreRepository _scoreRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly ITeacherFeedbackRepository _feedbackRepository;
    private readonly ISemesterRepository _semesterRepository;
    private readonly ILogger<ScoreService> _logger;

    public ScoreService(
        IScoreRepository scoreRepository,
        IStudentRepository studentRepository,
        ITeacherFeedbackRepository feedbackRepository,
        ISemesterRepository semesterRepository,
        ILogger<ScoreService> logger)
    {
        _scoreRepository = scoreRepository;
        _studentRepository = studentRepository;
        _feedbackRepository = feedbackRepository;
        _semesterRepository = semesterRepository;
        _logger = logger;
    }

    public async Task<ServiceResult<IEnumerable<StudentScoreItemDto>>> GetScoresForAssessmentAsync(int assessmentId, int? teacherId = null)
    {
        try
        {
            var assessment = await _scoreRepository.GetAssessmentWithDetailsAsync(assessmentId);
            if (assessment == null)
            {
                return ServiceResult<IEnumerable<StudentScoreItemDto>>.Fail(
                    ErrorCodes.AssessmentNotFound,
                    "Không tìm thấy bài kiểm tra.");
            }

            if (teacherId.HasValue && assessment.TeachingAssignment?.TeacherId != teacherId.Value)
            {
                return ServiceResult<IEnumerable<StudentScoreItemDto>>.Fail(
                    ErrorCodes.AuthForbidden,
                    "Bạn không có quyền quản lý điểm của bài kiểm tra này.");
            }

            var enrolledStudents = assessment.TeachingAssignment?.Class?.StudentClasses
                .Where(sc => sc.Status == "Enrolled" && sc.LeftAt == null)
                .OrderBy(sc => sc.Student.StudentCode)
                .Select(sc => sc.Student)
                .ToList() ?? new List<Student>();

            var existingScores = assessment.StudentScores.ToDictionary(s => s.StudentId);

            // Kiểm tra tự động khóa theo hạn 7 ngày kể từ ngày làm bài (ConductedAt)
            DateTime? deadline = assessment.ConductedAt?.AddDays(7);
            bool isAutoLocked = deadline.HasValue && DateTime.UtcNow >= deadline.Value;

            // Nếu đã quá hạn: hệ thống tự động ghi nhận điểm 0 cho những học sinh chưa có điểm
            if (isAutoLocked && enrolledStudents.Any())
            {
                bool hasAutoFilled = false;
                var autoFilledStudentIds = new List<int>();

                foreach (var st in enrolledStudents)
                {
                    if (!existingScores.TryGetValue(st.Id, out var scoreEntity) || !scoreEntity.Score.HasValue)
                    {
                        if (scoreEntity == null)
                        {
                            scoreEntity = new StudentScore
                            {
                                StudentId = st.Id,
                                AssessmentId = assessmentId,
                                Score = 0.0m,
                                Comment = "Quá hạn nhập điểm - Hệ thống tự động ghi nhận điểm 0",
                                IsLocked = true
                            };
                            await _scoreRepository.AddScoreAsync(scoreEntity);
                            existingScores[st.Id] = scoreEntity;
                        }
                        else
                        {
                            scoreEntity.Score = 0.0m;
                            if (string.IsNullOrWhiteSpace(scoreEntity.Comment))
                            {
                                scoreEntity.Comment = "Quá hạn nhập điểm - Hệ thống tự động ghi nhận điểm 0";
                            }
                            scoreEntity.IsLocked = true;
                            await _scoreRepository.UpdateScoreAsync(scoreEntity);
                        }
                        hasAutoFilled = true;
                        autoFilledStudentIds.Add(st.Id);
                    }
                    else if (!scoreEntity.IsLocked)
                    {
                        scoreEntity.IsLocked = true;
                        await _scoreRepository.UpdateScoreAsync(scoreEntity);
                        hasAutoFilled = true;
                    }
                }

                if (hasAutoFilled)
                {
                    await _scoreRepository.SaveChangesAsync();
                    if (assessment.TeachingAssignment != null)
                    {
                        foreach (var sId in autoFilledStudentIds)
                        {
                            await RecalculateStudentSubjectAverageAsync(
                                sId,
                                assessment.TeachingAssignment.SubjectId,
                                assessment.TeachingAssignment.SemesterId);
                        }
                    }
                }
            }

            var resultList = new List<StudentScoreItemDto>();
            foreach (var st in enrolledStudents)
            {
                existingScores.TryGetValue(st.Id, out var scoreEntity);

                resultList.Add(new StudentScoreItemDto
                {
                    StudentId = st.Id,
                    StudentCode = st.StudentCode,
                    FullName = st.User.FullName,
                    AssessmentId = assessmentId,
                    ScoreId = scoreEntity?.Id,
                    Score = scoreEntity?.Score,
                    Comment = scoreEntity?.Comment,
                    IsLocked = isAutoLocked || (scoreEntity?.IsLocked ?? false),
                    HasAuditLogs = scoreEntity?.ScoreAuditLogs.Any() ?? false
                });
            }

            return ServiceResult<IEnumerable<StudentScoreItemDto>>.Ok(resultList);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách điểm của bài kiểm tra Id: {AssessmentId}", assessmentId);
            return ServiceResult<IEnumerable<StudentScoreItemDto>>.Fail(
                ErrorCodes.DatabaseError,
                "Không thể tải danh sách điểm bài kiểm tra. Vui lòng thử lại sau.");
        }
    }

    public async Task<ServiceResult<StudentScoreItemDto>> SaveSingleScoreAsync(SaveStudentScoreDto dto, int modifierUserId, int? teacherId = null, bool isPrincipal = false)
    {
        try
        {
            if (dto.Comment?.Trim().Length > 255 || dto.Reason?.Trim().Length > 255)
                return ServiceResult<StudentScoreItemDto>.Fail(ErrorCodes.ValidationFailed, "Nhận xét và lý do tối đa 255 ký tự.");

            var assessment = await _scoreRepository.GetAssessmentWithDetailsAsync(dto.AssessmentId);
            if (assessment == null)
            {
                return ServiceResult<StudentScoreItemDto>.Fail(
                    ErrorCodes.AssessmentNotFound,
                    "Không tìm thấy bài kiểm tra.");
            }

            bool isTeacher = teacherId.HasValue;
            bool isStaff = !isTeacher && !isPrincipal;

            if (isTeacher && assessment.TeachingAssignment?.TeacherId != teacherId!.Value)
            {
                return ServiceResult<StudentScoreItemDto>.Fail(
                    ErrorCodes.AuthForbidden,
                    "Bạn không có quyền nhập điểm cho bài kiểm tra này.");
            }

            // 1. Kiểm tra trạng thái học kỳ: nếu Hiệu trưởng đã chốt sổ, chỉ Hiệu trưởng mới được sửa
            int semesterId = assessment.TeachingAssignment?.SemesterId ?? 0;
            bool isSemesterFinalized = semesterId > 0 && await _scoreRepository.IsSemesterFinalizedAsync(semesterId);
            if (isSemesterFinalized && !isPrincipal)
            {
                return ServiceResult<StudentScoreItemDto>.Fail(
                    ErrorCodes.ScoreLocked,
                    "Học kỳ này đã được Hiệu trưởng chốt sổ chính thức. Chỉ Hiệu trưởng mới có quyền điều chỉnh điểm.");
            }

            // 2. Kiểm tra hạn tự khóa 7 ngày đối với Giáo viên
            DateTime? deadline = assessment.ConductedAt?.AddDays(7);
            bool isAutoLocked = deadline.HasValue && DateTime.UtcNow >= deadline.Value;
            bool isLocked = isAutoLocked || assessment.StudentScores.Any(s => s.IsLocked);

            if (isTeacher && isLocked)
            {
                return ServiceResult<StudentScoreItemDto>.Fail(
                    ErrorCodes.ScoreLocked,
                    "Bài kiểm tra đã quá hạn nhập điểm (7 ngày kể từ ngày làm bài) và đã được hệ thống tự động khóa. Vui lòng liên hệ Giáo vụ (Staff) nếu cần điều chỉnh.");
            }

            var minScore = assessment.AssessmentType?.MinScore ?? 0;
            var maxScore = assessment.AssessmentType?.MaxScore ?? 10;
            if (dto.Score.HasValue && (dto.Score.Value < minScore || dto.Score.Value > maxScore))
            {
                return ServiceResult<StudentScoreItemDto>.Fail(
                    ErrorCodes.ScoreOutOfRange,
                    $"Điểm số phải nằm trong khoảng từ {minScore:0.##} đến {maxScore:0.##}.");
            }

            var isStudentInClass = assessment.TeachingAssignment?.Class?.StudentClasses
                .Any(sc => sc.StudentId == dto.StudentId && sc.Status == "Enrolled" && sc.LeftAt == null) ?? false;

            if (!isStudentInClass)
            {
                return ServiceResult<StudentScoreItemDto>.Fail(
                    ErrorCodes.ScoreStudentNotInClass,
                    "Học sinh không thuộc danh sách lớp học của bài kiểm tra này.");
            }

            var existingScore = await _scoreRepository.GetScoreByStudentAndAssessmentAsync(dto.StudentId, dto.AssessmentId);

            // Nếu là Staff sửa điểm đã khóa thì bắt buộc phải có lý do
            if (isStaff && existingScore != null && existingScore.Score != dto.Score && string.IsNullOrWhiteSpace(dto.Reason))
            {
                return ServiceResult<StudentScoreItemDto>.Fail(ErrorCodes.ScoreReasonRequired, "Giáo vụ bắt buộc phải nhập lý do điều chỉnh điểm.");
            }

            await using var transaction = await _scoreRepository.BeginTransactionAsync();
            decimal? oldScoreVal = existingScore?.Score;
            bool scoreChanged = existingScore == null || existingScore.Score != dto.Score;

            if (existingScore != null)
            {
                if (existingScore.Score != dto.Score)
                {
                    var auditLog = new ScoreAuditLog
                    {
                        StudentScoreId = existingScore.Id,
                        OldScore = existingScore.Score,
                        NewScore = dto.Score,
                        ModifiedBy = modifierUserId,
                        ModifiedAt = DateTime.UtcNow,
                        Reason = string.IsNullOrWhiteSpace(dto.Reason)
                            ? (isPrincipal ? "Hiệu trưởng điều chỉnh điểm" : (isTeacher ? "Giáo viên cập nhật điểm" : "Giáo vụ điều chỉnh điểm"))
                            : dto.Reason.Trim()
                    };
                    await _scoreRepository.AddScoreAuditLogAsync(auditLog);
                }

                existingScore.Score = dto.Score;
                existingScore.Comment = dto.Comment?.Trim();
                if (isStaff || isPrincipal)
                {
                    existingScore.IsLocked = true;
                }
                await _scoreRepository.UpdateScoreAsync(existingScore);
            }
            else
            {
                existingScore = new StudentScore
                {
                    StudentId = dto.StudentId,
                    AssessmentId = dto.AssessmentId,
                    Score = dto.Score,
                    Comment = dto.Comment?.Trim(),
                    IsLocked = isStaff || isPrincipal || isLocked
                };
                await _scoreRepository.AddScoreAsync(existingScore);
            }

            await _scoreRepository.SaveChangesAsync();

            // Nếu Staff sửa điểm -> Gửi thông báo đến Hiệu trưởng
            if (isStaff && scoreChanged)
            {
                await NotifyPrincipalsAboutScoreChangeAsync(
                    modifierUserId,
                    assessment,
                    dto.StudentId,
                    oldScoreVal,
                    dto.Score,
                    dto.Reason ?? "Giáo vụ điều chỉnh điểm theo quy chế");
            }

            // Tự động tính toán lại ĐTB môn học kỳ
            if (assessment.TeachingAssignment != null)
            {
                var averageResult = await RecalculateStudentSubjectAverageAsync(
                    dto.StudentId,
                    assessment.TeachingAssignment.SubjectId,
                    assessment.TeachingAssignment.SemesterId);
                if (!averageResult.Success)
                    return ServiceResult<StudentScoreItemDto>.Fail(averageResult.ErrorCode!, averageResult.Message!);
            }
            await transaction.CommitAsync();

            var resultDto = new StudentScoreItemDto
            {
                StudentId = dto.StudentId,
                AssessmentId = dto.AssessmentId,
                ScoreId = existingScore.Id,
                Score = existingScore.Score,
                Comment = existingScore.Comment,
                IsLocked = existingScore.IsLocked,
                HasAuditLogs = existingScore.ScoreAuditLogs.Any()
            };

            return ServiceResult<StudentScoreItemDto>.Ok(resultDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lưu điểm cho học sinh Id: {StudentId}, Assessment Id: {AssessmentId}", dto.StudentId, dto.AssessmentId);
            return ServiceResult<StudentScoreItemDto>.Fail(
                ErrorCodes.DatabaseError,
                "Không thể lưu điểm học sinh. Vui lòng thử lại sau.");
        }
    }

    public async Task<ServiceResult<int>> SaveBulkScoresAsync(BulkSaveScoreDto dto, int modifierUserId, int? teacherId = null, bool isPrincipal = false)
    {
        try
        {
            if (dto.Scores == null || dto.Scores.Count == 0 || dto.Scores.Select(s => s.StudentId).Distinct().Count() != dto.Scores.Count)
                return ServiceResult<int>.Fail(ErrorCodes.ValidationFailed, "Danh sách điểm phải có dữ liệu và không được trùng học sinh.");

            if (dto.GlobalReason?.Trim().Length > 255 || dto.Scores.Any(s => s.Comment?.Trim().Length > 255 || s.Reason?.Trim().Length > 255))
                return ServiceResult<int>.Fail(ErrorCodes.ValidationFailed, "Nhận xét và lý do tối đa 255 ký tự.");

            var assessment = await _scoreRepository.GetAssessmentWithDetailsAsync(dto.AssessmentId);
            if (assessment == null)
            {
                return ServiceResult<int>.Fail(
                    ErrorCodes.AssessmentNotFound,
                    "Không tìm thấy bài kiểm tra.");
            }

            bool isTeacher = teacherId.HasValue;
            bool isStaff = !isTeacher && !isPrincipal;

            // 1. Kiểm tra trạng thái học kỳ: nếu Hiệu trưởng đã chốt sổ, chỉ Hiệu trưởng mới được sửa
            int semesterId = assessment.TeachingAssignment?.SemesterId ?? 0;
            bool isSemesterFinalized = semesterId > 0 && await _scoreRepository.IsSemesterFinalizedAsync(semesterId);
            if (isSemesterFinalized && !isPrincipal)
            {
                return ServiceResult<int>.Fail(
                    ErrorCodes.ScoreLocked,
                    "Học kỳ này đã được Hiệu trưởng chốt sổ chính thức. Chỉ Hiệu trưởng mới có quyền điều chỉnh điểm.");
            }

            // 2. Kiểm tra hạn tự khóa 7 ngày
            DateTime? deadline = assessment.ConductedAt?.AddDays(7);
            bool isAutoLocked = deadline.HasValue && DateTime.UtcNow >= deadline.Value;
            bool isLocked = isAutoLocked || assessment.StudentScores.Any(s => s.IsLocked);

            if (isTeacher)
            {
                if (assessment.TeachingAssignment?.TeacherId != teacherId!.Value)
                {
                    return ServiceResult<int>.Fail(
                        ErrorCodes.AuthForbidden,
                        "Bạn không có quyền nhập điểm cho bài kiểm tra này.");
                }

                if (isLocked)
                {
                    return ServiceResult<int>.Fail(
                        ErrorCodes.AssessmentAlreadyLockedForTeacher,
                        "Bài kiểm tra này đã quá hạn nhập điểm (7 ngày kể từ ngày làm bài) và đã được hệ thống tự động khóa. Giáo viên không thể chỉnh sửa điểm số. Vui lòng liên hệ phòng Giáo vụ (Staff) nếu cần điều chỉnh.");
                }
            }

            var enrolledStudents = assessment.TeachingAssignment?.Class?.StudentClasses
                .Where(sc => sc.Status == "Enrolled" && sc.LeftAt == null)
                .OrderBy(sc => sc.Student.StudentCode)
                .Select(sc => sc.Student)
                .ToList() ?? new List<Student>();

            // Validate điểm hợp lệ từ MinScore đến MaxScore
            var enrolledIds = enrolledStudents.Select(s => s.Id).ToHashSet();
            if (dto.Scores.Any(s => !enrolledIds.Contains(s.StudentId)))
                return ServiceResult<int>.Fail(ErrorCodes.ScoreStudentNotInClass, "Danh sách gửi lên có học sinh không thuộc lớp của bài kiểm tra.");

            var minScore = assessment.AssessmentType?.MinScore ?? 0;
            var maxScore = assessment.AssessmentType?.MaxScore ?? 10;
            foreach (var item in dto.Scores)
            {
                if (item.Score.HasValue && (item.Score.Value < minScore || item.Score.Value > maxScore))
                {
                    return ServiceResult<int>.Fail(
                        ErrorCodes.ScoreOutOfRange,
                        $"Điểm số {item.Score.Value} của học sinh Id {item.StudentId} không hợp lệ (phải nằm trong khoảng từ {minScore:0.##} đến {maxScore:0.##}).");
                }
            }

            // 3. Nếu là Staff sửa điểm: bắt buộc phải có lý do chung hoặc riêng cho mỗi điểm thay đổi
            if (isStaff)
            {
                var globalReason = !string.IsNullOrWhiteSpace(dto.GlobalReason) ? dto.GlobalReason.Trim() : null;
                foreach (var item in dto.Scores)
                {
                    var existingScore = await _scoreRepository.GetScoreByStudentAndAssessmentAsync(item.StudentId, dto.AssessmentId);
                    if (existingScore != null && existingScore.Score != item.Score &&
                        string.IsNullOrWhiteSpace(globalReason) && string.IsNullOrWhiteSpace(item.Reason))
                    {
                        return ServiceResult<int>.Fail(ErrorCodes.ScoreReasonRequired, "Giáo vụ bắt buộc phải nhập lý do điều chỉnh điểm.");
                    }
                }
            }

            await using var transaction = await _scoreRepository.BeginTransactionAsync();
            int savedCount = 0;
            var affectedStudentIds = new HashSet<int>();
            int scoreModificationsCount = 0;

            foreach (var item in dto.Scores)
            {
                var existingScore = await _scoreRepository.GetScoreByStudentAndAssessmentAsync(item.StudentId, dto.AssessmentId);

                if (existingScore != null)
                {
                    // Nếu là Teacher và điểm này đã bị khóa trước đó thì bỏ qua
                    if (isTeacher && existingScore.IsLocked && existingScore.Score != item.Score)
                    {
                        continue;
                    }

                    // Ghi vết kiểm toán (Audit Log) nếu điểm số bị thay đổi
                    if (existingScore.Score != item.Score)
                    {
                        var reason = !string.IsNullOrWhiteSpace(item.Reason)
                            ? item.Reason.Trim()
                            : (!string.IsNullOrWhiteSpace(dto.GlobalReason)
                                ? dto.GlobalReason.Trim()
                                : (isPrincipal ? "Hiệu trưởng điều chỉnh điểm" : (isTeacher ? "Giáo viên cập nhật điểm" : "Giáo vụ điều chỉnh điểm")));

                        var auditLog = new ScoreAuditLog
                        {
                            StudentScoreId = existingScore.Id,
                            OldScore = existingScore.Score,
                            NewScore = item.Score,
                            ModifiedBy = modifierUserId,
                            ModifiedAt = DateTime.UtcNow,
                            Reason = reason
                        };
                        await _scoreRepository.AddScoreAuditLogAsync(auditLog);
                        scoreModificationsCount++;
                    }

                    existingScore.Score = item.Score;
                    existingScore.Comment = item.Comment?.Trim();

                    // Staff hoặc Principal sửa thì giữ khóa; Teacher sửa trước hạn thì không khóa
                    if (isStaff || isPrincipal)
                    {
                        existingScore.IsLocked = true;
                    }
                    else if (isTeacher)
                    {
                        existingScore.IsLocked = false;
                    }

                    await _scoreRepository.UpdateScoreAsync(existingScore);
                }
                else
                {
                    // Chỉ thêm mới nếu có điểm hoặc có nhận xét
                    if (item.Score.HasValue || !string.IsNullOrWhiteSpace(item.Comment))
                    {
                        var newScore = new StudentScore
                        {
                            StudentId = item.StudentId,
                            AssessmentId = dto.AssessmentId,
                            Score = item.Score,
                            Comment = item.Comment?.Trim(),
                            IsLocked = isStaff || isPrincipal
                        };
                        await _scoreRepository.AddScoreAsync(newScore);
                        if (isStaff || isPrincipal) scoreModificationsCount++;
                    }
                }

                savedCount++;
                affectedStudentIds.Add(item.StudentId);
            }

            await _scoreRepository.SaveChangesAsync();

            // Nếu là Staff và có điểm thay đổi -> Bắn notification cho Hiệu trưởng
            if (isStaff && scoreModificationsCount > 0)
            {
                await NotifyPrincipalsAboutBulkScoreChangesAsync(
                    modifierUserId,
                    assessment,
                    scoreModificationsCount,
                    dto.GlobalReason ?? "Giáo vụ điều chỉnh điểm theo quy chế");
            }

            // Tính toán lại ĐTB môn cho toàn bộ học sinh được cập nhật điểm
            if (assessment.TeachingAssignment != null)
            {
                foreach (var sId in affectedStudentIds)
                {
                    var averageResult = await RecalculateStudentSubjectAverageAsync(
                        sId,
                        assessment.TeachingAssignment.SubjectId,
                        assessment.TeachingAssignment.SemesterId);
                    if (!averageResult.Success)
                        return ServiceResult<int>.Fail(averageResult.ErrorCode!, averageResult.Message!);
                }
            }
            await transaction.CommitAsync();

            return ServiceResult<int>.Ok(savedCount, "Đã lưu bảng điểm thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lưu bảng điểm bài kiểm tra Id: {AssessmentId}", dto.AssessmentId);
            return ServiceResult<int>.Fail(ErrorCodes.DatabaseError, "Không thể lưu bảng điểm. Vui lòng thử lại sau.");
        }
    }

    private async Task NotifyPrincipalsAboutScoreChangeAsync(
        int modifierUserId,
        Assessment assessment,
        int studentId,
        decimal? oldScore,
        decimal? newScore,
        string reason)
    {
        try
        {
            var principals = await _scoreRepository.GetUsersByRoleAsync("Principal");
            if (!principals.Any()) return;

            var modifier = await _scoreRepository.GetUserByIdAsync(modifierUserId);
            var student = assessment.TeachingAssignment?.Class?.StudentClasses
                .FirstOrDefault(sc => sc.StudentId == studentId)?.Student;

            string modName = modifier?.FullName ?? "Giáo vụ";
            string subName = assessment.TeachingAssignment?.Subject?.SubjectName ?? "Môn học";
            string clsName = assessment.TeachingAssignment?.Class?.ClassName ?? "Lớp";
            string stuName = student?.User?.FullName ?? $"HS ID {studentId}";
            string oldStr = oldScore?.ToString("0.##") ?? "Trống";
            string newStr = newScore?.ToString("0.##") ?? "Trống";

            foreach (var p in principals)
            {
                var notif = new Notification
                {
                    SenderId = modifierUserId,
                    RecipientId = p.Id,
                    Title = "Cảnh báo điều chỉnh điểm sau khóa",
                    Content = $"Giáo vụ {modName} vừa điều chỉnh điểm môn {subName} ({assessment.AssessmentName}) lớp {clsName} của học sinh {stuName} từ {oldStr} thành {newStr}. Lý do: {reason}.",
                    SentAt = DateTime.UtcNow,
                    IsRead = false
                };
                await _scoreRepository.AddNotificationAsync(notif);
            }
            await _scoreRepository.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gửi thông báo điều chỉnh điểm tới Hiệu trưởng.");
        }
    }

    private async Task NotifyPrincipalsAboutBulkScoreChangesAsync(
        int modifierUserId,
        Assessment assessment,
        int modifiedCount,
        string reason)
    {
        try
        {
            var principals = await _scoreRepository.GetUsersByRoleAsync("Principal");
            if (!principals.Any()) return;

            var modifier = await _scoreRepository.GetUserByIdAsync(modifierUserId);
            string modName = modifier?.FullName ?? "Giáo vụ";
            string subName = assessment.TeachingAssignment?.Subject?.SubjectName ?? "Môn học";
            string clsName = assessment.TeachingAssignment?.Class?.ClassName ?? "Lớp";

            foreach (var p in principals)
            {
                var notif = new Notification
                {
                    SenderId = modifierUserId,
                    RecipientId = p.Id,
                    Title = "Cảnh báo điều chỉnh bảng điểm sau khóa",
                    Content = $"Giáo vụ {modName} vừa điều chỉnh điểm cho {modifiedCount} học sinh môn {subName} ({assessment.AssessmentName}) lớp {clsName}. Lý do: {reason}.",
                    SentAt = DateTime.UtcNow,
                    IsRead = false
                };
                await _scoreRepository.AddNotificationAsync(notif);
            }
            await _scoreRepository.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gửi thông báo điều chỉnh bảng điểm tới Hiệu trưởng.");
        }
    }

    public async Task<ServiceResult<SubjectScoreMatrixDto>> GetSubjectScoreMatrixAsync(int assignmentId, int? teacherId = null)
    {
        try
        {
            var assignment = await _scoreRepository.GetTeachingAssignmentWithDetailsAsync(assignmentId);
            if (assignment == null)
            {
                return ServiceResult<SubjectScoreMatrixDto>.Fail(
                    ErrorCodes.AssessmentAssignmentNotFound,
                    "Không tìm thấy phân công giảng dạy tương ứng.");
            }

            if (teacherId.HasValue && assignment.TeacherId != teacherId.Value)
            {
                return ServiceResult<SubjectScoreMatrixDto>.Fail(
                    ErrorCodes.AuthForbidden,
                    "Bạn không có quyền truy cập sổ điểm của phân công này.");
            }

            var enrolledStudents = assignment.Class.StudentClasses
                .Where(sc => sc.Status == "Enrolled" && sc.LeftAt == null)
                .OrderBy(sc => sc.Student.StudentCode)
                .Select(sc => sc.Student)
                .ToList();

            var assessments = assignment.Assessments
                .OrderBy(a => a.AssessmentType.Weight)
                .ThenBy(a => a.ConductedAt)
                .ToList();

            var allScores = await _scoreRepository.GetScoresByAssignmentIdAsync(assignmentId);
            var scoreLookup = allScores.ToLookup(s => s.StudentId);

            var studentRows = new List<StudentSubjectRowDto>();
            foreach (var st in enrolledStudents)
            {
                var studentScores = scoreLookup[st.Id].ToList();
                var scoreDict = new Dictionary<int, decimal?>();
                var scoreIdDict = new Dictionary<int, int?>();
                var isLockedDict = new Dictionary<int, bool>();

                decimal totalWeighted = 0m;
                decimal totalWeight = 0m;

                foreach (var ass in assessments)
                {
                    var sc = studentScores.FirstOrDefault(s => s.AssessmentId == ass.Id);
                    scoreDict[ass.Id] = sc?.Score;
                    scoreIdDict[ass.Id] = sc?.Id;
                    bool isAssLocked = (ass.ConductedAt.HasValue && DateTime.UtcNow >= ass.ConductedAt.Value.AddDays(7)) || (sc?.IsLocked ?? false);
                    isLockedDict[ass.Id] = isAssLocked;

                    if (sc?.Score.HasValue == true)
                    {
                        totalWeighted += sc.Score.Value * ass.AssessmentType.Weight;
                        totalWeight += ass.AssessmentType.Weight;
                    }
                }

                decimal? avg = totalWeight > 0 ? Math.Round(totalWeighted / totalWeight, 1) : null;
                bool isPassed = assignment.Subject.EvaluationType == "PassFail"
                    ? (avg == null || avg >= 5.0m)
                    : (avg == null || avg >= 5.0m);

                studentRows.Add(new StudentSubjectRowDto
                {
                    StudentId = st.Id,
                    StudentCode = st.StudentCode,
                    FullName = st.User.FullName,
                    AssessmentScores = scoreDict,
                    AssessmentScoreIds = scoreIdDict,
                    AssessmentIsLocked = isLockedDict,
                    AverageScore = avg,
                    IsPassed = isPassed
                });
            }

            var matrixDto = new SubjectScoreMatrixDto
            {
                AssignmentId = assignment.Id,
                ClassId = assignment.ClassId,
                ClassName = assignment.Class.ClassName,
                SubjectId = assignment.SubjectId,
                SubjectCode = assignment.Subject.SubjectCode,
                SubjectName = assignment.Subject.SubjectName,
                SemesterId = assignment.SemesterId,
                SemesterName = assignment.Semester.SemesterName,
                AcademicYearCode = assignment.Class.AcademicYear?.YearCode ?? string.Empty,
                TeacherName = assignment.Teacher.User.FullName,
                EvaluationType = assignment.Subject.EvaluationType,
                Assessments = assessments.Select(a =>
                {
                    bool isDeadlinePassed = a.ConductedAt.HasValue && DateTime.UtcNow >= a.ConductedAt.Value.AddDays(7);
                    bool hasLocked = a.StudentScores.Any(s => s.IsLocked);
                    return new AssessmentHeaderDto
                    {
                        Id = a.Id,
                        AssessmentName = a.AssessmentName,
                        TypeCode = a.AssessmentType.TypeCode,
                        TypeName = a.AssessmentType.TypeName,
                        Weight = a.AssessmentType.Weight,
                        IsLocked = isDeadlinePassed || hasLocked,
                        ConductedAt = a.ConductedAt
                    };
                }).ToList(),
                Students = studentRows
            };

            return ServiceResult<SubjectScoreMatrixDto>.Ok(matrixDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tải bảng ma trận điểm môn cho Assignment Id: {AssignmentId}", assignmentId);
            return ServiceResult<SubjectScoreMatrixDto>.Fail(
                ErrorCodes.DatabaseError,
                "Không thể tải sổ điểm bộ môn. Vui lòng thử lại sau.");
        }
    }

    public async Task<ServiceResult<HomeroomScoreReportDto>> GetHomeroomScoreReportAsync(int classId, int? semesterId = null, int? teacherId = null)
    {
        try
        {
            var classEntity = await _scoreRepository.GetClassWithStudentsAsync(classId);
            if (classEntity == null)
            {
                return ServiceResult<HomeroomScoreReportDto>.Fail(
                    ErrorCodes.ClassNotFound,
                    "Không tìm thấy lớp học.");
            }

            if (teacherId.HasValue && classEntity.HomeroomTeacherId != teacherId.Value)
            {
                return ServiceResult<HomeroomScoreReportDto>.Fail(
                    ErrorCodes.AuthForbidden,
                    "Bạn không phải là giáo viên chủ nhiệm của lớp học này.");
            }

            var activeSemesterId = (semesterId.HasValue && semesterId.Value > 0)
                ? semesterId.Value
                : (classEntity.TeachingAssignments.OrderByDescending(ta => ta.Semester.IsActive)
                    .ThenByDescending(ta => ta.Semester.StartDate).FirstOrDefault()?.SemesterId ?? 0);

            if (activeSemesterId == 0)
            {
                var semesterResults = await _scoreRepository.GetClassSubjectResultsAsync(classId, 0);
                activeSemesterId = semesterResults.FirstOrDefault()?.SemesterId ?? 0;
            }

            var semester = await _semesterRepository.GetByIdAsync(activeSemesterId);
            if (semester == null || semester.AcademicYearId != classEntity.AcademicYearId)
                return ServiceResult<HomeroomScoreReportDto>.Fail(ErrorCodes.ValidationFailed, "Học kỳ không thuộc năm học của lớp.");

            var subjects = classEntity.TeachingAssignments
                .Where(ta => ta.SemesterId == activeSemesterId)
                .Select(ta => ta.Subject)
                .DistinctBy(s => s.Id)
                .OrderBy(s => s.SubjectName)
                .Select(s => new SubjectHeaderDto
                {
                    Id = s.Id,
                    AssignmentId = classEntity.TeachingAssignments.First(ta => ta.SemesterId == activeSemesterId && ta.SubjectId == s.Id).Id,
                    SubjectCode = s.SubjectCode,
                    SubjectName = s.SubjectName,
                    EvaluationType = s.EvaluationType
                })
                .ToList();

            var enrolledStudents = classEntity.StudentClasses
                .Where(sc => sc.Status == "Enrolled" && sc.LeftAt == null)
                .OrderBy(sc => sc.Student.StudentCode)
                .Select(sc => sc.Student)
                .ToList();

            var subjectResults = await _scoreRepository.GetClassSubjectResultsAsync(classId, activeSemesterId);
            var subjectResultLookup = subjectResults.ToLookup(r => r.StudentId);

            var reports = await _scoreRepository.GetClassSemesterReportsAsync(classId, activeSemesterId);
            var reportLookup = reports.ToDictionary(r => r.StudentId);

            var studentRows = new List<HomeroomStudentRowDto>();
            foreach (var st in enrolledStudents)
            {
                var studentResults = subjectResultLookup[st.Id].ToList();
                var avgDict = new Dictionary<int, decimal?>();

                decimal sumAvg = 0m;
                int countScoreSubjects = 0;

                foreach (var sub in subjects)
                {
                    var res = studentResults.FirstOrDefault(r => r.SubjectId == sub.Id);
                    avgDict[sub.Id] = res?.AverageScore;

                    if (sub.EvaluationType == "Score" && res?.AverageScore.HasValue == true)
                    {
                        sumAvg += res.AverageScore.Value;
                        countScoreSubjects++;
                    }
                }

                reportLookup.TryGetValue(st.Id, out var rep);
                decimal? gpa = countScoreSubjects > 0 ? Math.Round(sumAvg / countScoreSubjects, 2) : null;

                string academicRank = DetermineAcademicRank(gpa, avgDict.Where(kv => subjects.Any(s => s.Id == kv.Key && s.EvaluationType == "Score")).ToDictionary(kv => kv.Key, kv => kv.Value));

                studentRows.Add(new HomeroomStudentRowDto
                {
                    StudentId = st.Id,
                    StudentCode = st.StudentCode,
                    FullName = st.User.FullName,
                    SubjectAverages = avgDict,
                    Gpa = gpa,
                    AcademicRank = academicRank,
                    ConductGrade = rep?.ConductGrade ?? "Tốt",
                    ClassRank = rep?.ClassRank,
                    Remarks = rep?.TeacherComments
                });
            }

            var reportDto = new HomeroomScoreReportDto
            {
                ClassId = classEntity.Id,
                ClassName = classEntity.ClassName,
                SemesterId = activeSemesterId,
                SemesterName = semester.SemesterName,
                AcademicYearCode = classEntity.AcademicYear?.YearCode ?? string.Empty,
                HomeroomTeacherName = classEntity.HomeroomTeacher?.User?.FullName ?? "Chưa phân công",
                Subjects = subjects,
                Students = studentRows
            };

            return ServiceResult<HomeroomScoreReportDto>.Ok(reportDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tải bảng điểm tổng hợp lớp chủ nhiệm Id: {ClassId}", classId);
            return ServiceResult<HomeroomScoreReportDto>.Fail(
                ErrorCodes.DatabaseError,
                "Không thể tải bảng điểm lớp chủ nhiệm. Vui lòng thử lại sau.");
        }
    }

    public async Task<ServiceResult<StudentTranscriptDto>> GetStudentTranscriptAsync(int studentId, int semesterId, int? requestingUserId = null)
    {
        try
        {
            var student = await _studentRepository.GetByIdAsync(studentId);
            if (student == null)
            {
                return ServiceResult<StudentTranscriptDto>.Fail(
                    ErrorCodes.StudentNotFound,
                    "Không tìm thấy hồ sơ học sinh.");
            }

            var semester = await _semesterRepository.GetByIdAsync(semesterId);
            if (semester == null)
                return ServiceResult<StudentTranscriptDto>.Fail(ErrorCodes.ResourceNotFound, "Không tìm thấy học kỳ.");

            if (requestingUserId.HasValue && !await _scoreRepository.CanUserViewStudentAsync(requestingUserId.Value, studentId, semesterId))
                return ServiceResult<StudentTranscriptDto>.Fail(ErrorCodes.AuthForbidden, "Bạn không có quyền xem bảng điểm học sinh này.");

            var studentScores = await _scoreRepository.GetScoresByStudentAndSemesterAsync(studentId, semesterId);
            var scoresList = studentScores.ToList();

            // Nhóm theo môn học
            var groupedBySubject = scoresList
                .GroupBy(s => s.Assessment.TeachingAssignment.SubjectId)
                .OrderBy(g => g.First().Assessment.TeachingAssignment.Subject.SubjectName)
                .ToList();

            var subjectDetails = new List<StudentSubjectDetailDto>();
            decimal sumGpa = 0m;
            int scoreCount = 0;

            foreach (var group in groupedBySubject)
            {
                var sub = group.First().Assessment.TeachingAssignment.Subject;
                var assessments = group.OrderBy(s => s.Assessment.AssessmentType.Weight).ThenBy(s => s.Assessment.ConductedAt).ToList();

                decimal totalWeighted = 0m;
                decimal totalWeight = 0m;

                var scoreDtos = assessments.Select(s =>
                {
                    if (s.Score.HasValue)
                    {
                        totalWeighted += s.Score.Value * s.Assessment.AssessmentType.Weight;
                        totalWeight += s.Assessment.AssessmentType.Weight;
                    }

                    return new AssessmentScoreDto
                    {
                        AssessmentId = s.AssessmentId,
                        AssessmentName = s.Assessment.AssessmentName,
                        TypeCode = s.Assessment.AssessmentType.TypeCode,
                        TypeName = s.Assessment.AssessmentType.TypeName,
                        Weight = s.Assessment.AssessmentType.Weight,
                        Score = s.Score,
                        Comment = s.Comment,
                        IsLocked = s.IsLocked,
                        ConductedAt = s.Assessment.ConductedAt
                    };
                }).ToList();

                decimal? avg = totalWeight > 0 ? Math.Round(totalWeighted / totalWeight, 1) : null;
                if (sub.EvaluationType == "Score" && avg.HasValue)
                {
                    sumGpa += avg.Value;
                    scoreCount++;
                }

                subjectDetails.Add(new StudentSubjectDetailDto
                {
                    SubjectId = sub.Id,
                    SubjectCode = sub.SubjectCode,
                    SubjectName = sub.SubjectName,
                    TeacherName = assessments.FirstOrDefault()?.Assessment.TeachingAssignment.Teacher.User.FullName ?? string.Empty,
                    EvaluationType = sub.EvaluationType,
                    Assessments = scoreDtos,
                    AverageScore = avg,
                    IsPassed = avg == null || avg >= 5.0m
                });
            }

            var report = await _scoreRepository.GetStudentSemesterReportAsync(studentId, semesterId);
            decimal? gpa = scoreCount > 0 ? Math.Round(sumGpa / scoreCount, 2) : null;

            // Tải danh sách nhận xét của giáo viên
            var feedbacks = await _feedbackRepository.GetByStudentAndSemesterAsync(studentId, semesterId);
            var feedbackDtos = feedbacks.Select(f => new TeacherFeedbackDto
            {
                Id = f.Id,
                StudentId = f.StudentId,
                StudentCode = student.StudentCode,
                StudentName = student.User?.FullName ?? string.Empty,
                TeacherId = f.TeacherId,
                TeacherName = f.Teacher?.User?.FullName ?? "Giáo viên",
                SemesterId = f.SemesterId,
                SemesterName = semester?.SemesterName ?? string.Empty,
                FeedbackType = f.FeedbackType,
                Content = f.Content,
                CreatedAt = f.CreatedAt
            }).ToList();

            var currentClass = await _scoreRepository.GetStudentClassForSemesterAsync(studentId, semesterId);

            var transcriptDto = new StudentTranscriptDto
            {
                StudentId = studentId,
                StudentCode = student.StudentCode,
                FullName = student.User?.FullName ?? "Học sinh",
                ClassName = currentClass?.ClassName ?? (scoresList.FirstOrDefault()?.Assessment.TeachingAssignment.Class.ClassName ?? string.Empty),
                SemesterId = semesterId,
                SemesterName = semester.SemesterName,
                AcademicYearCode = semester.AcademicYear?.YearCode ?? currentClass?.AcademicYear?.YearCode ?? string.Empty,
                Gpa = gpa,
                AcademicRank = DetermineAcademicRank(gpa, subjectDetails.Where(s => s.EvaluationType == "Score").ToDictionary(s => s.SubjectId, s => s.AverageScore)),
                ConductGrade = report?.ConductGrade ?? "Tốt",
                ClassRank = report?.ClassRank,
                TeacherComments = report?.TeacherComments,
                Subjects = subjectDetails,
                Feedbacks = feedbackDtos
            };

            return ServiceResult<StudentTranscriptDto>.Ok(transcriptDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tải bảng điểm cá nhân của học sinh Id: {StudentId}", studentId);
            return ServiceResult<StudentTranscriptDto>.Fail(
                ErrorCodes.DatabaseError,
                "Không thể tải bảng điểm cá nhân. Vui lòng thử lại sau.");
        }
    }

    public async Task<ServiceResult<IEnumerable<ScoreAuditLogDto>>> GetScoreAuditLogsAsync(int scoreId, int? teacherId = null)
    {
        try
        {
            var score = await _scoreRepository.GetScoreWithDetailsAsync(scoreId);
            if (score == null)
                return ServiceResult<IEnumerable<ScoreAuditLogDto>>.Fail(ErrorCodes.ResourceNotFound, "Không tìm thấy điểm số.");
            if (teacherId.HasValue && score.Assessment.TeachingAssignment.TeacherId != teacherId.Value)
                return ServiceResult<IEnumerable<ScoreAuditLogDto>>.Fail(ErrorCodes.AuthForbidden, "Bạn không có quyền xem lịch sử điểm này.");

            var logs = await _scoreRepository.GetScoreAuditLogsByScoreIdAsync(scoreId);
            var dtos = logs.Select(l => new ScoreAuditLogDto
            {
                Id = l.Id,
                StudentScoreId = l.StudentScoreId,
                OldScore = l.OldScore,
                NewScore = l.NewScore,
                ModifiedByName = l.ModifiedByUser?.FullName ?? "Hệ thống",
                ModifiedAt = l.ModifiedAt,
                Reason = l.Reason
            });

            return ServiceResult<IEnumerable<ScoreAuditLogDto>>.Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tải lịch sử chỉnh sửa điểm Id: {ScoreId}", scoreId);
            return ServiceResult<IEnumerable<ScoreAuditLogDto>>.Fail(
                ErrorCodes.DatabaseError,
                "Không thể tải lịch sử chỉnh sửa điểm.");
        }
    }

    public async Task<ServiceResult<bool>> RecalculateStudentSubjectAverageAsync(int studentId, int subjectId, int semesterId)
    {
        try
        {
            var scores = await _scoreRepository.GetScoresByStudentAndSemesterAsync(studentId, semesterId);
            var subjectScores = scores
                .Where(s => s.Assessment.TeachingAssignment.SubjectId == subjectId && s.Score.HasValue)
                .ToList();

            decimal totalWeighted = 0m;
            decimal totalWeight = 0m;

            foreach (var sc in subjectScores)
            {
                totalWeighted += sc.Score!.Value * sc.Assessment.AssessmentType.Weight;
                totalWeight += sc.Assessment.AssessmentType.Weight;
            }

            decimal? avg = totalWeight > 0 ? Math.Round(totalWeighted / totalWeight, 1) : null;
            bool isPassed = avg == null || avg >= 5.0m;

            var resultEntity = new SemesterSubjectResult
            {
                StudentId = studentId,
                SubjectId = subjectId,
                SemesterId = semesterId,
                AverageScore = avg,
                IsPassed = isPassed
            };

            await _scoreRepository.AddOrUpdateSemesterSubjectResultAsync(resultEntity);
            await _scoreRepository.SaveChangesAsync();

            // Nếu học kỳ đã có SemesterReport của học sinh này (ví dụ Hiệu trưởng đã chốt sổ hoặc sau chốt sổ),
            // tự động cập nhật lại GPA và xếp loại học lực trong SemesterReport
            var existingReport = await _scoreRepository.GetStudentSemesterReportAsync(studentId, semesterId);
            if (existingReport != null)
            {
                var allStudentScores = await _scoreRepository.GetScoresByStudentAndSemesterAsync(studentId, semesterId);
                var subjectGroups = allStudentScores
                    .GroupBy(s => s.Assessment.TeachingAssignment.SubjectId)
                    .ToList();

                decimal sumAvg = 0m;
                int countAvg = 0;
                var subjectAvgDict = new Dictionary<int, decimal?>();

                foreach (var grp in subjectGroups)
                {
                    decimal wSum = 0m;
                    decimal wTot = 0m;
                    foreach (var s in grp.Where(x => x.Score.HasValue))
                    {
                        wSum += s.Score!.Value * s.Assessment.AssessmentType.Weight;
                        wTot += s.Assessment.AssessmentType.Weight;
                    }
                    if (wTot > 0)
                    {
                        var sAvg = Math.Round(wSum / wTot, 1);
                        sumAvg += sAvg;
                        countAvg++;
                        subjectAvgDict[grp.Key] = sAvg;
                    }
                }

                if (countAvg > 0)
                {
                    decimal newGpa = Math.Round(sumAvg / countAvg, 2);
                    existingReport.Gpa = newGpa;
                    existingReport.AcademicRank = DetermineAcademicRank(newGpa, subjectAvgDict);
                    await _scoreRepository.AddOrUpdateSemesterReportAsync(existingReport);
                    await _scoreRepository.SaveChangesAsync();
                }
            }

            return ServiceResult<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tính lại điểm trung bình môn cho Student {StudentId}, Subject {SubjectId}", studentId, subjectId);
            return ServiceResult<bool>.Fail(ErrorCodes.DatabaseError, "Lỗi tính toán điểm trung bình môn.");
        }
    }

    private static string DetermineAcademicRank(decimal? gpa, Dictionary<int, decimal?> subjectAverages)
    {
        if (!gpa.HasValue) return "Chưa xếp loại";

        bool hasBelow5 = subjectAverages.Values.Any(v => v.HasValue && v.Value < 5.0m);
        bool hasBelow65 = subjectAverages.Values.Any(v => v.HasValue && v.Value < 6.5m);

        if (gpa.Value >= 8.0m && !hasBelow65) return "Giỏi";
        if (gpa.Value >= 6.5m && !hasBelow5) return "Khá";
        if (gpa.Value >= 5.0m) return "Đạt";
        return "Chưa đạt";
    }
}
