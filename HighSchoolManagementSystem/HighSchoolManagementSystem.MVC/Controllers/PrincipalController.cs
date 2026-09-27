using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HighSchoolManagementSystem.MVC.Controllers;

[Authorize(Roles = "Principal")]
public class PrincipalController : Controller
{
    public IActionResult Dashboard()
    {
        return View();
    }
}
