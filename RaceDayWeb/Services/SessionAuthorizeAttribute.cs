using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDayWeb.Services
{
    // Put [SessionAuthorize] on a controller to require a login,
    // or [SessionAuthorize(Role = "Organiser")] to require a particular role.
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class SessionAuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        public string? Role { get; set; }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            string? token = context.HttpContext.Session.GetString(SessionKeys.Token);
            string? role = context.HttpContext.Session.GetString(SessionKeys.Role);

            if (string.IsNullOrEmpty(token))
            {
                string returnUrl = context.HttpContext.Request.Path.Value + context.HttpContext.Request.QueryString.Value;
                context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
                return;
            }

            if (Role != null && role != Role)
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
            }
        }
    }
}