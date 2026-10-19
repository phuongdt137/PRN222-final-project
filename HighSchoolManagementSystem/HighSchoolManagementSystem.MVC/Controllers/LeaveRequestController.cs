using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.MVC.Models.ViewModels;
using HighSchoolManagementSystem.Repositories.Interfaces;
using HighSchoolManagementSystem.Services.Common;
using HighSchoolManagementSystem.Services.Interfaces;

namespace HighSchoolManagementSystem.MVC.Controllers;

[Authorize(Roles = "Staff,Teacher,Student,Parent")]
public class LeaveRequestController : Controller
{
    private readonly ILeaveRequestService _leaveRequestService;
    private readonly IStudentRepository _studentRepository;
    private readonly ITeacherRepository _teacherRepository;
    private readonly IFileStorageService _fileStorageService;

    public LeaveRequestController(
        ILeaveRequestService leaveRequestService,
        IStudentRepository studentRepository,
        ITeacherRepository teacherRepository,
        IFileStorageService fileStorageService)
    {
        _leaveRequestService = leaveRequestService;
        _studentRepository = studentRepository;
        _teacherRepository = teacherRepository;
        _fileStorageService = fileStorageService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? status)
    {
        if (!Request.Query.ContainsKey("status") && string.IsNullOrWhiteSpace(status) && CanReview())
        {
            status = "Pending";
        }

        var role = GetPrimaryRole();
        var userId = GetUserId();
        if (userId == null)
        {
            return RedirectToAction("Login", "Auth");
        }

        var result = role switch
        {
            "Staff" => await _leaveRequestService.GetForStaffAsync(status),
            "Teacher" => await _leaveRequestService.GetForTeacherAsync(userId.Value, status),
            "Student" => await _leaveRequestService.GetForStudentAsync(userId.Value, status),
            "Parent" => await _leaveRequestService.GetForParentAsync(userId.Value, status),
            _ => ServiceResult<IReadOnlyList<LeaveRequest>>.Fail(ErrorCodes.AuthForbidden, "Không có quyền.")
        };

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
        }

        var page = new LeaveRequestListPageViewModel
        {
            SelectedStatus = status,
            ViewerRole = role,
            CanCreate = role is "Student" or "Parent",
            CanReview = CanReview(),
            Statuses = BuildStatusOptions(status),
            Items = result.Success && result.Data != null
                ? result.Data.Select(MapListItem).ToList()
                : Array.Empty<LeaveRequestListItemViewModel>()
        };

        return View(page);
    }

    [HttpGet]
    [Authorize(Roles = "Student,Parent")]
    public async Task<IActionResult> Create()
    {
        var model = new CreateLeaveRequestViewModel
        {
            StartDate = SchoolDate.Today,
            EndDate = SchoolDate.Today
        };
        await PopulateCreateFormAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Student,Parent")]
    public async Task<IActionResult> Create(CreateLeaveRequestViewModel model)
    {
        await PopulateCreateFormAsync(model);

        var userId = GetUserId();
        if (userId == null)
        {
            return RedirectToAction("Login", "Auth");
        }

        if (User.IsInRole("Student"))
        {
            var student = await _studentRepository.GetByUserIdAsync(userId.Value);
            if (student == null)
            {
                ModelState.AddModelError(string.Empty, "Không tìm thấy hồ sơ học sinh.");
                return View(model);
            }

            model.StudentId = student.Id;
            ModelState.Remove(nameof(model.StudentId));
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        string? attachmentPath = null;
        if (model.AttachmentFile != null && model.AttachmentFile.Length > 0)
        {
            await using var stream = model.AttachmentFile.OpenReadStream();
            var saveResult = await _fileStorageService.SaveFileAsync(
                stream,
                model.AttachmentFile.FileName,
                "uploads/leave-requests");

            if (!saveResult.Success || string.IsNullOrEmpty(saveResult.Data))
            {
                ModelState.AddModelError(nameof(model.AttachmentFile), saveResult.Message ?? "Tải minh chứng thất bại.");
                return View(model);
            }

            attachmentPath = saveResult.Data;
        }

        var result = await _leaveRequestService.CreateAsync(
            model.StudentId,
            userId.Value,
            model.Reason,
            model.StartDate,
            model.EndDate,
            attachmentPath);

        if (!result.Success)
        {
            if (!string.IsNullOrEmpty(attachmentPath))
            {
                await _fileStorageService.DeleteFileAsync(attachmentPath);
            }

            ModelState.AddModelError(string.Empty, result.Message ?? "Không thể gửi đơn.");
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = result.Data!.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var access = await EnsureCanViewAsync(id);
        if (access.Result != null)
        {
            return access.Result;
        }

        var leave = access.Leave!;
        var userId = GetUserId()!.Value;

        var model = MapDetails(leave);
        model.CanReview = CanReview() && leave.Status == "Pending" && await CanReviewLeaveAsync(leave);
        model.CanCancel = leave.Status == "Pending" && leave.RequestBy == userId;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Staff,Teacher")]
    public async Task<IActionResult> Review(ReviewLeaveRequestViewModel model, string? returnUrl = null)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return RedirectToAction("Login", "Auth");
        }

        var isStaff = User.IsInRole("Staff");
        int? teacherId = null;
        if (!isStaff)
        {
            var teacher = await _teacherRepository.GetByUserIdAsync(userId.Value);
            teacherId = teacher?.Id;
        }

        var result = await _leaveRequestService.ReviewAsync(
            model.Id,
            userId.Value,
            model.Approve,
            model.ReviewNote,
            isStaff,
            teacherId);

        if (!result.Success)
        {
            if (result.ErrorCode == ErrorCodes.AuthForbidden)
            {
                return Forbid();
            }

            TempData["ErrorMessage"] = result.Message;
            return LocalRedirectIfSafe(returnUrl) ?? RedirectToAction(nameof(Details), new { id = model.Id });
        }

        TempData["SuccessMessage"] = result.Message;
        return LocalRedirectIfSafe(returnUrl) ?? RedirectToAction(nameof(Details), new { id = model.Id });
    }

    private IActionResult? LocalRedirectIfSafe(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return null;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Student,Parent")]
    public async Task<IActionResult> Cancel(int id)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return RedirectToAction("Login", "Auth");
        }

        var result = await _leaveRequestService.CancelAsync(id, userId.Value);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateCreateFormAsync(CreateLeaveRequestViewModel model)
    {
        model.ShowStudentPicker = User.IsInRole("Parent");
        if (!model.ShowStudentPicker)
        {
            return;
        }

        var userId = GetUserId();
        if (userId == null)
        {
            return;
        }

        var studentsResult = await _leaveRequestService.GetStudentsForParentAsync(userId.Value);
        model.Students = studentsResult.Success && studentsResult.Data != null
            ? studentsResult.Data.Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = $"{s.StudentCode} - {s.User.FullName}",
                Selected = s.Id == model.StudentId
            })
            : Enumerable.Empty<SelectListItem>();
    }

    private async Task<(IActionResult? Result, LeaveRequest? Leave)> EnsureCanViewAsync(int id)
    {
        var result = await _leaveRequestService.GetByIdAsync(id);
        if (!result.Success || result.Data == null)
        {
            TempData["ErrorMessage"] = result.Message ?? "Không tìm thấy đơn.";
            return (RedirectToAction(nameof(Index)), null);
        }

        var leave = result.Data;
        var userId = GetUserId();
        if (userId == null)
        {
            return (RedirectToAction("Login", "Auth"), null);
        }

        if (User.IsInRole("Staff"))
        {
            return (null, leave);
        }

        if (User.IsInRole("Teacher"))
        {
            return await CanReviewLeaveAsync(leave)
                ? (null, leave)
                : (Forbid(), null);
        }

        if (User.IsInRole("Student"))
        {
            var student = await _studentRepository.GetByUserIdAsync(userId.Value);
            if (student != null && student.Id == leave.StudentId)
            {
                return (null, leave);
            }

            return (Forbid(), null);
        }

        if (User.IsInRole("Parent"))
        {
            var children = await _leaveRequestService.GetStudentsForParentAsync(userId.Value);
            if (children.Success && children.Data != null && children.Data.Any(s => s.Id == leave.StudentId))
            {
                return (null, leave);
            }

            return (Forbid(), null);
        }

        return (Forbid(), null);
    }

    private async Task<bool> CanReviewLeaveAsync(LeaveRequest leave)
    {
        if (User.IsInRole("Staff"))
        {
            return true;
        }

        if (!User.IsInRole("Teacher"))
        {
            return false;
        }

        var userId = GetUserId();
        if (userId == null)
        {
            return false;
        }

        var teacher = await _teacherRepository.GetByUserIdAsync(userId.Value);
        if (teacher == null)
        {
            return false;
        }

        return leave.Student.StudentClasses.Any(sc =>
            sc.Status == "Enrolled" &&
            sc.LeftAt == null &&
            sc.Class.HomeroomTeacherId == teacher.Id);
    }

    private bool CanReview() => User.IsInRole("Staff") || User.IsInRole("Teacher");

    private string GetPrimaryRole()
    {
        if (User.IsInRole("Staff")) return "Staff";
        if (User.IsInRole("Teacher")) return "Teacher";
        if (User.IsInRole("Student")) return "Student";
        if (User.IsInRole("Parent")) return "Parent";
        return string.Empty;
    }

    private int? GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var id) ? id : null;
    }

    private static IEnumerable<SelectListItem> BuildStatusOptions(string? selected) =>
        new[]
        {
            new SelectListItem("Chờ duyệt", "Pending", selected == "Pending"),
            new SelectListItem("Đã duyệt", "Approved", selected == "Approved"),
            new SelectListItem("Từ chối", "Rejected", selected == "Rejected"),
            new SelectListItem("Đã hủy", "Cancelled", selected == "Cancelled")
        };

    private static LeaveRequestListItemViewModel MapListItem(LeaveRequest lr)
    {
        var currentClass = lr.Student.StudentClasses
            .FirstOrDefault(sc => sc.Status == "Enrolled" && sc.LeftAt == null);

        return new LeaveRequestListItemViewModel
        {
            Id = lr.Id,
            StudentCode = lr.Student.StudentCode,
            StudentName = lr.Student.User.FullName,
            ClassName = currentClass?.Class.ClassName,
            RequestedByName = lr.RequestedByUser.FullName,
            StartDate = lr.StartDate,
            EndDate = lr.EndDate,
            Reason = lr.Reason,
            Status = lr.Status,
            RequestedAt = lr.RequestedAt,
            ReviewedByName = lr.ReviewedByUser?.FullName
        };
    }

    private static LeaveRequestDetailsViewModel MapDetails(LeaveRequest lr)
    {
        var currentClass = lr.Student.StudentClasses
            .FirstOrDefault(sc => sc.Status == "Enrolled" && sc.LeftAt == null);

        return new LeaveRequestDetailsViewModel
        {
            Id = lr.Id,
            StudentCode = lr.Student.StudentCode,
            StudentName = lr.Student.User.FullName,
            ClassName = currentClass?.Class.ClassName,
            RequestedByName = lr.RequestedByUser.FullName,
            StartDate = lr.StartDate,
            EndDate = lr.EndDate,
            Reason = lr.Reason,
            Attachment = lr.Attachment,
            Status = lr.Status,
            RequestedAt = lr.RequestedAt,
            ReviewNote = lr.ReviewNote,
            ReviewAt = lr.ReviewAt,
            ReviewedByName = lr.ReviewedByUser?.FullName
        };
    }
}
