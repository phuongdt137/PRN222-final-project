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

    [HttpGet, Authorize(Roles = "Staff,Teacher")]
    public async Task<IActionResult> Take(int id)
    {
        var result = await attendance.GetSheetAsync(UserId, id);
        return result.Success ? View(result.Data) : Error(result);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Staff,Teacher")]
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
