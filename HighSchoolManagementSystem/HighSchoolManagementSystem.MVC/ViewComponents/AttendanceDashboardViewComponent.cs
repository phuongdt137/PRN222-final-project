using System.Security.Claims;
using HighSchoolManagementSystem.Services.Common;
using HighSchoolManagementSystem.Services.DTOs;
using HighSchoolManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HighSchoolManagementSystem.MVC.ViewComponents;

public class AttendanceDashboardViewComponent(IAttendanceService attendance) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(string mode = "card")
    {
        var user = HttpContext.User;
        if (!int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Content("");
        var today = SchoolDate.Today;
        if (user.IsInRole("Teacher"))
        {
            var result = await attendance.GetSessionsAsync(userId, new AttendanceFilter { FromDate = today, ToDate = today });
            return result.Success ? View(mode == "card" ? "TeacherCard" : "TeacherTable", result.Data) : Content(result.Message ?? "");
        }
        var history = await attendance.GetHistoryAsync(userId, new AttendanceFilter
        {
            FromDate = mode == "card" ? new DateTime(today.Year, today.Month, 1) : today, ToDate = today
        });
        return history.Success ? View(mode == "card" ? "StudentCard" : "StudentTable", history.Data) : Content(history.Message ?? "");
    }
}
