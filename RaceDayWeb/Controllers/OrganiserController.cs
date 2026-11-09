using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Services;

namespace RaceDayWeb.Controllers
{
    [SessionAuthorize(Role = "Organiser")]
    public class OrganiserController : BaseController
    {
        public IActionResult Dashboard()
        {
            return View();
        }
    }
}