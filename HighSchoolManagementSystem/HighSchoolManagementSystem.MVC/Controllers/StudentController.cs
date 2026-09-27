using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HighSchoolManagementSystem.MVC.Controllers;

[Authorize(Roles = "Student")]
public class StudentController : Controller
{
    public IActionResult Dashboard()
    {
        return View();
    }
}
