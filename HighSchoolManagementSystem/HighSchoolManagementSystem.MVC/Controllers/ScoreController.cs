using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HighSchoolManagementSystem.MVC.Models.ViewModels.Score;
using HighSchoolManagementSystem.Services.Common;
using HighSchoolManagementSystem.Services.DTOs;
using HighSchoolManagementSystem.Services.Interfaces;

namespace HighSchoolManagementSystem.MVC.Controllers;

[Authorize(Roles = "Teacher,Staff,Principal")]
public class ScoreController : Controller
{
    private readonly IScoreService _scoreService;
    private readonly ITeacherFeedbackService _feedbackService;

    public ScoreController(
        IScoreService scoreService,
        ITeacherFeedbackService feedbackService)
    {
        _scoreService = scoreService;
        _feedbackService = feedbackService;
    }

    // =========================================================================
    // 1. NHẬP ĐIỂM (STUDENT & BULK SCORE ENTRY)
    // =========================================================================

    [HttpGet]
    [Authorize(Roles = "Teacher,Staff,Principal")]
    public async Task<IActionResult> Entry(int assessmentId)
    {
        bool isStaff = User.IsInRole("Staff");
        bool isPrincipal = User.IsInRole("Principal");
        int? teacherId = null;

        if (!isStaff && !isPrincipal)
        {
            teacherId = await ResolveCurrentTeacherIdAsync();
            if (!teacherId.HasValue)
            {
                TempData["ErrorMessage"] = "Không tìm thấy hồ sơ giáo viên của tài khoản này.";
                return RedirectToAction("Dashboard", "Teacher");
            }
        }

        var contextResult = await _scoreService.GetScoreEntryContextAsync(assessmentId, teacherId, isStaff, isPrincipal);
        if (!contextResult.Success || contextResult.Data == null)
        {
            TempData["ErrorMessage"] = contextResult.Message ?? "Không thể tải thông tin nhập điểm.";
            if (isPrincipal) return RedirectToAction("AcademicReview", "Principal");
            if (isStaff) return RedirectToAction(nameof(HomeroomScores));
            return RedirectToAction("Index", "Assessment");
        }

        var ctx = contextResult.Data;
        var assessment = ctx.Assessment;

        var model = new ScoreEntryViewModel
        {
            AssessmentId = assessment.Id,
            AssessmentName = assessment.AssessmentName,
            AssessmentTypeName = assessment.AssessmentTypeName,
            Weight = assessment.Weight,
            MinScore = assessment.MinScore,
            MaxScore = assessment.MaxScore,
            SubjectName = assessment.SubjectName,
            ClassName = assessment.ClassName,
            SemesterName = assessment.SemesterName,
            AssignmentId = assessment.TeachingAssignmentId,
            IsAssessmentLocked = ctx.IsAssessmentLocked,
            IsStaff = isStaff,
            IsPrincipal = isPrincipal,
            IsSemesterFinalized = ctx.IsSemesterFinalized,
            CanEdit = ctx.CanEdit,
            ConductedAt = assessment.ConductedAt,
            Students = ctx.Students.Select(s => new StudentScoreItemRowViewModel
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
    [Authorize(Roles = "Teacher,Staff,Principal")]
    public async Task<IActionResult> SaveBulk(BulkScoreFormModel model)
    {
        bool isStaff = User.IsInRole("Staff");
        bool isPrincipal = User.IsInRole("Principal");
        int? teacherId = null;

        if (!isStaff && !isPrincipal)
        {
            teacherId = await ResolveCurrentTeacherIdAsync();
            if (!teacherId.HasValue)
            {
                TempData["ErrorMessage"] = "Không tìm thấy hồ sơ giáo viên.";
                return RedirectToAction("Dashboard", "Teacher");
            }
        }

        if (model.Scores == null || !model.Scores.Any())
        {
            TempData["ErrorMessage"] = "Không có dữ liệu điểm để lưu.";
            return RedirectToAction(nameof(Entry), new { assessmentId = model.AssessmentId });
        }

        if (!ModelState.IsValid || model.Scores.Any(s => !s.TryGetDecimalScore(out _)))
        {
            TempData["ErrorMessage"] = "Dữ liệu điểm không hợp lệ. Điểm phải là số từ 0 đến 10 hoặc để trống.";
            return await RedisplayEntryAsync(model);
        }

        if (!TryGetCurrentUserId(out var userId)) return Forbid();

        var dto = new BulkSaveScoreDto
        {
            AssessmentId = model.AssessmentId,
            GlobalReason = model.Reason,
            LockAfterSave = isStaff || isPrincipal,
            IsDraft = false,
            IsStaffOverride = isStaff,
            IsPrincipalOverride = isPrincipal,
            Scores = model.Scores.Select(s => new SaveStudentScoreDto
            {
                StudentId = s.StudentId,
                Score = s.GetDecimalScore(),
                Comment = s.Comment,
                Reason = model.Reason,
                IsPrincipalOverride = isPrincipal
            }).ToList()
        };

        var result = await _scoreService.SaveBulkScoresAsync(dto, userId, teacherId, isPrincipal);
        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message ?? "Đã lưu bảng điểm thành công.";
            return RedirectToAction(nameof(Entry), new { assessmentId = model.AssessmentId });
        }

        TempData["ErrorMessage"] = result.Message ?? "Lưu bảng điểm thất bại.";
        if (result.ErrorCode == ErrorCodes.DatabaseError)
            return RedirectToAction(nameof(Entry), new { assessmentId = model.AssessmentId });

        return await RedisplayEntryAsync(model);
    }

    // =========================================================================
    // 2. AUDIT LOG & LƯU ĐIỂM ĐƠN LẺ
    // =========================================================================

    [HttpGet]
    public async Task<IActionResult> AuditLogs(int scoreId)
    {
        var teacherId = await ResolveCurrentTeacherIdAsync();
        if (!User.IsInRole("Staff") && !User.IsInRole("Principal") && !teacherId.HasValue) return Forbid();

        var result = await _scoreService.GetScoreAuditLogsAsync(scoreId, teacherId);
        if (result.ErrorCode == ErrorCodes.AuthForbidden) return Forbid();
        return Json(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Teacher,Staff,Principal")]
    public async Task<IActionResult> SaveSingle(SingleScoreFormModel model)
    {
        bool isStaff = User.IsInRole("Staff");
        bool isPrincipal = User.IsInRole("Principal");
        var teacherId = (!isStaff && !isPrincipal) ? await ResolveCurrentTeacherIdAsync() : null;

        if (!isStaff && !isPrincipal && !teacherId.HasValue) return Forbid();
        if (!ModelState.IsValid || !model.TryGetDecimalScore(out var score))
            return BadRequest(new { success = false, message = "Điểm không đúng định dạng số." });
        if (!TryGetCurrentUserId(out var userId)) return Forbid();

        var result = await _scoreService.SaveSingleScoreAsync(new SaveStudentScoreDto
        {
            AssessmentId = model.AssessmentId,
            StudentId = model.StudentId,
            Score = score,
            Comment = model.Comment,
            Reason = model.Reason,
            IsPrincipalOverride = isPrincipal
        }, userId, teacherId, isPrincipal);

        if (result.ErrorCode == ErrorCodes.AuthForbidden) return Forbid();
        return Json(result);
    }

    // =========================================================================
    // 3. SỔ ĐIỂM BỘ MÔN (SUBJECT SCORE MATRIX)
    // =========================================================================

    [HttpGet]
    [Authorize(Roles = "Teacher,Staff,Principal")]
    public async Task<IActionResult> SubjectMatrix(int? assignmentId, int? classId = null, int? semesterId = null)
    {
        bool isPrivileged = User.IsInRole("Staff") || User.IsInRole("Principal");
        var teacherId = isPrivileged ? null : await ResolveCurrentTeacherIdAsync();

        if (!isPrivileged && !teacherId.HasValue)
        {
            TempData["ErrorMessage"] = "Không tìm thấy hồ sơ giáo viên.";
            return RedirectToAction("Dashboard", "Teacher");
        }

        var assignmentsResult = await _scoreService.GetSubjectAssignmentsForViewAsync(
            assignmentId, classId, semesterId, teacherId, isPrivileged);

        if (!assignmentsResult.Success)
        {
            TempData["ErrorMessage"] = assignmentsResult.Message;
        }

        var assignments = assignmentsResult.Data ?? new List<TeachingAssignmentSummaryDto>();
        var model = new SubjectScoreMatrixViewModel
        {
            TeacherAssignments = assignments
        };

        if (!assignments.Any())
        {
            return View(model);
        }

        int targetAssignmentId = assignmentId.HasValue && assignments.Any(a => a.Id == assignmentId.Value)
            ? assignmentId.Value
            : assignments.First().Id;

        model.SelectedAssignmentId = targetAssignmentId;

        var matrixResult = await _scoreService.GetSubjectScoreMatrixAsync(targetAssignmentId, teacherId);
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
        bool isPrivileged = User.IsInRole("Staff") || User.IsInRole("Principal");
        var teacherId = isPrivileged ? null : await ResolveCurrentTeacherIdAsync();

        if (!isPrivileged && !teacherId.HasValue)
        {
            TempData["ErrorMessage"] = "Không tìm thấy hồ sơ giáo viên.";
            return RedirectToAction("Dashboard", "Teacher");
        }

        var pageData = await _scoreService.GetHomeroomScoresPageDataAsync(classId, semesterId, teacherId, isPrivileged);
        if (!string.IsNullOrEmpty(pageData.ReportErrorMessage))
        {
            TempData["ErrorMessage"] = pageData.ReportErrorMessage;
        }

        var model = new HomeroomScoresViewModel
        {
            HomeroomClasses = pageData.AvailableClasses.Select(c => new ClassItemOption
            {
                Id = c.Id,
                ClassName = c.ClassName,
                AcademicYearCode = c.AcademicYearCode
            }).ToList(),
            Semesters = pageData.AvailableSemesters.Select(s => new SemesterItemOption
            {
                Id = s.Id,
                SemesterName = s.SemesterName,
                AcademicYearCode = s.AcademicYearCode,
                IsActive = s.IsActive
            }).ToList(),
            SelectedClassId = pageData.SelectedClassId,
            SelectedSemesterId = pageData.SelectedSemesterId,
            Report = pageData.Report
        };

        return View(model);
    }

    // =========================================================================
    // 5. XEM BẢNG ĐIỂM CHI TIẾT HỌC SINH (STAFF & TEACHER)
    // =========================================================================

    [HttpGet]
    public async Task<IActionResult> StudentTranscript(int studentId, int? semesterId, int? subjectId)
    {
        bool isPrivileged = User.IsInRole("Staff") || User.IsInRole("Principal");
        var teacherId = isPrivileged ? null : await ResolveCurrentTeacherIdAsync();

        if (!isPrivileged && !teacherId.HasValue) return Forbid();
        if (!TryGetCurrentUserId(out var userId)) return Forbid();

        var semestersDto = await _scoreService.GetAllSemesterOptionsAsync();
        var semestersList = semestersDto.Select(s => new SemesterItemOption
        {
            Id = s.Id,
            SemesterName = s.SemesterName,
            AcademicYearCode = s.AcademicYearCode,
            IsActive = s.IsActive
        }).ToList();

        int targetSemesterId = semesterId ?? (semestersList.FirstOrDefault(s => s.IsActive)?.Id ?? (semestersList.FirstOrDefault()?.Id ?? 0));

        var model = new StudentTranscriptViewModel
        {
            StudentId = studentId,
            CurrentTeacherId = teacherId,
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
    // 6. NHẬN XÉT CỦA GIÁO VIÊN (TEACHER FEEDBACK)
    // =========================================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> AddFeedback(CreateFeedbackInputModel model)
    {
        var teacherId = await ResolveCurrentTeacherIdAsync();
        if (!teacherId.HasValue)
        {
            TempData["ErrorMessage"] = "Không tìm thấy hồ sơ giáo viên.";
            return RedirectToAction("Dashboard", "Teacher");
        }

        if (!ModelState.IsValid) return BadRequest();

        var result = await _feedbackService.CreateFeedbackAsync(new CreateTeacherFeedbackDto
        {
            StudentId = model.StudentId,
            TeacherId = teacherId.Value,
            SemesterId = model.SemesterId,
            FeedbackType = model.FeedbackType,
            Content = model.Content
        }, teacherId.Value);

        if (result.ErrorCode == ErrorCodes.AuthForbidden) return Forbid();
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
            ? "Đã gửi nhận xét cho học sinh thành công."
            : (result.Message ?? "Gửi nhận xét thất bại.");

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
        var teacherId = await ResolveCurrentTeacherIdAsync();
        if (!teacherId.HasValue) return Forbid();
        if (!ModelState.IsValid) return BadRequest();

        var result = await _feedbackService.UpdateFeedbackAsync(new UpdateTeacherFeedbackDto
        {
            Id = model.Id,
            Content = model.Content,
            FeedbackType = model.FeedbackType
        }, teacherId.Value);

        if (result.ErrorCode == ErrorCodes.AuthForbidden) return Forbid();
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(StudentTranscript), new { studentId = model.StudentId, semesterId = model.SemesterId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> DeleteFeedback(int id, int studentId, int semesterId)
    {
        var teacherId = await ResolveCurrentTeacherIdAsync();
        if (!teacherId.HasValue) return Forbid();

        var result = await _feedbackService.DeleteFeedbackAsync(id, teacherId.Value);
        if (result.ErrorCode == ErrorCodes.AuthForbidden) return Forbid();
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(StudentTranscript), new { studentId, semesterId });
    }

    // =========================================================================
    // PRIVATE HELPERS
    // =========================================================================

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

    private bool TryGetCurrentUserId(out int userId)
    {
        return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
    }

    private async Task<int?> ResolveCurrentTeacherIdAsync()
    {
        if (!TryGetCurrentUserId(out var userId)) return null;
        return await _scoreService.GetTeacherIdByUserIdAsync(userId);
    }
}
