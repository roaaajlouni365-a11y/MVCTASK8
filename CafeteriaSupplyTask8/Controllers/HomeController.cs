using Microsoft.AspNetCore.Mvc;

namespace CafeteriaSupplyTask8.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => View();
    public IActionResult Error() { Response.StatusCode = 500; return View(); }
}
