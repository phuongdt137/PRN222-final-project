using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.MVC.Models.ViewModels.Score;
using HighSchoolManagementSystem.Repositories.Interfaces;
using HighSchoolManagementSystem.Services.Common;
using HighSchoolManagementSystem.Services.DTOs;
using HighSchoolManagementSystem.Services.Interfaces;

namespace HighSchoolManagementSystem.MVC.Controllers;

[Authorize(Roles = "Teacher,Staff")]
public class ScoreController : Controller
{
    private readonly IScoreService _scoreService;
    private readonly IAssessmentService _assessmentService;
    private readonly ITeacherFeedbackService _feedbackService;
    private readonly ITeacherRepository _teacherRepository;
    private readonly IClassRepository _classRepository;
    private readonly IAcademicYearRepository _academicYearRepository;
    private readonly ISemesterRepository _semesterRepository;
    private readonly ILogger<ScoreController> _logger;

    public ScoreController(
        IScoreService scoreService,
        IAssessmentService assessmentService,
        ITeacherFeedbackService feedbackService,
        ITeacherRepository teacherRepository,
        IClassRepository classRepository,
        IAcademicYearRepository academicYearRepository,
        ISemesterRepository semesterRepository,
        ILogger<ScoreController> logger)
    {
        _scoreService = scoreService;
        _assessmentService = assessmentService;
        _feedbackService = feedbackService;
        _teacherRepository = teacherRepository;
        _classRepository = classRepository;
        _academicYearRepository = academicYearRepository;
        _semesterRepository = semesterRepository;
        _logger = logger;
    }

    // =========================================================================
    // 1. NHẬP ĐIỂM (STUDENT & BULK SCORE ENTRY)
    // =========================================================================

    [HttpGet]
    [Authorize(Roles = "Teacher,Staff")]
    public async Task<IActionResult> Entry(int assessmentId)
    {
        bool isStaff = User.IsInRole("Staff");
        int? teacherId = null;

        if (!isStaff)
        {
            var teacher = await ResolveCurrentTeacherAsync();
            if (teacher == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy hồ sơ giáo viên của tài khoản này.";
                return RedirectToAction("Dashboard", "Teacher");
            }
            teacherId = teacher.Id;
        }

        var assessmentResult = await _assessmentService.GetAssessmentByIdAsync(assessmentId, teacherId);
        if (!assessmentResult.Success || assessmentResult.Data == null)
        {
            TempData["ErrorMessage"] = assessmentResult.Message ?? "Không tìm thấy thông tin bài kiểm tra.";
            if (isStaff)
            {
                return RedirectToAction(nameof(HomeroomScores));
            }
            return RedirectToAction("Index", "Assessment");
        }

        var assessment = assessmentResult.Data;

        var scoresResult = await _scoreService.GetScoresForAssessmentAsync(assessmentId, teacherId);
        if (!scoresResult.Success)
        {
            TempData["ErrorMessage"] = scoresResult.Message ?? "Không thể lấy danh sách học sinh để nhập điểm.";
            if (isStaff)
            {
                return RedirectToAction(nameof(HomeroomScores));
            }
            return RedirectToAction("Index", "Assessment", new { assignmentId = assessment.TeachingAssignmentId });
        }

        var studentScores = scoresResult.Data?.ToList() ?? new();

        // Giáo viên chỉ được sửa khi chưa có điểm bị khóa (chưa chốt nộp). Giáo vụ luôn có quyền điều chỉnh.
        bool isLocked = assessment.HasLockedScores || studentScores.Any(s => s.IsLocked);
        bool canEdit = isStaff || !isLocked;

        var model = new ScoreEntryViewModel
        {
            AssessmentId = assessment.Id,
            AssessmentName = assessment.AssessmentName,
            AssessmentTypeName = assessment.AssessmentTypeName,
            Weight = assessment.Weight,
            MaxScore = 10,
            SubjectName = assessment.SubjectName,
            ClassName = assessment.ClassName,
            SemesterName = assessment.SemesterName,
            AssignmentId = assessment.TeachingAssignmentId,
            IsAssessmentLocked = isLocked,
            IsStaff = isStaff,
            CanEdit = canEdit,
            ConductedAt = assessment.ConductedAt,
            Students = studentScores.Select(s => new StudentScoreItemRowViewModel
            {
                StudentId = s.StudentId,
                StudentCode = s.StudentCode,
                FullName = s.FullName,
                ScoreId = s.ScoreId,
                Score = s.Score,
                Comment = s.Comment,
                IsLocked = s.IsLocked,
            }).ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Teacher,Staff")]
    public async Task<IActionResult> SaveBulk(BulkScoreFormModel model)
    {
        bool isStaff = User.IsInRole("Staff");
        int? teacherId = null;

        if (!isStaff)
        {
            var teacher = await ResolveCurrentTeacherAsync();
            if (teacher == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy hồ sơ giáo viên.";
                return RedirectToAction("Dashboard", "Teacher");
            }
            teacherId = teacher.Id;
        }

        if (model.Scores == null || !model.Scores.Any())
        {
            TempData["ErrorMessage"] = "Không có dữ liệu điểm để lưu.";
            return RedirectToAction(nameof(Entry), new { assessmentId = model.AssessmentId });
        }

        if (!ModelState.IsValid || model.Scores.Any(s => !s.TryGetDecimalScore(out _)))
        {
            TempData["ErrorMessage"] = "Dữ liệu điểm không hợp lệ. Điểm phải là số từ 0 đến 10 hoặc để trống khi lưu nháp.";
            return await RedisplayEntryAsync(model);
        }

        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdValue, out var userId)) return Forbid();

        var dto = new BulkSaveScoreDto
        {
            AssessmentId = model.AssessmentId,
            GlobalReason = model.Reason,
            LockAfterSave = !model.IsDraft,
            IsDraft = model.IsDraft,
            IsStaffOverride = isStaff,
            Scores = model.Scores.Select(s => new SaveStudentScoreDto
            {
                StudentId = s.StudentId,
                Score = s.GetDecimalScore(),
                Comment = s.Comment,
                Reason = model.Reason
            }).ToList()
        };

        var result = await _scoreService.SaveBulkScoresAsync(dto, userId, teacherId);
        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message ?? "Đã lưu bảng điểm thành công.";
        }
        else
        {
            TempData["ErrorMessage"] = result.Message ?? "Lưu bảng điểm thất bại.";
            if (result.ErrorCode == ErrorCodes.DatabaseError)
                return RedirectToAction(nameof(Entry), new { assessmentId = model.AssessmentId });
            return await RedisplayEntryAsync(model);
        }

        return RedirectToAction(nameof(Entry), new { assessmentId = model.AssessmentId });
    }

    // =========================================================================
    // 2. AUDIT LOG (LỊCH SỬ ĐIỀU CHỈNH ĐIỂM)
    // =========================================================================

    [HttpGet]
    public async Task<IActionResult> AuditLogs(int scoreId)
    {
        var teacher = await ResolveCurrentTeacherAsync();
        if (!User.IsInRole("Staff") && teacher == null) return Forbid();
        var result = await _scoreService.GetScoreAuditLogsAsync(scoreId, teacher?.Id);
        if (result.ErrorCode == ErrorCodes.AuthForbidden) return Forbid();
        return Json(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSingle(SingleScoreFormModel model)
    {
        bool isStaff = User.IsInRole("Staff");
        var teacher = isStaff ? null : await ResolveCurrentTeacherAsync();
        if (!isStaff && teacher == null) return Forbid();
        if (!ModelState.IsValid || !model.TryGetDecimalScore(out var score))
            return BadRequest(new { success = false, message = "Điểm không đúng định dạng số." });
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Forbid();

        var result = await _scoreService.SaveSingleScoreAsync(new SaveStudentScoreDto
        {
            AssessmentId = model.AssessmentId, StudentId = model.StudentId,
            Score = score, Comment = model.Comment, Reason = model.Reason
        }, userId, teacher?.Id);
        if (result.ErrorCode == ErrorCodes.AuthForbidden) return Forbid();
        return Json(result);
    }

    // =========================================================================
    // 3. SỔ ĐIỂM BỘ MÔN (SUBJECT SCORE MATRIX)
    // =========================================================================

    [HttpGet]
    [Authorize(Roles = "Teacher,Staff")]
    public async Task<IActionResult> SubjectMatrix(int? assignmentId, int? classId = null, int? semesterId = null)
    {
        bool isStaff = User.IsInRole("Staff");
        var teacher = isStaff ? null : await ResolveCurrentTeacherAsync();
        if (!isStaff && teacher == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy hồ sơ giáo viên.";
            return RedirectToAction("Dashboard", "Teacher");
        }

        var assignments = new List<TeachingAssignmentSummaryDto>();
        if (isStaff)
        {
            if (!classId.HasValue && assignmentId.HasValue)
            {
                var summary = await _assessmentService.GetTeachingAssignmentSummaryAsync(assignmentId.Value);
                classId = summary.Data?.ClassId;
                semesterId ??= summary.Data?.SemesterId;
            }
            var classEntity = classId.HasValue ? await _classRepository.GetByIdWithDetailsAsync(classId.Value) : null;
            if (classEntity != null)
            {
                foreach (var assignment in classEntity.TeachingAssignments.Where(ta => !semesterId.HasValue || ta.SemesterId == semesterId.Value))
                {
                    var summary = await _assessmentService.GetTeachingAssignmentSummaryAsync(assignment.Id);
                    if (summary.Success && summary.Data != null) assignments.Add(summary.Data);
                }
            }
        }
        else
        {
            var assignmentsResult = await _assessmentService.GetTeacherAssignmentsAsync(teacher!.Id);
            if (!assignmentsResult.Success) TempData["ErrorMessage"] = assignmentsResult.Message;
            assignments = assignmentsResult.Data?.ToList() ?? new();
        }

        var model = new SubjectScoreMatrixViewModel
        {
            TeacherAssignments = assignments
        };

        if (!assignments.Any())
        {
            return View(model);
        }

        int targetAssignmentId = assignmentId ?? assignments.First().Id;
        if (!assignments.Any(a => a.Id == targetAssignmentId))
        {
            targetAssignmentId = assignments.First().Id;
        }

        model.SelectedAssignmentId = targetAssignmentId;

        var matrixResult = await _scoreService.GetSubjectScoreMatrixAsync(targetAssignmentId, teacher?.Id);
        if (matrixResult.Success)
        {
            model.Matrix = matrixResult.Data;
        }
        else
        {
            TempData["ErrorMessage"] = matrixResult.Message;
        }

        return View(model);
    }

    // =========================================================================
    // 4. BẢNG ĐIỂM LỚP HỌC (HOMEROOM & CLASS SCORE REPORT)
    // =========================================================================

    [HttpGet]
    public async Task<IActionResult> HomeroomScores(int? classId, int? semesterId)
    {
        bool isStaff = User.IsInRole("Staff");
        Teacher? teacher = null;
        List<ClassItemOption> availableClasses;

        if (isStaff)
        {
            var allClasses = await _classRepository.GetByFiltersAsync(null, null);
            availableClasses = allClasses.Select(c => new ClassItemOption
            {
                Id = c.Id,
                ClassName = c.ClassName,
                AcademicYearCode = c.AcademicYear?.YearCode ?? string.Empty
            }).ToList();
        }
        else
        {
            teacher = await ResolveCurrentTeacherAsync();
            if (teacher == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy hồ sơ giáo viên.";
                return RedirectToAction("Dashboard", "Teacher");
            }

            var homeroomClasses = await _classRepository.GetHomeroomClassesByTeacherIdAsync(teacher.Id);
            availableClasses = homeroomClasses.Select(c => new ClassItemOption
            {
                Id = c.Id,
                ClassName = c.ClassName,
                AcademicYearCode = c.AcademicYear?.YearCode ?? string.Empty
            }).ToList();
        }

        var allYears = await _academicYearRepository.GetAllAsync();

        var semestersList = new List<SemesterItemOption>();
        foreach (var yr in allYears)
        {
            foreach (var sem in yr.Semesters)
            {
                semestersList.Add(new SemesterItemOption
                {
                    Id = sem.Id,
                    SemesterName = sem.SemesterName,
                    AcademicYearCode = yr.YearCode,
                    IsActive = sem.IsActive
                });
            }
        }

        var model = new HomeroomScoresViewModel
        {
            HomeroomClasses = availableClasses,
            Semesters = semestersList
        };

        if (!availableClasses.Any())
        {
            return View(model);
        }

        int targetClassId = classId ?? availableClasses.First().Id;
        if (!availableClasses.Any(c => c.Id == targetClassId))
        {
            targetClassId = availableClasses.First().Id;
        }

        var selectedClass = availableClasses.First(c => c.Id == targetClassId);
        semestersList = semestersList.Where(s => s.AcademicYearCode == selectedClass.AcademicYearCode).ToList();
        model.Semesters = semestersList;
        int targetSemesterId = semesterId.HasValue && semestersList.Any(s => s.Id == semesterId.Value)
            ? semesterId.Value
            : (semestersList.FirstOrDefault(s => s.IsActive)?.Id ?? semestersList.FirstOrDefault()?.Id ?? 0);

        model.SelectedClassId = targetClassId;
        model.SelectedSemesterId = targetSemesterId;

        if (targetSemesterId > 0)
        {
            int? teacherIdParam = isStaff ? null : teacher?.Id;
            var reportResult = await _scoreService.GetHomeroomScoreReportAsync(targetClassId, targetSemesterId, teacherIdParam);
            if (reportResult.Success)
            {
                model.Report = reportResult.Data;
            }
            else
            {
                TempData["ErrorMessage"] = reportResult.Message;
            }
        }

        return View(model);
    }

    // =========================================================================
    // 5. XEM BẢNG ĐIỂM CHI TIẾT HỌC SINH (STAFF & TEACHER)
    // =========================================================================

    [HttpGet]
    public async Task<IActionResult> StudentTranscript(int studentId, int? semesterId, int? subjectId)
    {
        var teacher = User.IsInRole("Staff") ? null : await ResolveCurrentTeacherAsync();
        if (!User.IsInRole("Staff") && teacher == null) return Forbid();
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Forbid();
        var allYears = await _academicYearRepository.GetAllAsync();
        var semestersList = new List<SemesterItemOption>();
        foreach (var yr in allYears)
        {
            foreach (var sem in yr.Semesters)
            {
                semestersList.Add(new SemesterItemOption
                {
                    Id = sem.Id,
                    SemesterName = sem.SemesterName,
                    AcademicYearCode = yr.YearCode,
                    IsActive = sem.IsActive
                });
            }
        }

        int targetSemesterId = semesterId ?? (semestersList.FirstOrDefault(s => s.IsActive)?.Id ?? (semestersList.FirstOrDefault()?.Id ?? 0));

        var model = new StudentTranscriptViewModel
        {
            StudentId = studentId,
            CurrentTeacherId = teacher?.Id,
            SelectedSemesterId = targetSemesterId,
            SelectedSubjectId = subjectId,
            AvailableSemesters = semestersList
        };

        if (targetSemesterId > 0)
        {
            var transcriptResult = await _scoreService.GetStudentTranscriptAsync(studentId, targetSemesterId, userId);
            if (transcriptResult.ErrorCode == ErrorCodes.AuthForbidden) return Forbid();
            if (transcriptResult.Success)
            {
                model.Transcript = transcriptResult.Data;
                if (subjectId.HasValue && !model.Transcript!.Subjects.Any(s => s.SubjectId == subjectId.Value))
                    model.SelectedSubjectId = null;
            }
            else
            {
                TempData["ErrorMessage"] = transcriptResult.Message;
            }
        }

        return View(model);
    }

    // =========================================================================
    // 5. THÊM NHẬN XÉT CỦA GIÁO VIÊN (TEACHER FEEDBACK)
    // =========================================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> AddFeedback(CreateFeedbackInputModel model)
    {
        var teacher = await ResolveCurrentTeacherAsync();
        if (teacher == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy hồ sơ giáo viên.";
            return RedirectToAction("Dashboard", "Teacher");
        }

        if (!ModelState.IsValid) return BadRequest();
        var dto = new CreateTeacherFeedbackDto
        {
            StudentId = model.StudentId,
            TeacherId = teacher.Id,
            SemesterId = model.SemesterId,
            FeedbackType = model.FeedbackType,
            Content = model.Content
        };

        var result = await _feedbackService.CreateFeedbackAsync(dto, teacher.Id);
        if (result.ErrorCode == ErrorCodes.AuthForbidden) return Forbid();
        if (result.Success)
        {
            TempData["SuccessMessage"] = "Đã gửi nhận xét cho học sinh thành công.";
        }
        else
        {
            TempData["ErrorMessage"] = result.Message ?? "Gửi nhận xét thất bại.";
        }

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return RedirectToAction(nameof(HomeroomScores));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> EditFeedback(EditFeedbackInputModel model)
    {
        var teacher = await ResolveCurrentTeacherAsync();
        if (teacher == null) return Forbid();
        if (!ModelState.IsValid) return BadRequest();
        var result = await _feedbackService.UpdateFeedbackAsync(new UpdateTeacherFeedbackDto
        { Id = model.Id, Content = model.Content, FeedbackType = model.FeedbackType }, teacher.Id);
        if (result.ErrorCode == ErrorCodes.AuthForbidden) return Forbid();
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(StudentTranscript), new { studentId = model.StudentId, semesterId = model.SemesterId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> DeleteFeedback(int id, int studentId, int semesterId)
    {
        var teacher = await ResolveCurrentTeacherAsync();
        if (teacher == null) return Forbid();
        var result = await _feedbackService.DeleteFeedbackAsync(id, teacher.Id);
        if (result.ErrorCode == ErrorCodes.AuthForbidden) return Forbid();
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(StudentTranscript), new { studentId, semesterId });
    }

    private async Task<IActionResult> RedisplayEntryAsync(BulkScoreFormModel input)
    {
        var result = await Entry(input.AssessmentId);
        if (result is ViewResult view && view.Model is ScoreEntryViewModel page)
        {
            view.ViewName = "Entry";
            foreach (var row in page.Students)
            {
                var posted = input.Scores.FirstOrDefault(s => s.StudentId == row.StudentId);
                if (posted == null) continue;
                posted.TryGetDecimalScore(out var score);
                row.Score = score;
                row.Comment = posted.Comment;
            }
        }
        return result;
    }

    // =========================================================================
    // PRIVATE HELPERS
    // =========================================================================

    private async Task<Teacher?> ResolveCurrentTeacherAsync()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdValue, out var userId))
        {
            return null;
        }

        return await _teacherRepository.GetByUserIdAsync(userId);
    }
}
