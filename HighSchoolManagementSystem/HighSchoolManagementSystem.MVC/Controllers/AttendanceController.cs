using System.Security.Claims;
using HighSchoolManagementSystem.Services.Common;
using HighSchoolManagementSystem.Services.DTOs;
using HighSchoolManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HighSchoolManagementSystem.MVC.Controllers;

[Authorize(Roles = "Staff,Teacher,Student,Parent")]
public class AttendanceController : Controller
{
    private readonly IAttendanceService _attendanceService;

    public AttendanceController(IAttendanceService attendanceService)
    {
        _attendanceService = attendanceService;
    }

    private int GetUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        int.TryParse(userId, out var id);

        return id;
    }

    [HttpGet]
    [Authorize(Roles = "Staff")]
    public async Task<IActionResult> Manage(AttendanceFilter filter)
    {
        return await ShowSessionsAsync(filter);
    }

    [HttpGet]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> MyTeachingSessions(int page = 1)
    {
        var filter = new AttendanceFilter { Page = page };

        return await ShowSessionsAsync(filter);
    }

    private async Task<IActionResult> ShowSessionsAsync(AttendanceFilter filter)
    {
        var result = await _attendanceService.GetSessionsAsync(GetUserId(), filter);

        if (!result.Success)
        {
            return Error(result);
        }

        return View("Index", result.Data);
    }

    [HttpGet]
    [Authorize(Roles = "Staff,Teacher")]
    public async Task<IActionResult> Take(int id = 0, int? timetableEntryId = null, DateTime? date = null)
    {
        var result = await _attendanceService.GetSheetAsync(GetUserId(), id, timetableEntryId, date);

        if (!result.Success)
        {
            return Error(result);
        }

        return View(result.Data);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Staff,Teacher")]
    public async Task<IActionResult> Save(AttendanceSaveDto input)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Dữ liệu không hợp lệ. Kết quả chưa được lưu; vui lòng kiểm tra lại.";
        }
        else
        {
            var result = await _attendanceService.SaveAsync(GetUserId(), input);

            if (result.ErrorCode == ErrorCodes.AuthForbidden)
            {
                return Forbid();
            }

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }
        }

        return RedirectToAction(nameof(Take), new
        {
            id = input.SessionId,
            timetableEntryId = input.TimetableEntryId,
            date = input.Date.ToString("yyyy-MM-dd")
        });
    }

    [HttpGet]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> MyAttendance(AttendanceFilter filter)
    {
        return await ShowHistoryAsync(filter);
    }

    [HttpGet]
    [Authorize(Roles = "Parent")]
    public async Task<IActionResult> ChildAttendance(AttendanceFilter filter)
    {
        return await ShowHistoryAsync(filter);
    }

    private async Task<IActionResult> ShowHistoryAsync(AttendanceFilter filter)
    {
        var result = await _attendanceService.GetHistoryAsync(GetUserId(), filter);

        if (!result.Success)
        {
            return Error(result);
        }

        return View("History", result.Data);
    }

    private IActionResult Error(ServiceResult result)
    {
        if (result.ErrorCode == ErrorCodes.AuthForbidden)
        {
            return Forbid();
        }

        if (result.ErrorCode == ErrorCodes.ResourceNotFound)
        {
            return NotFound(result.Message);
        }

        return BadRequest(result.Message);
    }
}
