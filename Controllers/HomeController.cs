using Microsoft.AspNetCore.Mvc;

namespace Finder.Web.Controllers;

public class HomeController : Controller {
    private readonly ILogger<HomeController> _logger;
    public HomeController(ILogger<HomeController> logger) {
        _logger = logger;
    }

    [Route("/")]
    public IActionResult Index() {
        ViewBag.DarkMode = true;
        return View("Index");
    }

    [Route("/privacy")]
    public IActionResult Privacy() {
        ViewBag.DarkMode = false;
        return View("Privacy");
    }
}