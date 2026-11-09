using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Services;

namespace RaceDayWeb.Controllers
{
    public abstract class BaseController : Controller
    {
        protected bool IsLoggedIn => !string.IsNullOrEmpty(HttpContext.Session.GetString(SessionKeys.Token));

        protected string? CurrentRole => HttpContext.Session.GetString(SessionKeys.Role);

        // Each role has its own landing page
        protected IActionResult RedirectToHome()
        {
            if (CurrentRole == "Organiser")
            {
                return RedirectToAction("Dashboard", "Organiser");
            }

            return RedirectToAction("Index", "Events");
        }

        // Call this after an API request on a logged-in page. If the token has expired,
        // it ends the session and returns a redirect to login. Otherwise it returns null.
        protected IActionResult? HandleExpiredSession<T>(ApiResult<T> result)
        {
            if (result.IsUnauthorised)
            {
                HttpContext.Session.Clear();
                TempData["Info"] = "Your session has expired. Please log in again.";
                return RedirectToAction("Login", "Account");
            }

            return null;
        }
    }
}