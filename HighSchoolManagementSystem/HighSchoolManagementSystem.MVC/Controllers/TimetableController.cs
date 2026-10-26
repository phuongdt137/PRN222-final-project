using System.Security.Claims;
using HighSchoolManagementSystem.MVC.Models.ViewModels;
using HighSchoolManagementSystem.MVC.Models.ViewModels.Timetable;
using HighSchoolManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HighSchoolManagementSystem.MVC.Controllers;

public class TimetableController(ITimetableService timetableService, ISemesterService semesterService) : Controller
{
    [Authorize(Roles = "Staff")]
    public async Task<IActionResult> Index(int? teacherId, int? semesterId, DateTime? weekStart, bool editMode = false)
    {
        var activeSemesterId = await timetableService.GetActiveSemesterIdAsync();
        semesterId ??= activeSemesterId;
        weekStart = await NormalizeWeekStartAsync(semesterId, weekStart);
        var entriesResult = await timetableService.GetAllAsync(semesterId);
        var assignmentsResult = await timetableService.GetAssignmentsAsync(semesterId);
        var slotsResult = await timetableService.GetTimeSlotsAsync();
        var semestersResult = await semesterService.GetAllAsync();
        if (!entriesResult.Success) TempData["ErrorMessage"] = entriesResult.Message;

        var entries = teacherId.HasValue
            ? (entriesResult.Data ?? []).Where(e => e.TeachingAssignment.TeacherId == teacherId.Value).ToList()
            : [];
        var model = MapList(entries, weekStart, slotsResult.Data);
        model.SelectedTeacherId = teacherId;
        model.SelectedSemesterId = semesterId;
        model.EditMode = editMode && teacherId.HasValue;
        model.SemesterOptions = BuildSemesterOptions(semestersResult.Data, semesterId);
        model.TeacherOptions = assignmentsResult.Data?
            .GroupBy(a => a.TeacherId)
            .Select(g =>
            {
                var teacher = g.First().Teacher;
                return new SelectListItem($"{teacher.User.FullName} ({teacher.TeacherCode})", teacher.Id.ToString(), teacher.Id == teacherId);
            })
            .OrderBy(x => x.Text).ToList() ?? [];
        return View(model);
    }

    [Authorize(Roles = "Staff")]
    public async Task<IActionResult> Create(int? dayOfWeek, int? timeSlotId, int? teacherId, int? semesterId, DateTime? weekStart)
    {
        if (!teacherId.HasValue) return RedirectToAction(nameof(Index), new { semesterId, weekStart });
        return View("Edit", await BuildEditModel(null, dayOfWeek, timeSlotId, teacherId, semesterId, weekStart));
    }

    [Authorize(Roles = "Staff")]
    public async Task<IActionResult> Edit(int id, int? teacherId, int? semesterId, DateTime? weekStart)
    {
        var result = await timetableService.GetByIdAsync(id);
        if (!result.Success || result.Data == null) { TempData["ErrorMessage"] = result.Message; return RedirectToAction(nameof(Index)); }
        if ((teacherId.HasValue && result.Data.TeachingAssignment.TeacherId != teacherId.Value) ||
            (semesterId.HasValue && result.Data.TeachingAssignment.SemesterId != semesterId.Value))
        {
            TempData["ErrorMessage"] = "Lịch học không thuộc giáo viên hoặc học kỳ đang chọn.";
            return RedirectToAction(nameof(Index), new { teacherId, semesterId, weekStart });
        }
        return View(await BuildEditModel(result.Data, null, null, teacherId ?? result.Data.TeachingAssignment.TeacherId, semesterId ?? result.Data.TeachingAssignment.SemesterId, weekStart));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Staff")]
    public async Task<IActionResult> Edit(TimetableEditViewModel model)
    {
        if (!ModelState.IsValid) { await PopulateOptions(model); return View(model); }
        if (!model.SelectedSemesterId.HasValue || !model.SelectedTeacherId.HasValue)
        {
            ModelState.AddModelError(string.Empty, "Vui lòng chọn đúng giáo viên và học kỳ trước khi xếp lịch.");
            await PopulateOptions(model);
            return View(model);
        }
        var assignments = await timetableService.GetAssignmentsAsync(model.SelectedSemesterId);
        var selectedAssignment = assignments.Data?.FirstOrDefault(a => a.Id == model.TeachingAssignmentId);
        if (selectedAssignment == null || selectedAssignment.TeacherId != model.SelectedTeacherId.Value)
        {
            ModelState.AddModelError(nameof(model.TeachingAssignmentId), "Giáo viên của lịch này không được thay đổi.");
            await PopulateOptions(model);
            return View(model);
        }
        if (model.Id.HasValue)
        {
            var existing = await timetableService.GetByIdAsync(model.Id.Value);
            if (!existing.Success || existing.Data == null ||
                (model.SelectedTeacherId.HasValue && existing.Data.TeachingAssignment.TeacherId != model.SelectedTeacherId.Value) ||
                (model.SelectedSemesterId.HasValue && existing.Data.TeachingAssignment.SemesterId != model.SelectedSemesterId.Value))
            {
                ModelState.AddModelError(string.Empty, "Lịch học không thuộc giáo viên hoặc học kỳ đang chọn.");
                await PopulateOptions(model);
                return View(model);
            }
        }
        var result = await timetableService.SaveAsync(model.Id, model.TeachingAssignmentId, model.RoomId, model.TimeSlotId, model.DayOfWeek);
        if (!result.Success) { ModelState.AddModelError(string.Empty, result.Message ?? "Không thể lưu thời khóa biểu."); await PopulateOptions(model); return View(model); }
        TempData["SuccessMessage"] = model.Id.HasValue ? "Đã cập nhật tiết học thành công." : "Đã thêm tiết học thành công.";
        return RedirectToAction(nameof(Index), new { teacherId = model.SelectedTeacherId, semesterId = model.SelectedSemesterId, weekStart = model.WeekStart, editMode = true });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Staff")]
    public async Task<IActionResult> Delete(int id, int? teacherId, int? semesterId, DateTime? weekStart)
    {
        var existing = await timetableService.GetByIdAsync(id);
        if (!existing.Success || existing.Data == null ||
            (teacherId.HasValue && existing.Data.TeachingAssignment.TeacherId != teacherId.Value) ||
            (semesterId.HasValue && existing.Data.TeachingAssignment.SemesterId != semesterId.Value))
        {
            TempData["ErrorMessage"] = "Lịch học không thuộc giáo viên hoặc học kỳ đang chọn.";
            return RedirectToAction(nameof(Index), new { teacherId, semesterId, weekStart, editMode = true });
        }
        var result = await timetableService.DeleteAsync(id);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success ? "Đã xóa tiết học thành công." : result.Message;
        return RedirectToAction(nameof(Index), new { teacherId, semesterId, weekStart, editMode = true });
    }

    [Authorize(Roles = "Teacher,Student,Parent")]
    public async Task<IActionResult> MySchedule(int? semesterId, DateTime? weekStart)
    {
        var role = User.FindFirstValue(ClaimTypes.Role) ?? "";
        var userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
        semesterId ??= await timetableService.GetActiveSemesterIdAsync();
        weekStart = await NormalizeWeekStartAsync(semesterId, weekStart);
        var result = await timetableService.GetForCurrentUserAsync(role, userId, semesterId);
        var slots = await timetableService.GetTimeSlotsAsync();
        var semesters = await semesterService.GetAllAsync();
        if (!result.Success) TempData["ErrorMessage"] = result.Message;
        ViewData["Title"] = "Thời khóa biểu";
        var model = MapList(result.Data, weekStart, slots.Data);
        model.SelectedSemesterId = semesterId;
        model.SemesterOptions = BuildSemesterOptions(semesters.Data, semesterId);
        return View("MySchedule", model);
    }

    private async Task<TimetableEditViewModel> BuildEditModel(HighSchoolManagementSystem.Models.Entities.TimetableEntry? entry, int? dayOfWeek = null, int? timeSlotId = null, int? teacherId = null, int? semesterId = null, DateTime? weekStart = null)
    {
        var selectedTeacherId = teacherId ?? entry?.TeachingAssignment.TeacherId;
        var selectedSemesterId = semesterId ?? entry?.TeachingAssignment.SemesterId ?? await timetableService.GetActiveSemesterIdAsync();
        var model = new TimetableEditViewModel { Id = entry?.Id, TeachingAssignmentId = entry?.TeachingAssignmentId ?? 0, RoomId = entry?.RoomId ?? 0, TimeSlotId = entry?.TimeSlotId ?? timeSlotId ?? 0, DayOfWeek = entry?.DayOfWeek ?? dayOfWeek ?? 2, SelectedTeacherId = selectedTeacherId, SelectedSemesterId = selectedSemesterId, WeekStart = weekStart };
        await PopulateOptions(model);
        return model;
    }

    private async Task PopulateOptions(TimetableEditViewModel model)
    {
        var semesterId = model.SelectedSemesterId ?? await timetableService.GetActiveSemesterIdAsync();
        var assignments = await timetableService.GetAssignmentsAsync(semesterId);
        var rooms = await timetableService.GetRoomsAsync();
        var slots = await timetableService.GetTimeSlotsAsync();
        var teacherAssignments = assignments.Data?.Where(a => !model.SelectedTeacherId.HasValue || a.TeacherId == model.SelectedTeacherId.Value).ToList() ?? [];
        var selectedAssignment = teacherAssignments.FirstOrDefault(a => a.Id == model.TeachingAssignmentId);
        model.TeacherDisplayName = selectedAssignment?.Teacher.User.FullName
            ?? teacherAssignments.FirstOrDefault()?.Teacher.User.FullName
            ?? "Chưa chọn giáo viên";
        model.AssignmentOptions = teacherAssignments.Select(a => new SelectListItem($"{a.Class.ClassName} - {a.Subject.SubjectName}", a.Id.ToString(), a.Id == model.TeachingAssignmentId));
        model.RoomOptions = rooms.Data?.Select(r => new SelectListItem($"{r.RoomCode} - {r.RoomName}", r.Id.ToString(), r.Id == model.RoomId)) ?? [];
        model.TimeSlotOptions = slots.Data?.Select(t => new SelectListItem($"Tiết {t.PeriodNo} ({t.StartTime:hh\\:mm} - {t.EndTime:hh\\:mm})", t.Id.ToString(), t.Id == model.TimeSlotId)) ?? [];
    }

    internal static TimetableListViewModel MapList(IReadOnlyList<HighSchoolManagementSystem.Models.Entities.TimetableEntry>? entries, DateTime? weekStart, IReadOnlyList<HighSchoolManagementSystem.Models.Entities.TimeSlot>? slots)
    {
        var date = (weekStart ?? HighSchoolManagementSystem.Services.Common.SchoolDate.Today).Date;
        var mondayOffset = ((int)date.DayOfWeek + 6) % 7;
        date = date.AddDays(-mondayOffset);
        return new TimetableListViewModel
        {
            WeekStart = date,
            Slots = slots?.OrderBy(s => s.StartTime).Select(s => new TimetableSlotViewModel { Id = s.Id, PeriodNo = s.PeriodNo, Session = s.Session, SlotCode = s.SlotCode, TimeRange = s.StartTime.ToString(@"hh\:mm") + " - " + s.EndTime.ToString(@"hh\:mm") }).ToList() ?? [],
            Entries = entries?.Select(e => new TimetableItemViewModel { Id = e.Id, TimeSlotId = e.TimeSlotId, ClassId = e.TeachingAssignment.ClassId, DayOfWeek = e.DayOfWeek, PeriodNo = e.TimeSlot.PeriodNo, TimeRange = $"{e.TimeSlot.StartTime:hh\\:mm} - {e.TimeSlot.EndTime:hh\\:mm}", ClassName = e.TeachingAssignment.Class.ClassName, SubjectName = e.TeachingAssignment.Subject.SubjectName, TeacherName = e.TeachingAssignment.Teacher.User.FullName, RoomName = e.Room.RoomName }).ToList() ?? [],
            PeriodTimes = slots?.GroupBy(s => s.PeriodNo).ToDictionary(g => g.Key, g => $"{g.First().StartTime:hh\\:mm} - {g.First().EndTime:hh\\:mm}") ?? new Dictionary<int, string>()
            ,PeriodSlotIds = slots?.GroupBy(s => s.PeriodNo).ToDictionary(g => g.Key, g => g.First().Id) ?? new Dictionary<int, int>()
        };
    }

    private static IEnumerable<SelectListItem> BuildSemesterOptions(IReadOnlyList<HighSchoolManagementSystem.Models.Entities.Semester>? semesters, int? selectedId) =>
        semesters?.Select(s => new SelectListItem($"{s.SemesterName} ({s.StartDate:dd/MM/yyyy} - {s.EndDate:dd/MM/yyyy})", s.Id.ToString(), s.Id == selectedId)) ?? [];

    private async Task<DateTime?> NormalizeWeekStartAsync(int? semesterId, DateTime? weekStart)
    {
        if (!semesterId.HasValue || !weekStart.HasValue) return weekStart;

        var semesterResult = await semesterService.GetByIdAsync(semesterId.Value);
        var semester = semesterResult.Success ? semesterResult.Data : null;
        if (semester == null) return weekStart;

        var requestedDate = weekStart.Value.Date;
        if (requestedDate >= semester.StartDate.Date && requestedDate <= semester.EndDate.Date)
        {
            return requestedDate;
        }

        TempData["ErrorMessage"] = $"Tuần đã chọn nằm ngoài thời gian của {semester.SemesterName}. Hệ thống đã chuyển về tuần bắt đầu học kỳ.";
        return semester.StartDate.Date;
    }
}
