using Microsoft.AspNetCore.Mvc;

namespace Deneme.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}