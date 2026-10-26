using HighSchoolManagementSystem.MVC.Models.ViewModels;
using HighSchoolManagementSystem.MVC.Models.ViewModels.Assessment;
using HighSchoolManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HighSchoolManagementSystem.MVC.Controllers;

[Authorize(Roles = "Staff")]
public class AssessmentConfigurationController(IAssessmentConfigurationService service) : Controller
{
    public async Task<IActionResult> Index()
    {
        var result = await service.GetAllAsync();
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
            return View(new AssessmentConfigurationViewModel());
        }

        return View(new AssessmentConfigurationViewModel
        {
            Items = result.Data!.Select(t => new AssessmentConfigurationItemViewModel
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
        var result = await service.GetByIdAsync(id);
        if (!result.Success || result.Data == null) return RedirectToAction(nameof(Index));
        var item = result.Data;
        return View(new AssessmentConfigurationEditViewModel { Id = item.Id, TypeCode = item.TypeCode, TypeName = item.TypeName, Weight = item.Weight, MinEntries = item.MinEntries, MaxEntries = item.MaxEntries, MinScore = item.MinScore, MaxScore = item.MaxScore });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AssessmentConfigurationEditViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await service.SaveAsync(model.Id, model.TypeCode, model.TypeName, model.Weight,
            model.MinEntries, model.MaxEntries, model.MinScore, model.MaxScore);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Không thể lưu cấu hình đánh giá.");
            return View(model);
        }
        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await service.DeleteAsync(id);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }
}
