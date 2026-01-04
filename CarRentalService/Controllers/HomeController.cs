using Microsoft.AspNetCore.Mvc;

namespace CarRentalService.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
