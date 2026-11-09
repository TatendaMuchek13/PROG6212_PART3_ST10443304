using Microsoft.AspNetCore.Mvc;
using RaceDayWeb.Models;
using RaceDayWeb.Services;

namespace RaceDayWeb.Controllers
{
    public class AccountController : BaseController
    {
        private readonly ApiClient _api;

        public AccountController(ApiClient api)
        {
            _api = api;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (IsLoggedIn)
            {
                return RedirectToHome();
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _api.PostAsync<LoginResponse>("api/Auth/login", new { model.Email, model.Password });

            if (!result.Success || result.Data == null)
            {
                string message = string.IsNullOrEmpty(result.Error) ? "We couldn't log you in. Please try again." : result.Error;
                ModelState.AddModelError(string.Empty, message);
                return View(model);
            }

            // Start a fresh session holding the token and role for every later request
            HttpContext.Session.Clear();
            HttpContext.Session.SetString(SessionKeys.Token, result.Data.Token);
            HttpContext.Session.SetString(SessionKeys.Role, result.Data.Role);

            var profile = await _api.GetAsync<ProfileModel>("api/Profile");
            string displayName = profile.Success && profile.Data != null ? profile.Data.Name : model.Email;
            HttpContext.Session.SetString(SessionKeys.UserName, displayName);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToHome();
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (IsLoggedIn)
            {
                return RedirectToHome();
            }

            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _api.PostAsync<string>("api/Auth/register",
                new { model.Name, model.Surname, model.Cell, model.Email, model.Password, model.Role });

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Error);
                return View(model);
            }

            TempData["Success"] = "Your account has been created. Please log in.";
            return RedirectToAction(nameof(Login));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["Info"] = "You have been logged out.";
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}