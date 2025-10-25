using ICRent.Web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace ICRent.Web.Controllers
{
    public class LandingController : Controller
    {
        public IActionResult Index()
        {
            // Kullanýcý giriþ yapmamýþsa PublicLayout göster
            if (!User.Identity!.IsAuthenticated)
            {
                ViewData["Layout"] = "_PublicLayout";
                return View("PublicHome"); // ayrý public sayfa
            }

            // Giriþ yapmýþsa rolüne göre yönlendir
            if (User.IsInRole("Admin"))
                return RedirectToAction("Index", "Vehicles");
            if (User.IsInRole("User"))
                return RedirectToAction("Create", "WorkLogs");

            return RedirectToAction("Login", "Account");
        }

    }
}
