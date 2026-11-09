using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Services;

namespace RaceDayWeb.Controllers
{
    [SessionAuthorize(Role = "Participant")]
    public class EventsController : BaseController
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}