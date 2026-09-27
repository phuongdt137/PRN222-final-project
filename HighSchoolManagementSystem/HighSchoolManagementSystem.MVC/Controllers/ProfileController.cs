using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.MVC.Models.ViewModels.Profile;
using HighSchoolManagementSystem.Services.DTOs;
using HighSchoolManagementSystem.Services.Interfaces;

namespace HighSchoolManagementSystem.MVC.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly IUserService _userService;
    private readonly IFileStorageService _fileStorageService;

    public ProfileController(IUserService userService, IFileStorageService fileStorageService)
    {
        _userService = userService;
        _fileStorageService = fileStorageService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (!TryGetCurrentUserId(out int userId))
        {
            return RedirectToAction("Login", "Auth");
        }

        var result = await _userService.GetUserProfileAsync(userId);
        if (!result.Success || result.Data == null)
        {
            TempData["ErrorMessage"] = result.Message ?? "Không tìm thấy thông tin người dùng.";
            return RedirectToAction("Index", "Home");
        }

        var viewModel = MapToUserProfileViewModel(result.Data);
        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        if (!TryGetCurrentUserId(out int userId))
        {
            return RedirectToAction("Login", "Auth");
        }

        var result = await _userService.GetUserProfileAsync(userId);
        if (!result.Success || result.Data == null)
        {
            TempData["ErrorMessage"] = result.Message ?? "Không tìm thấy thông tin người dùng.";
            return RedirectToAction("Index");
        }

        var user = result.Data;
        var model = new UpdateProfileViewModel
        {
            Username = user.Username,
            FullName = user.FullName,
            Email = user.Email,
            RoleName = user.Role?.RoleName ?? string.Empty,
            RoleDescription = user.Role?.Description,
            PhoneNumber = user.PhoneNumber,
            Gender = user.Gender,
            DateOfBirth = user.DateOfBirth,
            AvatarUrl = user.AvatarUrl
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UpdateProfileViewModel model)
    {
        if (!TryGetCurrentUserId(out int userId))
        {
            return RedirectToAction("Login", "Auth");
        }

        // Lấy thông tin user hiện tại để hiển thị lại các trường read-only nếu form lỗi
        var currentUserResult = await _userService.GetUserProfileAsync(userId);
        if (!currentUserResult.Success || currentUserResult.Data == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy tài khoản người dùng.";
            return RedirectToAction("Index");
        }

        var currentUser = currentUserResult.Data;
        model.Username = currentUser.Username;
        model.FullName = currentUser.FullName;
        model.Email = currentUser.Email;
        model.RoleName = currentUser.Role?.RoleName ?? string.Empty;
        model.RoleDescription = currentUser.Role?.Description;
        model.AvatarUrl = currentUser.AvatarUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        string? newAvatarUrl = null;

        // Xử lý tải lên ảnh đại diện mới nếu người dùng chọn file
        if (model.AvatarFile != null && model.AvatarFile.Length > 0)
        {
            using (var stream = model.AvatarFile.OpenReadStream())
            {
                var saveResult = await _fileStorageService.SaveFileAsync(
                    stream, 
                    model.AvatarFile.FileName, 
                    "uploads/avatars");

                if (!saveResult.Success || string.IsNullOrEmpty(saveResult.Data))
                {
                    ModelState.AddModelError(nameof(model.AvatarFile), saveResult.Message ?? "Tải ảnh đại diện thất bại.");
                    return View(model);
                }

                newAvatarUrl = saveResult.Data;

                // Xóa ảnh đại diện cũ nếu là ảnh do hệ thống lưu trên local
                if (!string.IsNullOrWhiteSpace(currentUser.AvatarUrl) && 
                    currentUser.AvatarUrl.StartsWith("/uploads/avatars/", StringComparison.OrdinalIgnoreCase))
                {
                    await _fileStorageService.DeleteFileAsync(currentUser.AvatarUrl);
                }
            }
        }

        var updateDto = new UpdateProfileDto
        {
            PhoneNumber = model.PhoneNumber,
            Gender = model.Gender,
            DateOfBirth = model.DateOfBirth,
            AvatarUrl = newAvatarUrl ?? currentUser.AvatarUrl
        };

        var updateResult = await _userService.UpdateProfileAsync(userId, updateDto);
        if (!updateResult.Success || updateResult.Data == null)
        {
            ModelState.AddModelError(string.Empty, updateResult.Message ?? "Cập nhật hồ sơ thất bại.");
            return View(model);
        }

        // Cập nhật lại phiên đăng nhập (Cookie Claims) với avatar mới
        await RefreshUserClaimsAsync(updateResult.Data);

        TempData["SuccessMessage"] = "Cập nhật hồ sơ cá nhân thành công!";
        return RedirectToAction("Index");
    }

    // =========================================================================
    // ĐỔI MẬT KHẨU (CHANGE PASSWORD)
    // =========================================================================

    [HttpGet]
    public IActionResult ChangePassword()
    {
        if (!TryGetCurrentUserId(out _))
        {
            return RedirectToAction("Login", "Auth");
        }

        return View(new ChangePasswordViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!TryGetCurrentUserId(out int userId))
        {
            return RedirectToAction("Login", "Auth");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var dto = new ChangePasswordDto
        {
            CurrentPassword = model.CurrentPassword,
            NewPassword = model.NewPassword
        };

        var result = await _userService.ChangePasswordAsync(userId, dto);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Đổi mật khẩu thất bại.");
            return View(model);
        }

        TempData["SuccessMessage"] = "Đổi mật khẩu thành công! Hãy ghi nhớ mật khẩu mới của bạn.";
        return RedirectToAction("Index");
    }

    // =========================================================================
    // PRIVATE HELPERS
    // =========================================================================

    private bool TryGetCurrentUserId(out int userId)
    {
        userId = 0;
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out userId) && userId > 0;
    }

    private async Task RefreshUserClaimsAsync(User user)
    {
        var roleName = user.Role?.RoleName ?? User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.GivenName, user.FullName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, roleName),
            new Claim("AvatarUrl", user.AvatarUrl ?? string.Empty)
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);
    }

    private static UserProfileViewModel MapToUserProfileViewModel(User user)
    {
        var vm = new UserProfileViewModel
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            Gender = user.Gender,
            DateOfBirth = user.DateOfBirth,
            AvatarUrl = user.AvatarUrl,
            RoleName = user.Role?.RoleName ?? string.Empty,
            RoleDescription = user.Role?.Description,
            IsActive = user.IsActive,
            CreateAt = user.CreateAt
        };

        if (user.Teacher != null)
        {
            vm.TeacherSection = new TeacherProfileSection
            {
                TeacherCode = user.Teacher.TeacherCode,
                Specialization = user.Teacher.Specialization,
                Qualification = user.Teacher.Qualification,
                HireDate = user.Teacher.HireDate,
                Status = user.Teacher.Status,
                MaxPeriodsPerWeek = user.Teacher.MaxPeriodsPerWeek,
                HomeroomClassNames = user.Teacher.HomeroomClasses?.Select(c => c.ClassName).ToList() ?? new List<string>()
            };
        }

        if (user.Student != null)
        {
            var enrolledClass = user.Student.StudentClasses?.FirstOrDefault(sc => sc.Status == "Enrolled")?.Class;
            vm.StudentSection = new StudentProfileSection
            {
                StudentCode = user.Student.StudentCode,
                AdmissionDate = user.Student.AdmissionDate,
                Status = user.Student.Status,
                Address = user.Student.Address,
                CurrentClassName = enrolledClass?.ClassName,
                ParentFullName = user.Student.Parent?.User?.FullName,
                ParentPhone = user.Student.Parent?.User?.PhoneNumber ?? user.Student.Parent?.EmergencyContact
            };
        }

        if (user.Parent != null)
        {
            vm.ParentSection = new ParentProfileSection
            {
                Occupation = user.Parent.Occupation,
                EmergencyContact = user.Parent.EmergencyContact,
                Children = user.Parent.Students?.Select(s => new StudentChildSummary
                {
                    FullName = s.User?.FullName ?? "Học sinh",
                    StudentCode = s.StudentCode,
                    CurrentClassName = s.StudentClasses?.FirstOrDefault(sc => sc.Status == "Enrolled")?.Class?.ClassName
                }).ToList() ?? new List<StudentChildSummary>()
            };
        }

        return vm;
    }
}
