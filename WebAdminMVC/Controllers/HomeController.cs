using Microsoft.AspNetCore.Mvc;

namespace WebAdmin.MVC.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        // Redirect đến Dashboard nếu đã login, hoặc Login nếu chưa
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }
        return RedirectToAction("Login", "Auth");
    }
}