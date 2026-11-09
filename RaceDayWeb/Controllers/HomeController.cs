using Microsoft.AspNetCore.Mvc;

namespace RaceDayWeb.Controllers
{
    public class HomeController : BaseController
    {
        public IActionResult Index()
        {
            if (IsLoggedIn)
            {
                return RedirectToHome();
            }

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}