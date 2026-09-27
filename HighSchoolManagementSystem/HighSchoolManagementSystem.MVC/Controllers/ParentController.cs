using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HighSchoolManagementSystem.MVC.Controllers;

[Authorize(Roles = "Parent")]
public class ParentController : Controller
{
    public IActionResult Dashboard()
    {
        return View();
    }
}
