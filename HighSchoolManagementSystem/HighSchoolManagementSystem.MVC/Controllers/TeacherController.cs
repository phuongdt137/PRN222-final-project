using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HighSchoolManagementSystem.MVC.Controllers;

[Authorize(Roles = "Teacher")]
public class TeacherController : Controller
{
    public IActionResult Dashboard()
    {
        return View();
    }
}
