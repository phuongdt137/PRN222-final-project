using System.Security.Claims;
using HighSchoolManagementSystem.Services.Common;
using HighSchoolManagementSystem.Services.DTOs;
using HighSchoolManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HighSchoolManagementSystem.MVC.Controllers;

[Authorize(Roles = "Staff,Teacher,Student,Parent")]
public class AttendanceController(IAttendanceService attendance) : Controller
{
    private int UserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    [HttpGet, Authorize(Roles = "Staff")]
    public async Task<IActionResult> Manage(AttendanceFilter filter) => await List(filter);

    [HttpGet, Authorize(Roles = "Teacher")]
    public async Task<IActionResult> MyTeachingSessions(AttendanceFilter filter)
    {
        filter.FromDate ??= SchoolDate.Today;
        filter.ToDate ??= SchoolDate.Today;
        return await List(filter);
    }

    private async Task<IActionResult> List(AttendanceFilter filter)
    {
        var result = await attendance.GetSessionsAsync(UserId, filter);
        return result.Success ? View("Index", result.Data) : Error(result);
    }

    [HttpGet, Authorize(Roles = "Staff")]
    public async Task<IActionResult> Create(DateTime? date, int? semesterId, int? classId)
    {
        var result = await attendance.PreviewAsync(UserId, new() { Date = date ?? SchoolDate.Today, SemesterId = semesterId, ClassId = classId });
        return result.Success ? View(result.Data) : Error(result);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Staff")]
    public async Task<IActionResult> Create([Bind("Date,SemesterId,ClassId,EntryIds")] AttendanceCreateDto input)
    {
        if (!ModelState.IsValid) return BadRequest("Ngày hoặc tiết học không hợp lệ.");
        var result = await attendance.CreateAsync(UserId, input);
        if (!result.Success)
        {
            if (result.ErrorCode == ErrorCodes.AuthForbidden) return Forbid();
            ModelState.AddModelError("", result.Message ?? "Không thể tạo bảng.");
            var preview = await attendance.PreviewAsync(UserId, input);
            return preview.Success ? View(preview.Data) : Error(preview);
        }
        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Manage), new { fromDate = input.Date.ToString("yyyy-MM-dd"), toDate = input.Date.ToString("yyyy-MM-dd"), input.SemesterId, input.ClassId });
    }

    [HttpGet, Authorize(Roles = "Staff,Teacher")]
    public async Task<IActionResult> Take(int id)
    {
        var result = await attendance.GetSheetAsync(UserId, id);
        return result.Success ? View(result.Data) : Error(result);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Teacher")]
    public async Task<IActionResult> Save(AttendanceSaveDto input)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Dữ liệu không hợp lệ. Kết quả chưa được lưu; vui lòng kiểm tra lại.";
            return RedirectToAction(nameof(Take), new { id = input.SessionId });
        }
        var result = await attendance.SaveAsync(UserId, input);
        if (result.ErrorCode == ErrorCodes.AuthForbidden) return Forbid();
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Take), new { id = input.SessionId });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Staff")]
    public async Task<IActionResult> ManageSession(int sessionId, string rowVersion, string operation, string? reason)
    {
        var result = await attendance.ManageSessionAsync(UserId, sessionId, rowVersion, operation, reason);
        if (result.ErrorCode == ErrorCodes.AuthForbidden) return Forbid();
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Take), new { id = sessionId });
    }

    [HttpGet, Authorize(Roles = "Student")]
    public async Task<IActionResult> MyAttendance(AttendanceFilter filter) => await History(filter);

    [HttpGet, Authorize(Roles = "Parent")]
    public async Task<IActionResult> ChildAttendance(AttendanceFilter filter) => await History(filter);

    private async Task<IActionResult> History(AttendanceFilter filter)
    {
        var result = await attendance.GetHistoryAsync(UserId, filter);
        return result.Success ? View("History", result.Data) : Error(result);
    }

    private IActionResult Error(ServiceResult result) => result.ErrorCode switch
    {
        ErrorCodes.AuthForbidden => Forbid(),
        ErrorCodes.ResourceNotFound => NotFound(result.Message),
        _ => BadRequest(result.Message)
    };
}
