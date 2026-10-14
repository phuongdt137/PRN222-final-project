using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.MVC.Models.ViewModels.UserManagement;
using HighSchoolManagementSystem.Services.DTOs;
using HighSchoolManagementSystem.Services.Interfaces;

namespace HighSchoolManagementSystem.MVC.Controllers;

[Authorize(Roles = "Staff,Principal")]
public class UserManagementController : Controller
{
    private readonly IUserService _userService;

    public UserManagementController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, int? roleId, string? status)
    {
        bool? isActive = status switch
        {
            "Active" => true,
            "Inactive" => false,
            _ => null
        };

        var usersResult = await _userService.GetUsersAsync(search, roleId, isActive);
        var allUsersResult = await _userService.GetUsersAsync();
        var rolesResult = await _userService.GetRolesAsync();
        var allUsers = allUsersResult.Data ?? new List<User>();

        var model = new UserListViewModel
        {
            Search = search,
            RoleId = roleId,
            Status = status,
            TotalCount = allUsers.Count,
            ActiveCount = allUsers.Count(u => u.IsActive),
            InactiveCount = allUsers.Count(u => !u.IsActive),
            Roles = rolesResult.Data?.Select(r => new RoleOptionViewModel
            {
                Id = r.RoleId,
                Name = r.RoleName
            }).ToList() ?? new(),
            Users = usersResult.Data?.Select(u => MapListItem(u)).ToList() ?? new()
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var result = await _userService.GetUserByIdAsync(id);
        if (!result.Success || result.Data == null)
        {
            TempData["ErrorMessage"] = result.Message ?? "Không tìm thấy thông tin tài khoản.";
            return RedirectToAction(nameof(Index));
        }

        return View(MapDetails(result.Data));
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await PopulateRolesAsync();
        return View(new CreateUserViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateRolesAsync();
            return View(model);
        }

        var result = await _userService.CreateUserAsync(new CreateUserDto
        {
            Username = model.Username,
            Email = model.Email,
            FullName = model.FullName,
            Password = model.Password,
            PhoneNumber = model.PhoneNumber,
            Gender = model.Gender,
            DateOfBirth = model.DateOfBirth,
            RoleId = model.RoleId,
            IsActive = model.IsActive,
            AdmissionDate = model.AdmissionDate,
            Address = model.Address,
            Specialization = model.Specialization,
            Qualification = model.Qualification,
            MaxPeriodsPerWeek = model.MaxPeriodsPerWeek,
            HireDate = model.HireDate,
            Occupation = model.Occupation,
            EmergencyContact = model.EmergencyContact
        });

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Đã xảy ra lỗi khi tạo tài khoản.");
            await PopulateRolesAsync();
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var result = await _userService.GetUserByIdAsync(id);
        if (!result.Success || result.Data == null)
        {
            TempData["ErrorMessage"] = result.Message ?? "Không tìm thấy tài khoản cần cập nhật.";
            return RedirectToAction(nameof(Index));
        }

        await PopulateRolesAsync();
        return View(new EditUserViewModel
        {
            Id = result.Data.Id,
            Username = result.Data.Username,
            Email = result.Data.Email,
            FullName = result.Data.FullName,
            PhoneNumber = result.Data.PhoneNumber,
            Gender = result.Data.Gender,
            DateOfBirth = result.Data.DateOfBirth,
            RoleId = result.Data.RoleId
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EditUserViewModel model)
    {
        if (id != model.Id)
        {
            TempData["ErrorMessage"] = "Yêu cầu cập nhật không hợp lệ.";
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
        {
            await PopulateRolesAsync();
            return View(model);
        }

        var result = await _userService.UpdateUserAccountAsync(id, new UpdateUserAccountDto
        {
            Email = model.Email,
            FullName = model.FullName,
            PhoneNumber = model.PhoneNumber,
            Gender = model.Gender,
            DateOfBirth = model.DateOfBirth,
            RoleId = model.RoleId
        });

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Đã xảy ra lỗi khi cập nhật tài khoản.");
            await PopulateRolesAsync();
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id, string? returnUrl)
    {
        if (id == GetCurrentUserId())
        {
            TempData["ErrorMessage"] = "Bạn không thể tự vô hiệu hóa tài khoản đang đăng nhập.";
            return RedirectToSafeUrl(returnUrl);
        }

        var userResult = await _userService.GetUserByIdAsync(id);
        if (!userResult.Success || userResult.Data == null)
        {
            TempData["ErrorMessage"] = userResult.Message ?? "Không tìm thấy tài khoản.";
            return RedirectToSafeUrl(returnUrl);
        }

        var result = await _userService.SetUserActiveAsync(id, !userResult.Data.IsActive);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToSafeUrl(returnUrl);
    }

    [HttpGet]
    public async Task<IActionResult> ResetPassword(int id)
    {
        var result = await _userService.GetUserByIdAsync(id);
        if (!result.Success || result.Data == null)
        {
            TempData["ErrorMessage"] = result.Message ?? "Không tìm thấy tài khoản.";
            return RedirectToAction(nameof(Index));
        }

        return View(new ResetUserPasswordViewModel
        {
            Id = result.Data.Id,
            Username = result.Data.Username,
            FullName = result.Data.FullName
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetUserPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _userService.ResetUserPasswordAsync(model.Id, new ResetUserPasswordDto
        {
            NewPassword = model.NewPassword
        });

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Đã xảy ra lỗi khi đặt lại mật khẩu.");
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    private async Task PopulateRolesAsync()
    {
        var result = await _userService.GetRolesAsync();
        ViewBag.Roles = result.Data?.Select(r => new RoleOptionViewModel
        {
            Id = r.RoleId,
            Name = r.RoleName
        }).ToList() ?? new List<RoleOptionViewModel>();
    }

    private int GetCurrentUserId()
    {
        return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : 0;
    }

    private IActionResult RedirectToSafeUrl(string? returnUrl)
    {
        return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction(nameof(Index));
    }

    private static UserListItemViewModel MapListItem(User user)
    {
        return new UserListItemViewModel
        {
            Id = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            Email = user.Email,
            RoleName = user.Role?.RoleName ?? "Chưa gán",
            IsActive = user.IsActive,
            CreateAt = user.CreateAt
        };
    }

    private static UserDetailsViewModel MapDetails(User user)
    {
        string? profileCode = user.Student?.StudentCode ?? user.Teacher?.TeacherCode;
        string? profileDetails = null;

        if (user.Student != null)
        {
            profileDetails = $"Học sinh • Mã: {user.Student.StudentCode} • Trạng thái: {user.Student.Status} • Ngày nhập học: {user.Student.AdmissionDate?.ToString("dd/MM/yyyy") ?? "Chưa rõ"}";
        }
        else if (user.Teacher != null)
        {
            profileDetails = $"Giáo viên • Mã: {user.Teacher.TeacherCode} • Chuyên môn: {user.Teacher.Specialization ?? "Chưa phân"} • Trình độ: {user.Teacher.Qualification ?? "Chưa cập nhật"}";
        }
        else if (user.Parent != null)
        {
            profileDetails = $"Phụ huynh • Nghề nghiệp: {user.Parent.Occupation ?? "Chưa cập nhật"} • LH khẩn cấp: {user.Parent.EmergencyContact ?? "Chưa cập nhật"}";
        }

        return new UserDetailsViewModel
        {
            Id = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            Email = user.Email,
            RoleName = user.Role?.RoleName ?? "Chưa gán",
            IsActive = user.IsActive,
            CreateAt = user.CreateAt,
            PhoneNumber = user.PhoneNumber,
            Gender = user.Gender,
            DateOfBirth = user.DateOfBirth,
            ProfileCode = profileCode,
            ProfileDetails = profileDetails
        };
    }
}
