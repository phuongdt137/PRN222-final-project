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
                    IsLocked = scoreEntity?.IsLocked ?? false,
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

    public async Task<ServiceResult<StudentScoreItemDto>> SaveSingleScoreAsync(SaveStudentScoreDto dto, int modifierUserId, int? teacherId = null)
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

            if (teacherId.HasValue && assessment.TeachingAssignment?.TeacherId != teacherId.Value)
            {
                return ServiceResult<StudentScoreItemDto>.Fail(
                    ErrorCodes.AuthForbidden,
                    "Bạn không có quyền nhập điểm cho bài kiểm tra này.");
            }

            if (dto.Score.HasValue && (dto.Score.Value < 0 || dto.Score.Value > 10))
            {
                return ServiceResult<StudentScoreItemDto>.Fail(
                    ErrorCodes.ScoreOutOfRange,
                    "Điểm số phải nằm trong thang điểm từ 0.0 đến 10.0.");
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

            if (teacherId.HasValue && assessment.StudentScores.Any(s => s.IsLocked))
                return ServiceResult<StudentScoreItemDto>.Fail(ErrorCodes.ScoreLocked, "Bảng điểm đã chốt nộp. Giáo viên không thể điều chỉnh.");

            if (!teacherId.HasValue && existingScore != null && existingScore.Score != dto.Score && string.IsNullOrWhiteSpace(dto.Reason))
                return ServiceResult<StudentScoreItemDto>.Fail(ErrorCodes.ScoreReasonRequired, "Giáo vụ phải nhập lý do điều chỉnh điểm.");

            await using var transaction = await _scoreRepository.BeginTransactionAsync();
            if (existingScore != null)
            {
                if (existingScore.IsLocked && teacherId.HasValue)
                {
                    return ServiceResult<StudentScoreItemDto>.Fail(
                        ErrorCodes.ScoreLocked,
                        "Cột điểm của học sinh này đã bị khóa. Vui lòng liên hệ quản lý để mở khóa nếu cần sửa.");
                }

                if (existingScore.Score != dto.Score)
                {
                    var auditLog = new ScoreAuditLog
                    {
                        StudentScoreId = existingScore.Id,
                        OldScore = existingScore.Score,
                        NewScore = dto.Score,
                        ModifiedBy = modifierUserId,
                        ModifiedAt = DateTime.UtcNow,
                        Reason = string.IsNullOrWhiteSpace(dto.Reason) ? "Điều chỉnh điểm số" : dto.Reason.Trim()
                    };
                    await _scoreRepository.AddScoreAuditLogAsync(auditLog);
                }

                existingScore.Score = dto.Score;
                existingScore.Comment = dto.Comment?.Trim();
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
                    IsLocked = !teacherId.HasValue && assessment.StudentScores.Any(s => s.IsLocked)
                };
                await _scoreRepository.AddScoreAsync(existingScore);
            }

            await _scoreRepository.SaveChangesAsync();

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

    public async Task<ServiceResult<int>> SaveBulkScoresAsync(BulkSaveScoreDto dto, int modifierUserId, int? teacherId = null)
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
            bool isStaff = !isTeacher;

            if (isTeacher)
            {
                if (assessment.TeachingAssignment?.TeacherId != teacherId!.Value)
                {
                    return ServiceResult<int>.Fail(
                        ErrorCodes.AuthForbidden,
                        "Bạn không có quyền nhập điểm cho bài kiểm tra này.");
                }

                // Kiểm tra nếu bài kiểm tra đã bị khóa điểm (đã nộp chính thức trước đó)
                bool isAlreadyLocked = assessment.StudentScores.Any(s => s.IsLocked);
                if (isAlreadyLocked)
                {
                    return ServiceResult<int>.Fail(
                        ErrorCodes.AssessmentAlreadyLockedForTeacher,
                        "Bài kiểm tra này đã được chốt nộp và khóa điểm. Giáo viên không thể chỉnh sửa điểm số. Vui lòng liên hệ phòng Giáo vụ (Staff) nếu cần điều chỉnh.");
                }
            }

            var enrolledStudents = assessment.TeachingAssignment?.Class?.StudentClasses
                .Where(sc => sc.Status == "Enrolled" && sc.LeftAt == null)
                .OrderBy(sc => sc.Student.StudentCode)
                .Select(sc => sc.Student)
                .ToList() ?? new List<Student>();

            // Validate điểm hợp lệ từ 0.0 đến 10.0
            var enrolledIds = enrolledStudents.Select(s => s.Id).ToHashSet();
            if (dto.Scores.Any(s => !enrolledIds.Contains(s.StudentId)))
                return ServiceResult<int>.Fail(ErrorCodes.ScoreStudentNotInClass, "Danh sách gửi lên có học sinh không thuộc lớp của bài kiểm tra.");

            foreach (var item in dto.Scores)
            {
                if (item.Score.HasValue && (item.Score.Value < 0 || item.Score.Value > 10))
                {
                    return ServiceResult<int>.Fail(
                        ErrorCodes.ScoreOutOfRange,
                        $"Điểm số {item.Score.Value} của học sinh Id {item.StudentId} không hợp lệ (phải nằm trong thang điểm từ 0.0 đến 10.0).");
                }
            }

            // Nếu là Teacher và nộp chính thức (!dto.IsDraft)
            if (isTeacher && !dto.IsDraft)
            {
                // 1. Kiểm tra phải nhập đủ 100% học sinh trong lớp (vắng thi thì điểm 0)
                var submittedScoreMap = dto.Scores
                    .Where(s => s.Score.HasValue)
                    .ToDictionary(s => s.StudentId);

                var missingScoreStudents = enrolledStudents
                    .Where(s => !submittedScoreMap.ContainsKey(s.Id))
                    .ToList();

                if (missingScoreStudents.Any())
                {
                    var names = string.Join(", ", missingScoreStudents.Take(3).Select(s => s.User.FullName));
                    var extra = missingScoreStudents.Count > 3 ? $" và {missingScoreStudents.Count - 3} học sinh khác" : "";
                    return ServiceResult<int>.Fail(
                        ErrorCodes.AssessmentSubmissionIncomplete,
                        $"Để chốt nộp bảng điểm, bạn bắt buộc phải nhập điểm cho toàn bộ {enrolledStudents.Count} học sinh trong lớp. Hiện còn thiếu điểm của: {names}{extra}. Trường hợp học sinh vắng thi, vui lòng cho điểm 0 kèm lý do ở phần nhận xét.");
                }

                // 2. Kiểm tra tất cả học sinh đều phải có nhận xét
                var missingCommentStudents = enrolledStudents
                    .Where(s =>
                    {
                        var row = dto.Scores.FirstOrDefault(x => x.StudentId == s.Id);
                        return row == null || string.IsNullOrWhiteSpace(row.Comment);
                    })
                    .ToList();

                if (missingCommentStudents.Any())
                {
                    var names = string.Join(", ", missingCommentStudents.Take(3).Select(s => s.User.FullName));
                    var extra = missingCommentStudents.Count > 3 ? $" và {missingCommentStudents.Count - 3} học sinh khác" : "";
                    return ServiceResult<int>.Fail(
                        ErrorCodes.ScoreCommentRequired,
                        $"Tất cả học sinh đều phải có nhận xét trước khi chốt nộp. Hiện còn thiếu nhận xét của: {names}{extra}. Đối với học sinh vắng thi (điểm 0), bắt buộc ghi rõ lý do vắng thi.");
                }

                // Nộp chính thức sẽ khóa điểm vĩnh viễn đối với giáo viên
                dto.LockAfterSave = true;
            }
            else if (isTeacher && dto.IsDraft)
            {
                // Lưu nháp: không khóa điểm
                dto.LockAfterSave = false;
            }
            else if (isStaff)
            {
                // Giáo vụ chỉnh sửa điểm đã khóa
                var globalReason = !string.IsNullOrWhiteSpace(dto.GlobalReason) ? dto.GlobalReason.Trim() : null;

                foreach (var item in dto.Scores)
                {
                    var existingScore = await _scoreRepository.GetScoreByStudentAndAssessmentAsync(item.StudentId, dto.AssessmentId);
                    if (existingScore != null && existingScore.Score != item.Score &&
                        string.IsNullOrWhiteSpace(globalReason) && string.IsNullOrWhiteSpace(item.Reason))
                    {
                        return ServiceResult<int>.Fail(ErrorCodes.ScoreReasonRequired, "Mỗi điểm điều chỉnh phải có lý do riêng hoặc lý do chung.");
                    }
                }

                // Khi giáo vụ sửa xong, điểm vẫn tiếp tục ở trạng thái khóa
                dto.LockAfterSave = true;
                dto.IsDraft = false;
            }

            await using var transaction = await _scoreRepository.BeginTransactionAsync();
            int savedCount = 0;
            var affectedStudentIds = new HashSet<int>();

            foreach (var item in dto.Scores)
            {
                var existingScore = await _scoreRepository.GetScoreByStudentAndAssessmentAsync(item.StudentId, dto.AssessmentId);

                if (existingScore != null)
                {
                    // Nếu là Giáo viên và bản ghi điểm này đã bị khóa riêng lẻ
                    if (isTeacher && existingScore.IsLocked && existingScore.Score != item.Score)
                    {
                        continue;
                    }

                    // Ghi vết kiểm toán (Audit Log) nếu điểm số bị thay đổi
                    if (existingScore.Score != item.Score)
                    {
                        var reason = !string.IsNullOrWhiteSpace(item.Reason) 
                            ? item.Reason.Trim() 
                            : (!string.IsNullOrWhiteSpace(dto.GlobalReason) ? dto.GlobalReason.Trim() : (dto.IsDraft ? "Lưu nháp điểm" : "Nhập điểm chính thức"));

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
                    }

                    existingScore.Score = item.Score;
                    existingScore.Comment = item.Comment?.Trim();
                    if (dto.LockAfterSave)
                    {
                        existingScore.IsLocked = true;
                    }
                    else if (dto.IsDraft)
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
                            IsLocked = dto.LockAfterSave
                        };
                        await _scoreRepository.AddScoreAsync(newScore);
                    }
                }

                savedCount++;
                affectedStudentIds.Add(item.StudentId);
            }

            await _scoreRepository.SaveChangesAsync();

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

            string resultMessage;
            if (dto.IsDraft)
            {
                resultMessage = $"Đã lưu nháp bảng điểm thành công ({savedCount} học sinh). Bảng điểm chưa bị khóa, bạn có thể tiếp tục chỉnh sửa trước khi chốt nộp.";
            }
            else if (isStaff)
            {
                resultMessage = $"Đã cập nhật bảng điểm thành công ({savedCount} học sinh). Lịch sử điều chỉnh đã được lưu vết kiểm toán.";
            }
            else
            {
                resultMessage = $"Đã chốt và nộp bảng điểm thành công ({savedCount} học sinh). Bảng điểm đã được khóa chính thức theo quy chế.";
            }

            return ServiceResult<int>.Ok(savedCount, resultMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lưu điểm hàng loạt cho bài kiểm tra Id: {AssessmentId}", dto.AssessmentId);
            return ServiceResult<int>.Fail(
                ErrorCodes.DatabaseError,
                "Không thể lưu điểm hàng loạt. Vui lòng thử lại sau.");
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
                    isLockedDict[ass.Id] = sc?.IsLocked ?? false;

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
                Assessments = assessments.Select(a => new AssessmentHeaderDto
                {
                    Id = a.Id,
                    AssessmentName = a.AssessmentName,
                    TypeCode = a.AssessmentType.TypeCode,
                    TypeName = a.AssessmentType.TypeName,
                    Weight = a.AssessmentType.Weight
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
