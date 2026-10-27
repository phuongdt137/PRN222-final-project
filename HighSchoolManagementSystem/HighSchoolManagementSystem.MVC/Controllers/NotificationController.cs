using System.Security.Claims;
using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Services.DTOs;
using HighSchoolManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HighSchoolManagementSystem.MVC.Controllers;

[Authorize]
public class NotificationController : Controller
{
    private readonly INotificationService _notificationService;

    public NotificationController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : null;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, bool? isRead = null)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Challenge();

        var result = await _notificationService.GetUserNotificationsAsync(userId.Value, page, 15, isRead);
        if (!result.Success || result.Data == null)
        {
            TempData["ErrorMessage"] = result.Message;
            return View(new NotificationPageDto());
        }

        return View(result.Data);
    }

    [HttpGet]
    public async Task<IActionResult> GetSummary()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new { success = false, message = "Chưa đăng nhập." });
        }

        var result = await _notificationService.GetSummaryAsync(userId.Value);
        if (!result.Success || result.Data == null)
        {
            return BadRequest(new { success = false, message = result.Message });
        }

        return Ok(new
        {
            success = true,
            unreadCount = result.Data.UnreadCount,
            recent = result.Data.RecentNotifications
        });
    }

    [HttpGet]
    [Authorize(Roles = "Principal,Staff")]
    public async Task<IActionResult> Create()
    {
        var classesResult = await _notificationService.GetClassesForAudienceAsync();
        ViewBag.Classes = classesResult.Success ? classesResult.Data : new List<Class>();
        return View(new CreateBroadcastNotificationDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Principal,Staff")]
    public async Task<IActionResult> Create(CreateBroadcastNotificationDto model)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Challenge();

        if (!ModelState.IsValid)
        {
            var classesResult = await _notificationService.GetClassesForAudienceAsync();
            ViewBag.Classes = classesResult.Success ? classesResult.Data : new List<Class>();
            return View(model);
        }

        var result = await _notificationService.BroadcastNotificationAsync(
            userId.Value,
            model.TargetAudience,
            model.Title,
            model.Content,
            model.SendEmail
        );

        if (!result.Success)
        {
            ModelState.AddModelError("", result.Message ?? "Không thể gửi thông báo.");
            var classesResult = await _notificationService.GetClassesForAudienceAsync();
            ViewBag.Classes = classesResult.Success ? classesResult.Data : new List<Class>();
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new { success = false, message = "Chưa đăng nhập." });
        }

        var result = await _notificationService.MarkAsReadAsync(id, userId.Value);
        return Json(new { success = result.Success, message = result.Message });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new { success = false, message = "Chưa đăng nhập." });
        }

        var result = await _notificationService.MarkAllAsReadAsync(userId.Value);
        return Json(new { success = result.Success, message = result.Message });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new { success = false, message = "Chưa đăng nhập." });
        }

        var result = await _notificationService.DeleteAsync(id, userId.Value);
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new { success = result.Success, message = result.Message });
        }

        if (result.Success)
        {
            TempData["SuccessMessage"] = "Đã xóa thông báo thành công.";
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
