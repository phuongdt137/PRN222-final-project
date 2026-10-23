using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Repositories.Data;
using HighSchoolManagementSystem.Services.Common;
using HighSchoolManagementSystem.Services.DTOs;
using HighSchoolManagementSystem.Services.Interfaces;

namespace HighSchoolManagementSystem.Services.Implementations;

public class PrincipalService : IPrincipalService
{
    private readonly AppDbContext _context;
    private readonly ILogger<PrincipalService> _logger;

    public PrincipalService(AppDbContext context, ILogger<PrincipalService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ServiceResult<PrincipalDashboardDto>> GetDashboardStatsAsync(int? currentYearId = null, int? currentSemesterId = null)
    {
        try
        {
            var dto = new PrincipalDashboardDto();

            // 1. Năm học & Học kỳ hiện hành
            Semester? activeSemester = null;
            if (currentSemesterId.HasValue)
            {
                activeSemester = await _context.Semesters
                    .Include(s => s.AcademicYear)
                    .FirstOrDefaultAsync(s => s.Id == currentSemesterId.Value);
            }
            else
            {
                activeSemester = await _context.Semesters
                    .Include(s => s.AcademicYear)
                    .FirstOrDefaultAsync(s => s.IsActive);
            }

            if (activeSemester == null)
            {
                activeSemester = await _context.Semesters
                    .Include(s => s.AcademicYear)
                    .OrderByDescending(s => s.StartDate)
                    .FirstOrDefaultAsync();
            }

            if (activeSemester != null)
            {
                dto.CurrentSemesterId = activeSemester.Id;
                dto.CurrentSemesterName = activeSemester.SemesterName;
                dto.CurrentYearCode = activeSemester.AcademicYear?.YearCode ?? string.Empty;
            }

            // 2. Tổng số học sinh & phân bổ theo khối
            var activeStudentsQuery = _context.Students.Where(s => s.Status == "Active");
            dto.TotalStudents = await activeStudentsQuery.CountAsync();

            var gradeLevels = await _context.GradeLevels.OrderBy(g => g.Id).ToListAsync();
            foreach (var grade in gradeLevels)
            {
                int countInGrade = await _context.StudentClasses
                    .Where(sc => sc.Status == "Enrolled" && sc.LeftAt == null && sc.Class.GradeLevelId == grade.Id)
                    .Select(sc => sc.StudentId)
                    .Distinct()
                    .CountAsync();

                dto.StudentsByGrade[grade.LevelName] = countInGrade;
            }

            // 3. Đội ngũ giáo viên & cán bộ
            dto.TotalTeachers = await _context.Teachers.CountAsync();
            dto.ActiveTeachers = await _context.Teachers.CountAsync(t => t.Status == "Active");

            // 4. Lớp học & phòng học
            var classesQuery = _context.Classes.AsQueryable();
            if (activeSemester != null)
            {
                classesQuery = classesQuery.Where(c => c.AcademicYearId == activeSemester.AcademicYearId);
            }
            dto.TotalClasses = await classesQuery.CountAsync();

            foreach (var grade in gradeLevels)
            {
                int classCount = await classesQuery.CountAsync(c => c.GradeLevelId == grade.Id);
                dto.ClassesByGrade[grade.LevelName] = classCount;
            }

            dto.TotalRooms = await _context.Rooms.CountAsync(r => r.Status == "Available" || r.Status == "InUse");

            // 5. Chuyên cần hôm nay
            var today = DateTime.Today;
            var todayAttendance = await _context.AttendanceRecords
                .Include(a => a.Student).ThenInclude(s => s.StudentClasses).ThenInclude(sc => sc.Class)
                .Where(a => a.AttendanceDate.Date == today)
                .ToListAsync();

            if (todayAttendance.Any())
            {
                dto.TodayTotalAttendanceRecords = todayAttendance.Count;
                dto.TodayPresentCount = todayAttendance.Count(a => a.Status == "Present");
                dto.TodayExcusedCount = todayAttendance.Count(a => a.Status == "Excused");
                dto.TodayUnexcusedCount = todayAttendance.Count(a => a.Status == "Absent");
                dto.TodayAttendanceRate = dto.TodayTotalAttendanceRecords > 0
                    ? Math.Round((decimal)dto.TodayPresentCount / dto.TodayTotalAttendanceRecords * 100m, 1)
                    : 100m;
            }
            else
            {
                dto.TodayTotalAttendanceRecords = dto.TotalStudents;
                dto.TodayPresentCount = dto.TotalStudents;
                dto.TodayExcusedCount = 0;
                dto.TodayUnexcusedCount = 0;
                dto.TodayAttendanceRate = dto.TotalStudents > 0 ? 100m : 0m;
            }

            // Chuyên cần theo từng khối
            foreach (var grade in gradeLevels)
            {
                int gradeStudents = dto.StudentsByGrade.GetValueOrDefault(grade.LevelName, 0);
                var gradeRecords = todayAttendance.Where(a => a.Student.StudentClasses
                    .Any(sc => sc.Status == "Enrolled" && sc.Class.GradeLevelId == grade.Id)).ToList();

                int present = gradeRecords.Any() ? gradeRecords.Count(a => a.Status == "Present") : gradeStudents;
                int excused = gradeRecords.Count(a => a.Status == "Excused");
                int unexcused = gradeRecords.Count(a => a.Status == "Absent");
                decimal rate = gradeStudents > 0
                    ? Math.Round((decimal)present / (present + excused + unexcused > 0 ? (present + excused + unexcused) : gradeStudents) * 100m, 1)
                    : 100m;

                dto.AttendanceByGrade.Add(new GradeAttendanceKpiDto
                {
                    GradeName = grade.LevelName,
                    TotalStudents = gradeStudents,
                    PresentCount = present,
                    ExcusedCount = excused,
                    UnexcusedCount = unexcused,
                    AttendanceRate = rate
                });
            }

            // 6. Nề nếp & Kỷ luật mới nhất
            var recentEvaluations = await _context.ConductEvaluations
                .Include(ce => ce.Student).ThenInclude(s => s.User)
                .Include(ce => ce.Student).ThenInclude(s => s.StudentClasses).ThenInclude(sc => sc.Class)
                .Include(ce => ce.HomeroomTeacher).ThenInclude(t => t.User)
                .OrderByDescending(ce => ce.EvaluatedAt)
                .Take(5)
                .ToListAsync();

            foreach (var ce in recentEvaluations)
            {
                var studentClass = ce.Student.StudentClasses.FirstOrDefault(sc => sc.Status == "Enrolled" && sc.LeftAt == null)?.Class?.ClassName ?? "Chưa xếp";
                dto.RecentConductEvents.Add(new RecentConductEventDto
                {
                    EvaluatedAt = ce.EvaluatedAt,
                    StudentName = ce.Student.User?.FullName ?? "N/A",
                    StudentCode = ce.Student.StudentCode,
                    ClassName = studentClass,
                    ConductGrade = ce.ConductGrade,
                    TeacherComment = ce.TeacherComment,
                    TeacherName = ce.HomeroomTeacher?.User?.FullName ?? "N/A"
                });
            }

            // Phân bổ hạnh kiểm toàn trường
            var conductSummary = await _context.ConductEvaluations
                .Where(ce => activeSemester == null || ce.SemesterId == activeSemester.Id)
                .GroupBy(ce => ce.ConductGrade)
                .Select(g => new { Grade = g.Key, Count = g.Count() })
                .ToListAsync();

            foreach (var item in conductSummary)
            {
                dto.ConductDistribution[item.Grade] = item.Count;
            }

            // 7. Nhat ky dieu chinh diem so sau khoa (Giam sat cua Hieu truong)
            var recentAudits = await _context.ScoreAuditLogs
                .AsNoTracking()
                .Include(l => l.ModifiedByUser)
                .Include(l => l.StudentScore).ThenInclude(ss => ss.Student).ThenInclude(st => st.User)
                .Include(l => l.StudentScore).ThenInclude(ss => ss.Student).ThenInclude(st => st.StudentClasses).ThenInclude(sc => sc.Class)
                .Include(l => l.StudentScore).ThenInclude(ss => ss.Assessment).ThenInclude(a => a.TeachingAssignment).ThenInclude(ta => ta.Class)
                .Include(l => l.StudentScore).ThenInclude(ss => ss.Assessment).ThenInclude(a => a.TeachingAssignment).ThenInclude(ta => ta.Subject)
                .OrderByDescending(l => l.ModifiedAt)
                .Take(10)
                .ToListAsync();

            foreach (var log in recentAudits)
            {
                var assessment = log.StudentScore?.Assessment;
                var ta = assessment?.TeachingAssignment;
                var cls = ta?.Class?.ClassName ?? log.StudentScore?.Student?.StudentClasses.FirstOrDefault(sc => sc.Status == "Enrolled" && sc.LeftAt == null)?.Class?.ClassName ?? "N/A";

                dto.RecentScoreModifications.Add(new RecentScoreAuditDto
                {
                    Id = log.Id,
                    StudentName = log.StudentScore?.Student?.User?.FullName ?? "N/A",
                    StudentCode = log.StudentScore?.Student?.StudentCode ?? "N/A",
                    ClassName = cls,
                    SubjectName = ta?.Subject?.SubjectName ?? "N/A",
                    AssessmentName = assessment?.AssessmentName ?? "N/A",
                    OldScore = log.OldScore,
                    NewScore = log.NewScore,
                    ModifiedByName = log.ModifiedByUser?.FullName ?? "Hệ thống",
                    ModifiedAt = log.ModifiedAt,
                    Reason = log.Reason
                });
            }

            // 8. Thong bao gui den Hieu truong gan day
            var principalNotifications = await _context.Notifications
                .AsNoTracking()
                .Include(n => n.Sender)
                .Where(n => n.Recipient.Role.RoleName == "Principal")
                .OrderByDescending(n => n.SentAt)
                .Take(10)
                .ToListAsync();

            foreach (var notif in principalNotifications)
            {
                dto.RecentNotifications.Add(new NotificationItemDto
                {
                    Id = notif.Id,
                    Title = notif.Title,
                    Content = notif.Content,
                    SentAt = notif.SentAt,
                    IsRead = notif.IsRead,
                    SenderName = notif.Sender?.FullName ?? "Hệ thống"
                });
            }

            return ServiceResult<PrincipalDashboardDto>.Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy dữ liệu Dashboard Hiệu trưởng.");
            return ServiceResult<PrincipalDashboardDto>.Fail(ErrorCodes.SystemError, "Không thể tải số liệu thống kê Dashboard.");
        }
    }

    public async Task<ServiceResult<AcademicReviewPageDto>> GetAcademicReviewDataAsync(int semesterId, int? gradeLevelId = null, int? classId = null)
    {
        try
        {
            Semester? semester = null;
            if (semesterId <= 0)
            {
                semester = await _context.Semesters
                    .Include(s => s.AcademicYear)
                    .FirstOrDefaultAsync(s => s.IsActive);
                if (semester == null)
                {
                    semester = await _context.Semesters.Include(s => s.AcademicYear).OrderByDescending(s => s.StartDate).FirstOrDefaultAsync();
                }
                semesterId = semester?.Id ?? 0;
            }
            else
            {
                semester = await _context.Semesters
                    .Include(s => s.AcademicYear)
                    .FirstOrDefaultAsync(s => s.Id == semesterId);
            }

            if (semester == null)
            {
                return ServiceResult<AcademicReviewPageDto>.Fail(ErrorCodes.SemesterNotFound, "Không tìm thấy học kỳ yêu cầu.");
            }

            bool isFinalized = await _context.SemesterReports.AnyAsync(sr => sr.SemesterId == semesterId);

            var dto = new AcademicReviewPageDto
            {
                SemesterId = semesterId,
                SemesterName = semester.SemesterName,
                AcademicYearCode = semester.AcademicYear?.YearCode ?? string.Empty,
                IsSemesterFinalized = isFinalized
            };

            var classesQuery = _context.Classes
                .Include(c => c.GradeLevel)
                .Include(c => c.HomeroomTeacher)!.ThenInclude(t => t!.User)
                .Include(c => c.StudentClasses.Where(sc => sc.Status != "Transferred")).ThenInclude(sc => sc.Student)
                .Where(c => c.AcademicYearId == semester.AcademicYearId);

            if (gradeLevelId.HasValue)
            {
                classesQuery = classesQuery.Where(c => c.GradeLevelId == gradeLevelId.Value);
            }
            if (classId.HasValue)
            {
                classesQuery = classesQuery.Where(c => c.Id == classId.Value);
            }

            var classes = await classesQuery.OrderBy(c => c.GradeLevelId).ThenBy(c => c.ClassName).ToListAsync();
            dto.TotalClasses = classes.Count;

            var savedReports = await _context.SemesterReports
                .Where(sr => sr.SemesterId == semesterId)
                .ToListAsync();

            var subjectResults = await _context.SemesterSubjectResults
                .Where(ssr => ssr.SemesterId == semesterId && ssr.AverageScore.HasValue)
                .ToListAsync();

            decimal totalSchoolGpa = 0m;
            int evaluatedSchoolCount = 0;

            foreach (var cls in classes)
            {
                var studentIds = cls.StudentClasses.Select(sc => sc.StudentId).ToList();
                int studentCount = studentIds.Count;
                dto.TotalStudents += studentCount;

                var classReports = savedReports.Where(r => studentIds.Contains(r.StudentId)).ToList();
                int gradedCount = 0;
                decimal sumClassGpa = 0m;
                int exc = 0, good = 0, pass = 0, fail = 0;

                if (classReports.Any())
                {
                    foreach (var rep in classReports)
                    {
                        if (rep.Gpa.HasValue)
                        {
                            gradedCount++;
                            sumClassGpa += rep.Gpa.Value;
                            totalSchoolGpa += rep.Gpa.Value;
                            evaluatedSchoolCount++;

                            var rank = rep.AcademicRank ?? "Chưa đạt";
                            if (rank.Contains("Giỏi", StringComparison.OrdinalIgnoreCase)) exc++;
                            else if (rank.Contains("Khá", StringComparison.OrdinalIgnoreCase)) good++;
                            else if (rank.Contains("Đạt", StringComparison.OrdinalIgnoreCase)) pass++;
                            else fail++;

                            dto.AcademicRankCounts[rank] = dto.AcademicRankCounts.GetValueOrDefault(rank, 0) + 1;
                        }
                    }
                }
                else
                {
                    foreach (var sid in studentIds)
                    {
                        var studentSubjects = subjectResults.Where(sr => sr.StudentId == sid).ToList();
                        if (studentSubjects.Any())
                        {
                            gradedCount++;
                            decimal gpa = Math.Round(studentSubjects.Average(sr => sr.AverageScore!.Value), 2);
                            sumClassGpa += gpa;
                            totalSchoolGpa += gpa;
                            evaluatedSchoolCount++;

                            bool below5 = studentSubjects.Any(sr => sr.AverageScore < 5.0m);
                            bool below65 = studentSubjects.Any(sr => sr.AverageScore < 6.5m);

                            string rank = "Chưa đạt";
                            if (gpa >= 8.0m && !below65) { rank = "Giỏi"; exc++; }
                            else if (gpa >= 6.5m && !below5) { rank = "Khá"; good++; }
                            else if (gpa >= 5.0m) { rank = "Đạt"; pass++; }
                            else { fail++; }

                            dto.AcademicRankCounts[rank] = dto.AcademicRankCounts.GetValueOrDefault(rank, 0) + 1;
                        }
                    }
                }

                dto.EvaluatedStudentsCount += gradedCount;

                dto.ClassReviews.Add(new ClassAcademicReviewDto
                {
                    ClassId = cls.Id,
                    ClassName = cls.ClassName,
                    GradeLevelName = cls.GradeLevel?.LevelName ?? string.Empty,
                    HomeroomTeacherName = cls.HomeroomTeacher?.User?.FullName,
                    StudentCount = studentCount,
                    GradedStudentCount = gradedCount,
                    AverageGpa = gradedCount > 0 ? Math.Round(sumClassGpa / gradedCount, 2) : null,
                    ExcellentCount = exc,
                    GoodCount = good,
                    PassCount = pass,
                    FailCount = fail
                });
            }

            dto.SchoolAverageGpa = evaluatedSchoolCount > 0 ? Math.Round(totalSchoolGpa / evaluatedSchoolCount, 2) : 0m;
            return ServiceResult<AcademicReviewPageDto>.Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy dữ liệu rà soát học kỳ {SemesterId}", semesterId);
            return ServiceResult<AcademicReviewPageDto>.Fail(ErrorCodes.SystemError, "Không thể tải dữ liệu rà soát học kỳ.");
        }
    }

    public async Task<ServiceResult<bool>> FinalizeSemesterAsync(int semesterId, int principalUserId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var semester = await _context.Semesters
                .Include(s => s.AcademicYear)
                .FirstOrDefaultAsync(s => s.Id == semesterId);
            if (semester == null)
            {
                return ServiceResult<bool>.Fail(ErrorCodes.SemesterNotFound, "Học kỳ không tồn tại.");
            }

            bool alreadyFinalized = await _context.SemesterReports.AnyAsync(sr => sr.SemesterId == semesterId);
            if (alreadyFinalized)
            {
                return ServiceResult<bool>.Fail(ErrorCodes.ValidationFailed, "Học kỳ này đã được chốt sổ trước đó.");
            }

            // 1. Lấy tất cả lớp thuộc năm học của học kỳ
            var classes = await _context.Classes
                .Include(c => c.StudentClasses.Where(sc => sc.Status != "Transferred"))
                .Where(c => c.AcademicYearId == semester.AcademicYearId)
                .ToListAsync();

            // 2. Lấy toàn bộ điểm đánh giá của học kỳ
            var scores = await _context.StudentScores
                .Include(ss => ss.Assessment).ThenInclude(a => a.AssessmentType)
                .Include(ss => ss.Assessment).ThenInclude(a => a.TeachingAssignment)
                .Where(ss => ss.Assessment.TeachingAssignment.SemesterId == semesterId)
                .ToListAsync();

            // 3. Lấy đánh giá hạnh kiểm
            var conducts = await _context.ConductEvaluations
                .Where(ce => ce.SemesterId == semesterId)
                .ToDictionaryAsync(ce => ce.StudentId, ce => ce);

            // 4. Nhận xét của giáo viên
            var feedbacks = await _context.TeacherFeedbacks
                .Where(tf => tf.SemesterId == semesterId)
                .GroupBy(tf => tf.StudentId)
                .ToDictionaryAsync(g => g.Key, g => string.Join("; ", g.Select(f => f.Content)));

            // 5. Tính toán kết quả cho từng lớp
            foreach (var cls in classes)
            {
                var classStudentIds = cls.StudentClasses.Select(sc => sc.StudentId).ToList();
                var studentGpas = new List<(int StudentId, decimal Gpa, string Rank, string Conduct, string? Comment)>();

                foreach (var studentId in classStudentIds)
                {
                    var studentScores = scores.Where(ss => ss.StudentId == studentId && ss.Score.HasValue).ToList();
                    var subjectGroups = studentScores.GroupBy(ss => ss.Assessment.TeachingAssignment.SubjectId).ToList();

                    decimal sumSubjectAvg = 0m;
                    int subjectCount = 0;
                    bool hasBelow5 = false;
                    bool hasBelow65 = false;

                    foreach (var group in subjectGroups)
                    {
                        int subjectId = group.Key;
                        decimal totalWeight = 0m;
                        decimal weightedScore = 0m;

                        foreach (var sc in group)
                        {
                            decimal weight = sc.Assessment.AssessmentType?.Weight ?? 1m;
                            weightedScore += sc.Score!.Value * weight;
                            totalWeight += weight;
                        }

                        decimal subjectAvg = totalWeight > 0 ? Math.Round(weightedScore / totalWeight, 2) : 0m;
                        sumSubjectAvg += subjectAvg;
                        subjectCount++;

                        if (subjectAvg < 5.0m) hasBelow5 = true;
                        if (subjectAvg < 6.5m) hasBelow65 = true;

                        var existingSsr = await _context.SemesterSubjectResults
                            .FirstOrDefaultAsync(r => r.StudentId == studentId && r.SubjectId == subjectId && r.SemesterId == semesterId);

                        if (existingSsr == null)
                        {
                            _context.SemesterSubjectResults.Add(new SemesterSubjectResult
                            {
                                StudentId = studentId,
                                SubjectId = subjectId,
                                SemesterId = semesterId,
                                AverageScore = subjectAvg,
                                IsPassed = subjectAvg >= 5.0m
                            });
                        }
                        else
                        {
                            existingSsr.AverageScore = subjectAvg;
                            existingSsr.IsPassed = subjectAvg >= 5.0m;
                        }
                    }

                    decimal gpa = subjectCount > 0 ? Math.Round(sumSubjectAvg / subjectCount, 2) : 0m;

                    string academicRank = "Chưa đạt";
                    if (gpa >= 8.0m && !hasBelow65) academicRank = "Giỏi";
                    else if (gpa >= 6.5m && !hasBelow5) academicRank = "Khá";
                    else if (gpa >= 5.0m) academicRank = "Đạt";

                    string conductGrade = conducts.TryGetValue(studentId, out var ce) ? ce.ConductGrade : "Tốt";
                    string? comment = feedbacks.GetValueOrDefault(studentId);

                    studentGpas.Add((studentId, gpa, academicRank, conductGrade, comment));
                }

                // Xếp thứ hạng lớp (Class Rank) theo GPA giảm dần
                var rankedStudents = studentGpas.OrderByDescending(x => x.Gpa).ToList();
                for (int rankIndex = 0; rankIndex < rankedStudents.Count; rankIndex++)
                {
                    var item = rankedStudents[rankIndex];
                    int classRank = rankIndex + 1;

                    var existingReport = await _context.SemesterReports
                        .FirstOrDefaultAsync(sr => sr.StudentId == item.StudentId && sr.SemesterId == semesterId);

                    if (existingReport == null)
                    {
                        _context.SemesterReports.Add(new SemesterReport
                        {
                            StudentId = item.StudentId,
                            SemesterId = semesterId,
                            Gpa = item.Gpa,
                            AcademicRank = item.Rank,
                            ConductGrade = item.Conduct,
                            ClassRank = classRank,
                            TeacherComments = item.Comment
                        });
                    }
                    else
                    {
                        existingReport.Gpa = item.Gpa;
                        existingReport.AcademicRank = item.Rank;
                        existingReport.ConductGrade = item.Conduct;
                        existingReport.ClassRank = classRank;
                        existingReport.TeacherComments = item.Comment;
                    }
                }
            }

            // 6. KHÓA CỨNG TOÀN BỘ ĐIỂM SỐ CỦA HỌC KỲ
            foreach (var score in scores)
            {
                score.IsLocked = true;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return ServiceResult<bool>.Ok(true, $"Đã phê duyệt và chốt sổ học kỳ \"{semester.SemesterName}\" thành công. Toàn bộ điểm số đã được khóa an toàn.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Lỗi khi chốt sổ học kỳ {SemesterId}", semesterId);
            return ServiceResult<bool>.Fail(ErrorCodes.SystemError, $"Đã xảy ra lỗi khi chốt sổ học kỳ: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> UnlockSemesterAsync(int semesterId, int principalUserId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var semester = await _context.Semesters.FirstOrDefaultAsync(s => s.Id == semesterId);
            if (semester == null)
            {
                return ServiceResult<bool>.Fail(ErrorCodes.SemesterNotFound, "Học kỳ không tồn tại.");
            }

            // Mở khóa các điểm số
            var scores = await _context.StudentScores
                .Include(ss => ss.Assessment).ThenInclude(a => a.TeachingAssignment)
                .Where(ss => ss.Assessment.TeachingAssignment.SemesterId == semesterId)
                .ToListAsync();

            foreach (var s in scores)
            {
                s.IsLocked = false;
            }

            // Xóa snapshot semester_reports để mở trạng thái chốt sổ
            var reports = await _context.SemesterReports
                .Where(sr => sr.SemesterId == semesterId)
                .ToListAsync();

            _context.SemesterReports.RemoveRange(reports);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return ServiceResult<bool>.Ok(true, $"Đã mở khóa học kỳ \"{semester.SemesterName}\". Giáo viên và giáo vụ hiện có thể cập nhật điểm bổ sung.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Lỗi khi mở khóa học kỳ {SemesterId}", semesterId);
            return ServiceResult<bool>.Fail(ErrorCodes.SystemError, $"Đã xảy ra lỗi khi mở khóa học kỳ: {ex.Message}");
        }
    }

    public async Task<ServiceResult<AcademicReportDto>> GetAcademicReportDataAsync(int semesterId, int? gradeLevelId = null, int? subjectId = null)
    {
        try
        {
            Semester? semester = null;
            if (semesterId <= 0)
            {
                semester = await _context.Semesters
                    .Include(s => s.AcademicYear)
                    .FirstOrDefaultAsync(s => s.IsActive);
                if (semester == null)
                {
                    semester = await _context.Semesters.Include(s => s.AcademicYear).OrderByDescending(s => s.StartDate).FirstOrDefaultAsync();
                }
                semesterId = semester?.Id ?? 0;
            }
            else
            {
                semester = await _context.Semesters
                    .Include(s => s.AcademicYear)
                    .FirstOrDefaultAsync(s => s.Id == semesterId);
            }

            if (semester == null)
            {
                return ServiceResult<AcademicReportDto>.Fail(ErrorCodes.SemesterNotFound, "Không tìm thấy học kỳ yêu cầu.");
            }

            var dto = new AcademicReportDto
            {
                SemesterId = semesterId,
                SemesterName = semester.SemesterName,
                AcademicYearCode = semester.AcademicYear?.YearCode ?? string.Empty
            };

            var gradeLevels = await _context.GradeLevels.OrderBy(g => g.Id).ToListAsync();
            if (gradeLevelId.HasValue)
            {
                gradeLevels = gradeLevels.Where(g => g.Id == gradeLevelId.Value).ToList();
            }

            var reports = await _context.SemesterReports
                .Include(sr => sr.Student).ThenInclude(s => s.User)
                .Include(sr => sr.Student).ThenInclude(s => s.StudentClasses).ThenInclude(sc => sc.Class).ThenInclude(c => c.GradeLevel)
                .Where(sr => sr.SemesterId == semesterId)
                .ToListAsync();

            foreach (var grade in gradeLevels)
            {
                var gradeReports = reports.Where(r => r.Student.StudentClasses
                    .Any(sc => sc.Status != "Transferred" && sc.Class.AcademicYearId == semester.AcademicYearId && sc.Class.GradeLevelId == grade.Id)).ToList();

                int total = gradeReports.Count;
                decimal? avgGpa = total > 0 ? Math.Round(gradeReports.Where(r => r.Gpa.HasValue).Average(r => r.Gpa!.Value), 2) : null;
                int exc = gradeReports.Count(r => r.AcademicRank == "Giỏi");
                int good = gradeReports.Count(r => r.AcademicRank == "Khá");
                int pass = gradeReports.Count(r => r.AcademicRank == "Đạt");
                int fail = gradeReports.Count(r => r.AcademicRank == "Chưa đạt");

                dto.GradeSummaries.Add(new GradeAcademicSummaryDto
                {
                    GradeName = grade.LevelName,
                    TotalStudents = total,
                    AverageGpa = avgGpa,
                    ExcellentCount = exc,
                    GoodCount = good,
                    PassCount = pass,
                    FailCount = fail
                });
            }

            var subjectQuery = _context.Subjects.Where(s => s.IsActive);
            if (subjectId.HasValue)
            {
                subjectQuery = subjectQuery.Where(s => s.Id == subjectId.Value);
            }
            var subjects = await subjectQuery.ToListAsync();

            var subjectResults = await _context.SemesterSubjectResults
                .Where(ssr => ssr.SemesterId == semesterId && ssr.AverageScore.HasValue)
                .ToListAsync();

            foreach (var sub in subjects)
            {
                var subResults = subjectResults.Where(sr => sr.SubjectId == sub.Id).ToList();
                int totalAssessments = await _context.Assessments
                    .CountAsync(a => a.TeachingAssignment.SubjectId == sub.Id && a.TeachingAssignment.SemesterId == semesterId);

                int totalStudents = subResults.Count;
                decimal? avgScore = totalStudents > 0 ? Math.Round(subResults.Average(sr => sr.AverageScore!.Value), 2) : null;
                int passCount = subResults.Count(sr => sr.AverageScore >= 5.0m);
                int excCount = subResults.Count(sr => sr.AverageScore >= 8.0m);

                dto.SubjectSummaries.Add(new SubjectPerformanceSummaryDto
                {
                    SubjectId = sub.Id,
                    SubjectName = sub.SubjectName,
                    SubjectCode = sub.SubjectCode,
                    TotalAssessments = totalAssessments,
                    AverageScore = avgScore,
                    PassRate = totalStudents > 0 ? Math.Round((decimal)passCount / totalStudents * 100m, 1) : 0m,
                    ExcellentRate = totalStudents > 0 ? Math.Round((decimal)excCount / totalStudents * 100m, 1) : 0m
                });
            }

            var topReports = reports.Where(r => r.Gpa.HasValue)
                .OrderByDescending(r => r.Gpa)
                .Take(20)
                .ToList();

            int rank = 1;
            foreach (var r in topReports)
            {
                var activeClass = r.Student.StudentClasses.FirstOrDefault(sc => sc.Status != "Transferred" && sc.Class.AcademicYearId == semester.AcademicYearId)?.Class?.ClassName ?? "N/A";
                dto.TopStudents.Add(new StudentRankingItemDto
                {
                    Rank = rank++,
                    StudentId = r.StudentId,
                    StudentCode = r.Student.StudentCode,
                    FullName = r.Student.User?.FullName ?? "N/A",
                    ClassName = activeClass,
                    Gpa = r.Gpa,
                    AcademicRank = r.AcademicRank ?? "N/A",
                    ConductGrade = r.ConductGrade ?? "N/A"
                });
            }

            return ServiceResult<AcademicReportDto>.Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tạo báo cáo học tập cho học kỳ {SemesterId}", semesterId);
            return ServiceResult<AcademicReportDto>.Fail(ErrorCodes.SystemError, "Không thể tải báo cáo học tập.");
        }
    }

    public async Task<ServiceResult<AttendanceReportDto>> GetAttendanceReportDataAsync(int? semesterId = null, int? gradeLevelId = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        try
        {
            DateTime to = toDate?.Date ?? DateTime.Today;
            DateTime from = fromDate?.Date ?? to.AddDays(-30);

            var semester = semesterId.HasValue
                ? await _context.Semesters.FirstOrDefaultAsync(s => s.Id == semesterId.Value)
                : await _context.Semesters.FirstOrDefaultAsync(s => s.IsActive);

            var dto = new AttendanceReportDto
            {
                SemesterId = semester?.Id,
                SemesterName = semester?.SemesterName ?? "Tất cả học kỳ",
                FromDate = from,
                ToDate = to
            };

            var recordsQuery = _context.AttendanceRecords
                .Include(a => a.Student).ThenInclude(s => s.StudentClasses).ThenInclude(sc => sc.Class).ThenInclude(c => c.GradeLevel)
                .Where(a => a.AttendanceDate.Date >= from.Date && a.AttendanceDate.Date <= to.Date);

            var records = await recordsQuery.ToListAsync();

            dto.TotalRecords = records.Count;
            dto.TotalPresent = records.Count(r => r.Status == "Present");
            dto.TotalExcused = records.Count(r => r.Status == "Excused");
            dto.TotalUnexcused = records.Count(r => r.Status == "Absent");
            dto.OverallAttendanceRate = dto.TotalRecords > 0
                ? Math.Round((decimal)dto.TotalPresent / dto.TotalRecords * 100m, 1)
                : 100m;

            var classesQuery = _context.Classes.Include(c => c.GradeLevel).AsQueryable();
            if (gradeLevelId.HasValue)
            {
                classesQuery = classesQuery.Where(c => c.GradeLevelId == gradeLevelId.Value);
            }
            var classes = await classesQuery.OrderBy(c => c.GradeLevelId).ThenBy(c => c.ClassName).ToListAsync();

            foreach (var cls in classes)
            {
                var classRecords = records.Where(r => r.Student.StudentClasses
                    .Any(sc => sc.Status == "Enrolled" && sc.ClassId == cls.Id)).ToList();

                int totalClassRecords = classRecords.Count;
                int present = classRecords.Count(r => r.Status == "Present");
                int absents = classRecords.Count(r => r.Status == "Absent" || r.Status == "Excused");
                int late = classRecords.Count(r => r.Status == "Late");
                decimal rate = totalClassRecords > 0 ? Math.Round((decimal)present / totalClassRecords * 100m, 1) : 100m;

                dto.ClassStats.Add(new ClassAttendanceStatDto
                {
                    ClassId = cls.Id,
                    ClassName = cls.ClassName,
                    GradeName = cls.GradeLevel?.LevelName ?? string.Empty,
                    TotalStudents = await _context.StudentClasses.CountAsync(sc => sc.ClassId == cls.Id && sc.Status == "Enrolled" && sc.LeftAt == null),
                    TotalAbsents = absents,
                    TotalLate = late,
                    AttendanceRate = rate
                });
            }

            return ServiceResult<AttendanceReportDto>.Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tạo báo cáo chuyên cần.");
            return ServiceResult<AttendanceReportDto>.Fail(ErrorCodes.SystemError, "Không thể tải báo cáo chuyên cần.");
        }
    }

    public async Task<ServiceResult<byte[]>> ExportAcademicReportCsvAsync(int semesterId, int? gradeLevelId = null)
    {
        try
        {
            var reportResult = await GetAcademicReportDataAsync(semesterId, gradeLevelId);
            if (!reportResult.Success || reportResult.Data == null)
            {
                return ServiceResult<byte[]>.Fail(reportResult.ErrorCode ?? ErrorCodes.SystemError, reportResult.Message ?? "Không thể lấy dữ liệu báo cáo.");
            }

            var report = reportResult.Data;
            var sb = new StringBuilder();

            sb.AppendLine($"\"BÁO CÁO TỔNG KẾT HỌC TẬP TOÀN TRƯỜNG - {report.SemesterName} ({report.AcademicYearCode})\"");
            sb.AppendLine($"\"Ngày xuất:\",\"{DateTime.Now:dd/MM/yyyy HH:mm}\"");
            sb.AppendLine();

            sb.AppendLine("\"1. THỐNG KÊ XẾP LOẠI THEO KHỐI LỚP\"");
            sb.AppendLine("\"Khối Lớp\",\"Tổng số HS\",\"ĐTB GPA\",\"Giỏi\",\"Khá\",\"Đạt\",\"Chưa Đạt\"");
            foreach (var g in report.GradeSummaries)
            {
                sb.AppendLine($"\"{g.GradeName}\",{g.TotalStudents},{g.AverageGpa?.ToString("0.00") ?? "N/A"},{g.ExcellentCount},{g.GoodCount},{g.PassCount},{g.FailCount}");
            }
            sb.AppendLine();

            sb.AppendLine("\"2. THỐNG KÊ HIỆU SUẤT THEO MÔN HỌC\"");
            sb.AppendLine("\"Mã môn\",\"Tên môn\",\"Số bài kiểm tra\",\"Điểm TB\",\"Tỷ lệ đạt (%)\",\"Tỷ lệ giỏi (%)\"");
            foreach (var s in report.SubjectSummaries)
            {
                sb.AppendLine($"\"{s.SubjectCode}\",\"{s.SubjectName}\",{s.TotalAssessments},{s.AverageScore?.ToString("0.00") ?? "N/A"},{s.PassRate:0.0}%,{s.ExcellentRate:0.0}%");
            }
            sb.AppendLine();

            sb.AppendLine("\"3. TOP HỌC SINH XUẤT SẮC TOÀN TRƯỜNG\"");
            sb.AppendLine("\"Hạng\",\"Mã HS\",\"Họ và tên\",\"Lớp\",\"Điểm GPA\",\"Học lực\",\"Hạnh kiểm\"");
            foreach (var st in report.TopStudents)
            {
                sb.AppendLine($"{st.Rank},\"{st.StudentCode}\",\"{st.FullName}\",\"{st.ClassName}\",{st.Gpa?.ToString("0.00") ?? "N/A"},\"{st.AcademicRank}\",\"{st.ConductGrade}\"");
            }

            var preamble = Encoding.UTF8.GetPreamble();
            var contentBytes = Encoding.UTF8.GetBytes(sb.ToString());
            var resultBytes = new byte[preamble.Length + contentBytes.Length];
            Buffer.BlockCopy(preamble, 0, resultBytes, 0, preamble.Length);
            Buffer.BlockCopy(contentBytes, 0, resultBytes, preamble.Length, contentBytes.Length);

            return ServiceResult<byte[]>.Ok(resultBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xuất file CSV báo cáo học tập.");
            return ServiceResult<byte[]>.Fail(ErrorCodes.SystemError, "Không thể xuất file báo cáo.");
        }
    }

    public async Task<ServiceResult<byte[]>> ExportAttendanceReportCsvAsync(int? semesterId = null, int? gradeLevelId = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        try
        {
            var reportResult = await GetAttendanceReportDataAsync(semesterId, gradeLevelId, fromDate, toDate);
            if (!reportResult.Success || reportResult.Data == null)
            {
                return ServiceResult<byte[]>.Fail(reportResult.ErrorCode ?? ErrorCodes.SystemError, reportResult.Message ?? "Không thể lấy dữ liệu báo cáo chuyên cần.");
            }

            var report = reportResult.Data;
            var sb = new StringBuilder();

            sb.AppendLine($"\"BÁO CÁO THỐNG KÊ CHUYÊN CẦN TOÀN TRƯỜNG - {report.SemesterName}\"");
            sb.AppendLine($"\"Thời gian:\",\"{report.FromDate:dd/MM/yyyy} đến {report.ToDate:dd/MM/yyyy}\"");
            sb.AppendLine($"\"Tổng lượt ghi nhận:\",{report.TotalRecords}");
            sb.AppendLine($"\"Tổng lượt có mặt:\",{report.TotalPresent}");
            sb.AppendLine($"\"Có phép:\",{report.TotalExcused},\"Không phép:\",{report.TotalUnexcused}");
            sb.AppendLine($"\"Tỷ lệ chuyên cần chung:\",{report.OverallAttendanceRate:0.0}%");
            sb.AppendLine();

            sb.AppendLine("\"CHI TIẾT CHUYÊN CẦN THEO TỪNG LỚP HỌC\"");
            sb.AppendLine("\"Khối Lớp\",\"Tên Lớp\",\"Sĩ số\",\"Tổng vắng\",\"Đi muộn\",\"Tỷ lệ chuyên cần (%)\"");
            foreach (var c in report.ClassStats)
            {
                sb.AppendLine($"\"{c.GradeName}\",\"{c.ClassName}\",{c.TotalStudents},{c.TotalAbsents},{c.TotalLate},{c.AttendanceRate:0.0}%");
            }

            var preamble = Encoding.UTF8.GetPreamble();
            var contentBytes = Encoding.UTF8.GetBytes(sb.ToString());
            var resultBytes = new byte[preamble.Length + contentBytes.Length];
            Buffer.BlockCopy(preamble, 0, resultBytes, 0, preamble.Length);
            Buffer.BlockCopy(contentBytes, 0, resultBytes, preamble.Length, contentBytes.Length);

            return ServiceResult<byte[]>.Ok(resultBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xuất file CSV báo cáo chuyên cần.");
            return ServiceResult<byte[]>.Fail(ErrorCodes.SystemError, "Không thể xuất file báo cáo chuyên cần.");
        }
    }

    public async Task<ServiceResult<YearEndPromotionPageDto>> GetYearEndPromotionDataAsync(
        int? sourceAcademicYearId = null,
        int? sourceClassId = null,
        int? targetAcademicYearId = null)
    {
        try
        {
            var allYears = await _context.AcademicYears
                .Include(y => y.Semesters)
                .OrderByDescending(y => y.StartDate)
                .ToListAsync();

            if (!allYears.Any())
            {
                return ServiceResult<YearEndPromotionPageDto>.Fail(
                    ErrorCodes.ResourceNotFound,
                    "Chưa có dữ liệu năm học trong hệ thống.");
            }

            var sourceYear = sourceAcademicYearId.HasValue
                ? allYears.FirstOrDefault(y => y.Id == sourceAcademicYearId.Value) ?? (allYears.FirstOrDefault(y => y.IsCurrent) ?? allYears.First())
                : (allYears.FirstOrDefault(y => y.IsCurrent) ?? allYears.First());

            var targetYear = targetAcademicYearId.HasValue
                ? allYears.FirstOrDefault(y => y.Id == targetAcademicYearId.Value && y.Id != sourceYear.Id)
                : allYears.Where(y => y.StartDate > sourceYear.StartDate).OrderBy(y => y.StartDate).FirstOrDefault();

            var sourceSemesters = sourceYear.Semesters.OrderBy(s => s.StartDate).ThenBy(s => s.Id).ToList();
            var semIds = sourceSemesters.Select(s => s.Id).ToList();
            var finalizedSemIds = await _context.SemesterReports
                .Where(sr => semIds.Contains(sr.SemesterId))
                .Select(sr => sr.SemesterId)
                .Distinct()
                .ToListAsync();

            var dto = new YearEndPromotionPageDto
            {
                SourceAcademicYearId = sourceYear.Id,
                SourceAcademicYearCode = sourceYear.YearCode,
                TargetAcademicYearId = targetYear?.Id,
                TargetAcademicYearCode = targetYear?.YearCode,
                AllSemestersFinalized = sourceSemesters.Count > 0 && sourceSemesters.All(s => finalizedSemIds.Contains(s.Id)),
                SourceSemesters = sourceSemesters.Select(s => new SemesterFinalizationStatusDto
                {
                    SemesterId = s.Id,
                    SemesterName = s.SemesterName,
                    IsFinalized = finalizedSemIds.Contains(s.Id)
                }).ToList()
            };

            var sourceClasses = await _context.Classes
                .Include(c => c.GradeLevel)
                .Where(c => c.AcademicYearId == sourceYear.Id)
                .OrderBy(c => c.GradeLevel.LevelCode)
                .ThenBy(c => c.ClassName)
                .ToListAsync();

            var selectedClass = sourceClassId.HasValue
                ? sourceClasses.FirstOrDefault(c => c.Id == sourceClassId.Value) ?? sourceClasses.FirstOrDefault()
                : sourceClasses.FirstOrDefault();

            if (selectedClass == null)
            {
                return ServiceResult<YearEndPromotionPageDto>.Ok(dto);
            }

            dto.SourceClassId = selectedClass.Id;
            dto.SourceClassName = selectedClass.ClassName;
            dto.SourceGradeLevelId = selectedClass.GradeLevelId;
            dto.SourceGradeLevelName = selectedClass.GradeLevel?.LevelName ?? string.Empty;

            var allGrades = await _context.GradeLevels
                .OrderBy(g => g.LevelCode)
                .ThenBy(g => g.Id)
                .ToListAsync();

            int gradeIdx = allGrades.FindIndex(g => g.Id == selectedClass.GradeLevelId);
            bool isGrade12ByName = (selectedClass.GradeLevel?.LevelCode.Contains("12") ?? false)
                                || (selectedClass.GradeLevel?.LevelName.Contains("12") ?? false);
            dto.IsFinalGrade = isGrade12ByName || (gradeIdx >= 0 && gradeIdx == allGrades.Count - 1);

            if (!dto.IsFinalGrade && gradeIdx >= 0 && gradeIdx + 1 < allGrades.Count)
            {
                dto.NextGradeLevelId = allGrades[gradeIdx + 1].Id;
                dto.NextGradeLevelName = allGrades[gradeIdx + 1].LevelName;
            }

            var memberships = await _context.StudentClasses
                .Include(sc => sc.Student).ThenInclude(s => s.User)
                .Include(sc => sc.Student).ThenInclude(s => s.StudentClasses).ThenInclude(otherSc => otherSc.Class).ThenInclude(c => c.AcademicYear)
                .Where(sc => sc.ClassId == selectedClass.Id && sc.Status != "Transferred")
                .OrderBy(sc => sc.Student.StudentCode)
                .ToListAsync();

            var studentIds = memberships.Select(m => m.StudentId).ToList();
            var reports = await _context.SemesterReports
                .Where(sr => studentIds.Contains(sr.StudentId) && semIds.Contains(sr.SemesterId))
                .ToListAsync();

            foreach (var sc in memberships)
            {
                var stReports = reports.Where(r => r.StudentId == sc.StudentId).ToList();
                decimal? sem1Gpa = sourceSemesters.Count >= 1
                    ? stReports.FirstOrDefault(r => r.SemesterId == sourceSemesters[0].Id)?.Gpa
                    : null;
                decimal? sem2Gpa = sourceSemesters.Count >= 2
                    ? stReports.FirstOrDefault(r => r.SemesterId == sourceSemesters[1].Id)?.Gpa
                    : null;

                decimal? yearGpa = null;
                if (sem1Gpa.HasValue && sem2Gpa.HasValue)
                {
                    yearGpa = Math.Round((sem1Gpa.Value + sem2Gpa.Value * 2m) / 3m, 2);
                }
                else if (sem2Gpa.HasValue || sem1Gpa.HasValue)
                {
                    yearGpa = sem2Gpa ?? sem1Gpa;
                }

                var lastSemId = sourceSemesters.LastOrDefault()?.Id ?? 0;
                var lastReport = stReports.FirstOrDefault(r => r.SemesterId == lastSemId) ?? stReports.LastOrDefault();
                string yearConduct = lastReport?.ConductGrade ?? "Tốt";

                string yearRank = "Chưa tổng kết";
                if (yearGpa.HasValue)
                {
                    if (yearGpa.Value >= 8.0m) yearRank = "Giỏi";
                    else if (yearGpa.Value >= 6.5m) yearRank = "Khá";
                    else if (yearGpa.Value >= 5.0m) yearRank = "Đạt";
                    else yearRank = "Chưa đạt";
                }

                bool conductOk = !string.Equals(yearConduct, "Yếu", StringComparison.OrdinalIgnoreCase)
                              && !string.Equals(yearConduct, "Chưa đạt", StringComparison.OrdinalIgnoreCase);
                bool isEligible = yearGpa.HasValue && yearGpa.Value >= 5.0m && conductOk;
                string suggestedOutcome = isEligible
                    ? (dto.IsFinalGrade ? "Graduate" : "Promote")
                    : "Repeat";

                var targetMembership = targetYear != null
                    ? sc.Student.StudentClasses.FirstOrDefault(other => other.Class.AcademicYearId == targetYear.Id && other.Status != "Transferred")
                    : sc.Student.StudentClasses.FirstOrDefault(other => other.Class.AcademicYear.StartDate > sourceYear.StartDate && other.Status != "Transferred");

                bool isAlreadyProcessed = sc.Status is "Promoted" or "Repeated" or "Graduated"
                                       || sc.Student.Status == "Graduated"
                                       || targetMembership != null;

                string? targetAssignedClass = targetMembership != null
                    ? $"{targetMembership.Class.ClassName} ({targetMembership.Class.AcademicYear.YearCode})"
                    : (sc.Status == "Graduated" || sc.Student.Status == "Graduated" ? "Đã tốt nghiệp THPT" : null);

                dto.TotalStudents++;
                if (isAlreadyProcessed)
                {
                    dto.AlreadyProcessedCount++;
                }
                else if (isEligible)
                {
                    dto.SuggestedPromoteOrGraduateCount++;
                }
                else
                {
                    dto.SuggestedRepeatCount++;
                }

                dto.Students.Add(new StudentYearEndPromotionItemDto
                {
                    StudentId = sc.StudentId,
                    SourceMembershipId = sc.Id,
                    StudentCode = sc.Student.StudentCode,
                    FullName = sc.Student.User?.FullName ?? string.Empty,
                    Semester1Gpa = sem1Gpa,
                    Semester2Gpa = sem2Gpa,
                    YearGpa = yearGpa,
                    YearAcademicRank = yearRank,
                    YearConductGrade = yearConduct,
                    IsEligible = isEligible,
                    SuggestedOutcome = suggestedOutcome,
                    IsAlreadyProcessed = isAlreadyProcessed,
                    CurrentSourceStatus = sc.Status,
                    TargetAssignedClassName = targetAssignedClass
                });
            }

            return ServiceResult<YearEndPromotionPageDto>.Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tải dữ liệu xét lên lớp / tốt nghiệp.");
            return ServiceResult<YearEndPromotionPageDto>.Fail(
                ErrorCodes.SystemError,
                "Không thể tải dữ liệu xét lên lớp / tốt nghiệp.");
        }
    }

    public async Task<ServiceResult<int>> ExecuteYearEndPromotionAsync(ExecuteYearEndPromotionDto dto, int principalUserId)
    {
        if (dto == null || dto.SourceAcademicYearId <= 0 || dto.SourceClassId <= 0 || dto.Decisions == null || !dto.Decisions.Any())
        {
            return ServiceResult<int>.Fail(
                ErrorCodes.ValidationFailed,
                "Dữ liệu xét duyệt không hợp lệ. Vui lòng chọn lớp nguồn và danh sách học sinh.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var sourceYear = await _context.AcademicYears
                .Include(y => y.Semesters)
                .FirstOrDefaultAsync(y => y.Id == dto.SourceAcademicYearId);
            if (sourceYear == null)
            {
                return ServiceResult<int>.Fail(ErrorCodes.ResourceNotFound, "Không tìm thấy năm học nguồn.");
            }

            var sourceClass = await _context.Classes
                .Include(c => c.GradeLevel)
                .FirstOrDefaultAsync(c => c.Id == dto.SourceClassId && c.AcademicYearId == sourceYear.Id);
            if (sourceClass == null)
            {
                return ServiceResult<int>.Fail(ErrorCodes.ClassNotFound, "Không tìm thấy lớp nguồn thuộc năm học đã chọn.");
            }

            // Ràng buộc bắt buộc: Tất cả học kỳ của năm học nguồn phải được chốt sổ điểm
            var sourceSemesters = sourceYear.Semesters.ToList();
            if (!sourceSemesters.Any())
            {
                return ServiceResult<int>.Fail(
                    ErrorCodes.ValidationFailed,
                    "Năm học nguồn chưa có học kỳ nào được cấu hình.");
            }

            var semIds = sourceSemesters.Select(s => s.Id).ToList();
            var finalizedSemIds = await _context.SemesterReports
                .Where(sr => semIds.Contains(sr.SemesterId))
                .Select(sr => sr.SemesterId)
                .Distinct()
                .ToListAsync();

            if (!sourceSemesters.All(s => finalizedSemIds.Contains(s.Id)))
            {
                return ServiceResult<int>.Fail(
                    ErrorCodes.ValidationFailed,
                    "Tất cả các học kỳ của năm học nguồn phải được chốt sổ điểm trước khi xét lên lớp / tốt nghiệp.");
            }

            var allGrades = await _context.GradeLevels
                .OrderBy(g => g.LevelCode)
                .ThenBy(g => g.Id)
                .ToListAsync();

            int gradeIdx = allGrades.FindIndex(g => g.Id == sourceClass.GradeLevelId);
            bool isGrade12ByName = (sourceClass.GradeLevel?.LevelCode.Contains("12") ?? false)
                                || (sourceClass.GradeLevel?.LevelName.Contains("12") ?? false);
            bool isFinalGrade = isGrade12ByName || (gradeIdx >= 0 && gradeIdx == allGrades.Count - 1);
            int? nextGradeId = (!isFinalGrade && gradeIdx >= 0 && gradeIdx + 1 < allGrades.Count)
                ? allGrades[gradeIdx + 1].Id
                : null;

            var memberships = await _context.StudentClasses
                .Include(sc => sc.Student)
                .Where(sc => sc.ClassId == sourceClass.Id && sc.Status == "Enrolled")
                .ToListAsync();

            var validDecisions = new List<(StudentClass Membership, string Outcome)>();
            foreach (var dec in dto.Decisions)
            {
                var m = memberships.FirstOrDefault(x => x.Id == dec.SourceMembershipId && x.StudentId == dec.StudentId);
                if (m == null) continue;

                string outcome = dec.Outcome?.Trim() ?? string.Empty;
                if (isFinalGrade)
                {
                    if (outcome is not ("Graduate" or "Repeat"))
                    {
                        return ServiceResult<int>.Fail(ErrorCodes.ValidationFailed, "Quyết định cho học sinh khối 12 chỉ có thể là Tốt nghiệp hoặc Ở lại lớp.");
                    }
                }
                else
                {
                    if (outcome is not ("Promote" or "Repeat"))
                    {
                        return ServiceResult<int>.Fail(ErrorCodes.ValidationFailed, "Quyết định cho học sinh khối 10/11 chỉ có thể là Lên lớp hoặc Ở lại lớp.");
                    }
                }

                validDecisions.Add((m, outcome));
            }

            if (!validDecisions.Any())
            {
                return ServiceResult<int>.Fail(
                    ErrorCodes.ValidationFailed,
                    "Không có học sinh nào ở trạng thái đang học cần xử lý trong lớp này.");
            }

            bool hasPromote = validDecisions.Any(d => d.Outcome == "Promote");
            bool hasRepeat = validDecisions.Any(d => d.Outcome == "Repeat");

            if (hasPromote || hasRepeat)
            {
                if (!dto.TargetAcademicYearId.HasValue || dto.TargetAcademicYearId.Value == sourceYear.Id)
                {
                    return ServiceResult<int>.Fail(
                        ErrorCodes.ValidationFailed,
                        "Vui lòng chọn Năm học đích (năm học mới) hợp lệ để xếp lớp cho học sinh.");
                }

                var targetYear = await _context.AcademicYears.FirstOrDefaultAsync(y => y.Id == dto.TargetAcademicYearId.Value);
                if (targetYear == null || targetYear.StartDate <= sourceYear.StartDate)
                {
                    return ServiceResult<int>.Fail(
                        ErrorCodes.ValidationFailed,
                        "Năm học đích phải là năm học diễn ra sau năm học nguồn.");
                }
            }

            if (hasPromote)
            {
                if (!dto.PromoteTargetClassId.HasValue || !nextGradeId.HasValue)
                {
                    return ServiceResult<int>.Fail(
                        ErrorCodes.ValidationFailed,
                        "Vui lòng chọn Lớp lên lớp thuộc khối kế tiếp ở năm học mới.");
                }

                var promoteClass = await _context.Classes.FirstOrDefaultAsync(c =>
                    c.Id == dto.PromoteTargetClassId.Value &&
                    c.AcademicYearId == dto.TargetAcademicYearId!.Value &&
                    c.GradeLevelId == nextGradeId.Value);

                if (promoteClass == null)
                {
                    return ServiceResult<int>.Fail(
                        ErrorCodes.ValidationFailed,
                        "Lớp lên lớp được chọn không thuộc khối kế tiếp trong năm học đích.");
                }
            }

            if (hasRepeat)
            {
                if (!dto.RepeatTargetClassId.HasValue)
                {
                    return ServiceResult<int>.Fail(
                        ErrorCodes.ValidationFailed,
                        "Trong danh sách có học sinh Lưu ban (Ở lại lớp). Vui lòng chọn Lớp học lại ngay trên màn hình.");
                }

                var repeatClass = await _context.Classes.FirstOrDefaultAsync(c =>
                    c.Id == dto.RepeatTargetClassId.Value &&
                    c.AcademicYearId == dto.TargetAcademicYearId!.Value &&
                    c.GradeLevelId == sourceClass.GradeLevelId);

                if (repeatClass == null)
                {
                    return ServiceResult<int>.Fail(
                        ErrorCodes.ValidationFailed,
                        "Lớp học lại (lưu ban) được chọn phải cùng khối với lớp nguồn trong năm học đích.");
                }
            }

            var now = DateTime.UtcNow;
            int promotedCount = 0, repeatedCount = 0, graduatedCount = 0;

            foreach (var (membership, outcome) in validDecisions)
            {
                if (outcome == "Promote")
                {
                    bool alreadyInTargetYear = await _context.StudentClasses
                        .AnyAsync(sc => sc.StudentId == membership.StudentId && sc.Class.AcademicYearId == dto.TargetAcademicYearId!.Value);
                    if (!alreadyInTargetYear)
                    {
                        _context.StudentClasses.Add(new StudentClass
                        {
                            StudentId = membership.StudentId,
                            ClassId = dto.PromoteTargetClassId!.Value,
                            Status = "Enrolled",
                            JoinedAt = now
                        });
                    }

                    membership.Status = "Promoted";
                    membership.LeftAt = now;
                    promotedCount++;
                }
                else if (outcome == "Repeat")
                {
                    bool alreadyInTargetYear = await _context.StudentClasses
                        .AnyAsync(sc => sc.StudentId == membership.StudentId && sc.Class.AcademicYearId == dto.TargetAcademicYearId!.Value);
                    if (!alreadyInTargetYear)
                    {
                        _context.StudentClasses.Add(new StudentClass
                        {
                            StudentId = membership.StudentId,
                            ClassId = dto.RepeatTargetClassId!.Value,
                            Status = "Enrolled",
                            JoinedAt = now
                        });
                    }

                    membership.Status = "Repeated";
                    membership.LeftAt = now;
                    repeatedCount++;
                }
                else if (outcome == "Graduate")
                {
                    membership.Status = "Graduated";
                    membership.LeftAt = now;
                    membership.Student.Status = "Graduated";
                    graduatedCount++;
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            int totalProcessed = promotedCount + repeatedCount + graduatedCount;
            string summaryMsg = isFinalGrade
                ? $"Đã xét duyệt hoàn tất lớp {sourceClass.ClassName}: {graduatedCount} học sinh Tốt nghiệp, {repeatedCount} học sinh Ở lại lớp."
                : $"Đã xét duyệt hoàn tất lớp {sourceClass.ClassName}: {promotedCount} học sinh Lên lớp, {repeatedCount} học sinh Ở lại lớp.";

            return ServiceResult<int>.Ok(totalProcessed, summaryMsg);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Lỗi khi thực thi xét lên lớp / tốt nghiệp cho lớp {ClassId}", dto.SourceClassId);
            return ServiceResult<int>.Fail(
                ErrorCodes.SystemError,
                $"Đã xảy ra lỗi khi xét lên lớp / tốt nghiệp: {ex.Message}");
        }
    }
}
