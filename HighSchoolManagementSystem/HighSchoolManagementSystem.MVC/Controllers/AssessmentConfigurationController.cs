using HighSchoolManagementSystem.MVC.Models.ViewModels;
using HighSchoolManagementSystem.MVC.Models.ViewModels.Assessment;
using HighSchoolManagementSystem.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HighSchoolManagementSystem.MVC.Controllers;

[Authorize(Roles = "Staff")]
public class AssessmentConfigurationController(IAssessmentRepository repository) : Controller
{
    public async Task<IActionResult> Index()
    {
        var types = await repository.GetAllAssessmentTypesAsync();
        return View(new AssessmentConfigurationViewModel
        {
            Items = types.Select(t => new AssessmentConfigurationItemViewModel
            {
                Id = t.Id,
                TypeCode = t.TypeCode,
                TypeName = t.TypeName,
                Weight = t.Weight,
                MinEntries = t.MinEntries,
                MaxEntries = t.MaxEntries,
                MinScore = t.MinScore,
                MaxScore = t.MaxScore,
                AssessmentCount = t.Assessments.Count
            }).ToList()
        });
    }

    public IActionResult Create() => View("Edit", new AssessmentConfigurationEditViewModel());

    public async Task<IActionResult> Edit(int id)
    {
        var item = await repository.GetAssessmentTypeByIdAsync(id);
        if (item == null) return RedirectToAction(nameof(Index));
        return View(new AssessmentConfigurationEditViewModel { Id = item.Id, TypeCode = item.TypeCode, TypeName = item.TypeName, Weight = item.Weight, MinEntries = item.MinEntries, MaxEntries = item.MaxEntries, MinScore = item.MinScore, MaxScore = item.MaxScore });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AssessmentConfigurationEditViewModel model)
    {
        if (model.MinEntries > model.MaxEntries) ModelState.AddModelError(nameof(model.MinEntries), "Số lần tối thiểu không được lớn hơn tối đa.");
        if (model.MinScore > model.MaxScore) ModelState.AddModelError(nameof(model.MinScore), "Điểm tối thiểu không được lớn hơn tối đa.");
        if (!ModelState.IsValid) return View(model);
        var code = model.TypeCode.Trim().ToUpperInvariant();
        if (await repository.AssessmentTypeCodeExistsAsync(code, model.Id)) { ModelState.AddModelError(nameof(model.TypeCode), "Mã loại đánh giá đã tồn tại."); return View(model); }
        if (await repository.GetAssessmentTypeWeightTotalAsync(model.Id) + model.Weight > 100) { ModelState.AddModelError(nameof(model.Weight), "Tổng trọng số không được vượt quá 100%."); return View(model); }
        var item = model.Id.HasValue ? await repository.GetAssessmentTypeByIdAsync(model.Id.Value) : new HighSchoolManagementSystem.Models.Entities.AssessmentType();
        if (model.Id.HasValue && item == null) return RedirectToAction(nameof(Index));
        if (model.Id.HasValue && item!.Assessments.Count > model.MaxEntries)
        {
            ModelState.AddModelError(nameof(model.MaxEntries), $"Không thể đặt số lần tối đa thấp hơn số bài đã tồn tại ({item.Assessments.Count}).");
            return View(model);
        }
        item!.TypeCode = code; item.TypeName = model.TypeName.Trim(); item.Weight = model.Weight; item.MinEntries = model.MinEntries; item.MaxEntries = model.MaxEntries; item.MinScore = model.MinScore; item.MaxScore = model.MaxScore;
        if (!model.Id.HasValue) await repository.AddAssessmentTypeAsync(item);
        else await repository.UpdateAssessmentTypeAsync(item);
        await repository.SaveChangesAsync();
        TempData["SuccessMessage"] = "Đã lưu cấu hình đánh giá.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await repository.GetAssessmentTypeByIdAsync(id);
        if (item == null) return RedirectToAction(nameof(Index));
        if (item.Assessments.Count > 0) TempData["ErrorMessage"] = "Không thể xóa loại đánh giá đã được sử dụng.";
        else { await repository.DeleteAssessmentTypeAsync(item); await repository.SaveChangesAsync(); TempData["SuccessMessage"] = "Đã xóa loại đánh giá."; }
        return RedirectToAction(nameof(Index));
    }
}
