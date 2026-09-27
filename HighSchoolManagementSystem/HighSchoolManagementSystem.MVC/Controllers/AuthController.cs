using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.MVC.Models.ViewModels;
using HighSchoolManagementSystem.MVC.Models.ViewModels.Auth;
using HighSchoolManagementSystem.Services.Interfaces;

namespace HighSchoolManagementSystem.MVC.Controllers;

public class AuthController : Controller
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var role = User.FindFirstValue(ClaimTypes.Role);
            return RedirectToDashboardByRole(role);
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _authService.AuthenticateAsync(model.UsernameOrEmail, model.Password);
        if (!result.Success || result.Data == null)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Tên đăng nhập hoặc mật khẩu không chính xác.");
            return View(model);
        }

        var user = result.Data;
        var roleName = user.Role?.RoleName ?? string.Empty;

        // Thiết lập phiên đăng nhập Cookie
        await SignInUserAsync(user, roleName, model.RememberMe);

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return RedirectToDashboardByRole(roleName);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    // =========================================================================
    // QUÊN & ĐẶT LẠI MẬT KHẨU (FORGOT & RESET PASSWORD)
    // =========================================================================

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var role = User.FindFirstValue(ClaimTypes.Role);
            return RedirectToDashboardByRole(role);
        }

        return View(new ForgotPasswordViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var resetLinkBaseUrl = Url.Action("ResetPassword", "Auth", null, Request.Scheme) 
            ?? $"{Request.Scheme}://{Request.Host}/Auth/ResetPassword";

        var result = await _authService.SendResetPasswordEmailAsync(model.Email, resetLinkBaseUrl);

        if (!result.Success)
        {
            if (result.ErrorCode == "MAIL_002")
            {
                ViewBag.WarningMessage = "Hệ thống email chưa được cấu hình tài khoản gửi trong appsettings.json. Đường link đặt lại mật khẩu đã được in ra Console để hỗ trợ kiểm thử.";
            }
            else
            {
                ModelState.AddModelError(string.Empty, result.Message ?? "Không thể gửi email đặt lại mật khẩu. Vui lòng thử lại sau.");
                return View(model);
            }
        }

        ViewBag.SuccessMessage = "Nếu địa chỉ email tồn tại trong hệ thống, hướng dẫn đặt lại mật khẩu đã được gửi đến hòm thư của bạn. Vui lòng kiểm tra hộp thư đến (hoặc hòm thư rác/spam).";
        ViewBag.SubmittedEmail = model.Email;
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ResetPassword(string? email, string? token)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var role = User.FindFirstValue(ClaimTypes.Role);
            return RedirectToDashboardByRole(role);
        }

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
        {
            TempData["ErrorMessage"] = "Liên kết đặt lại mật khẩu không hợp lệ hoặc thiếu thông tin xác thực.";
            return RedirectToAction("Login");
        }

        var validateResult = await _authService.ValidatePasswordResetTokenAsync(email, token);
        if (!validateResult.Success)
        {
            TempData["ErrorMessage"] = validateResult.Message ?? "Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.";
            return RedirectToAction("Login");
        }

        return View(new ResetPasswordViewModel
        {
            Email = email,
            Token = token
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _authService.ResetPasswordAsync(model.Email, model.Token, model.NewPassword);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Đặt lại mật khẩu thất bại.");
            return View(model);
        }

        TempData["SuccessMessage"] = "Đặt lại mật khẩu thành công! Vui lòng đăng nhập bằng mật khẩu mới.";
        return RedirectToAction("Login");
    }

    // PRIVATE HELPERS

    private async Task SignInUserAsync(User user, string roleName, bool rememberMe)
    {
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
            IsPersistent = rememberMe,
            ExpiresUtc = rememberMe 
                ? DateTimeOffset.UtcNow.AddDays(14) 
                : DateTimeOffset.UtcNow.AddHours(8)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);
    }

    private IActionResult RedirectToDashboardByRole(string? roleName)
    {
        return roleName switch
        {
            "Principal" => RedirectToAction("Dashboard", "Principal"),
            "Staff" => RedirectToAction("Dashboard", "Staff"),
            "Teacher" => RedirectToAction("Dashboard", "Teacher"),
            "Student" => RedirectToAction("Dashboard", "Student"),
            "Parent" => RedirectToAction("Dashboard", "Parent"),
            _ => RedirectToAction("Index", "Home")
        };
    }
}
